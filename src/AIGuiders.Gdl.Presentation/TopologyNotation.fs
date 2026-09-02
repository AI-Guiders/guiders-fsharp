namespace AIGuiders.Gdl.Presentation

open System.Text

[<RequireQualifiedAccess>]
module TopologyNotation =
    type private Group = { Stack: string list; Compose: ZoneComposeKind }

    type private AssignResult =
        { Arrangement: TopologyArrangement
          Hosts: LogicalDisplayHost list }

    let rec parse (wire: string) =
        if System.String.IsNullOrWhiteSpace wire then
            TopologyParseResult.Fail "Topology wire is empty."
        else
            let source = wire.Trim()

            if source.Equals("single", System.StringComparison.OrdinalIgnoreCase) then
                TopologyParseResult.Ok
                    { Arrangement = TopologyArrangement.SingleSurfaceCompositional
                      Hosts = []
                      SourceWire = source }
            else
                match parseGroups source with
                | Error message -> TopologyParseResult.Fail message
                | Ok [] -> TopologyParseResult.Fail "Topology wire has no host groups."
                | Ok groups ->
                    match assignHosts groups with
                    | Error message -> TopologyParseResult.Fail message
                    | Ok assign ->
                        let hosts =
                            assign.Hosts
                            |> List.mapi (fun index host ->
                                { host with
                                    HostIndex = index
                                    HostId = buildHostId host.Role index assign.Hosts })

                        TopologyParseResult.Ok
                            { Arrangement = assign.Arrangement
                              Hosts = hosts
                              SourceWire = source }

    and private assignHosts (groups: Group list) : Result<AssignResult, string> =
        match groups with
        | [ g ] when g.Compose = ZoneComposeKind.OneOf ->
            if List.length g.Stack < 2 then
                Error "OneOf group needs at least two channels."
            else
                Ok
                    { Arrangement = TopologyArrangement.SingleHostOneOf
                      Hosts =
                        [ { HostIndex = 0
                            HostId = "host-0"
                            Role = AttentionDisplayRole.PmOneOf
                            Compose = ZoneComposeKind.OneOf
                            ChannelStack = g.Stack
                            ActiveChannel = List.head g.Stack } ] }
        | [ _ ] ->
            Error "Single () group with '+' is spatial split — use multi-host wire or 'single' + layout board."
        | [ a; b ] when a.Compose <> ZoneComposeKind.OneOf && b.Compose <> ZoneComposeKind.OneOf ->
            if List.length a.Stack = 1 && List.length b.Stack = 1 then
                Ok
                    { Arrangement = TopologyArrangement.MultiHost
                      Hosts = [ hostFromGroup 0 a; hostFromGroup 1 b ] }
            else
                Error "Two dedicated hosts must have one channel each."
        | [ a; b ] when a.Compose = ZoneComposeKind.OneOf <> (b.Compose = ZoneComposeKind.OneOf) ->
            match hostFromGroupOrOneOf 0 a, hostFromGroupOrOneOf 1 b with
            | Ok h0, Ok h1 -> Ok { Arrangement = TopologyArrangement.MultiHost; Hosts = [ h0; h1 ] }
            | Error message, _ -> Error message
            | _, Error message -> Error message
        | [ _; _ ] ->
            Error "Two-window topology needs one dedicated host and one OneOf (/) group, or two dedicated single-channel hosts."
        | groups when List.length groups = 3 ->
            assignThreeHosts groups
        | _ ->
            Error "Topology wire supports 1 OneOf group (single TopLevel), 2–3 multi-host groups, or 'single'."

    and private hostFromGroup index (g: Group) =
        { HostIndex = index
          HostId = $"host-{index}"
          Role = inferRole (List.head g.Stack)
          Compose = ZoneComposeKind.Split
          ChannelStack = g.Stack
          ActiveChannel = List.head g.Stack }

    and private hostFromGroupOrOneOf index (g: Group) =
        if g.Compose = ZoneComposeKind.OneOf then
            if List.length g.Stack < 2 then
                Error "OneOf group needs at least two channels."
            else
                Ok
                    { HostIndex = index
                      HostId = $"host-{index}"
                      Role = AttentionDisplayRole.PmOneOf
                      Compose = ZoneComposeKind.OneOf
                      ChannelStack = g.Stack
                      ActiveChannel = List.head g.Stack }
        elif List.length g.Stack <> 1 then
            Error "Dedicated host group must contain a single channel."
        else
            Ok(hostFromGroup index g)

    and private assignThreeHosts groups =
        let rec loop remaining hosts groups =
            match groups with
            | [] -> Ok { Arrangement = TopologyArrangement.MultiHost; Hosts = List.rev hosts }
            | g :: rest ->
                if g.Compose = ZoneComposeKind.OneOf && List.length g.Stack < 2 then
                    Error "OneOf group needs at least two channels."
                else
                    let preferred = inferRole (List.head g.Stack)

                    let role =
                        if Set.contains preferred remaining then
                            preferred
                        elif Set.contains AttentionDisplayRole.Forward remaining then
                            AttentionDisplayRole.Forward
                        elif Set.contains AttentionDisplayRole.Pfd remaining then
                            AttentionDisplayRole.Pfd
                        else
                            AttentionDisplayRole.Mfd

                    if not (Set.contains role remaining) then
                        Error "Could not assign Pfd/Forward/Mfd roles to three hosts."
                    else
                        let remaining = Set.remove role remaining

                        let scanRole =
                            let candidate =
                                if g.Compose = ZoneComposeKind.OneOf && List.length g.Stack > 1 && role <> AttentionDisplayRole.Forward then
                                    AttentionDisplayRole.PmOneOf
                                else
                                    role

                            if g.Compose = ZoneComposeKind.Split || List.length g.Stack = 1 then role else candidate

                        let host =
                            { HostIndex = List.length hosts
                              HostId = $"host-{List.length hosts}"
                              Role = scanRole
                              Compose = g.Compose
                              ChannelStack = g.Stack
                              ActiveChannel = List.head g.Stack }

                        loop remaining (host :: hosts) rest

        loop
            (set
                [ AttentionDisplayRole.Pfd
                  AttentionDisplayRole.Forward
                  AttentionDisplayRole.Mfd ])
            []
            groups

    and private parseGroups (text: string) : Result<Group list, string> =
        let rec loop i groups =
            if i >= text.Length then
                Ok(List.rev groups)
            elif text.[i] <> '(' then
                Error $"Expected '(' at position {i}."
            else
                let mutable j = i + 1
                let start = j

                while j < text.Length && text.[j] <> ')' do
                    j <- j + 1

                if j >= text.Length then
                    Error "Missing ')' in topology wire."
                else
                    let inner = collapseWs (text.Substring(start, j - start))
                    j <- j + 1

                    match parseGroup inner with
                    | Error message -> Error message
                    | Ok parsed ->
                        let k = skipWs text j
                        loop k (parsed :: groups)

        loop (skipWs text 0) []

    and private parseGroup (inner: string) : Result<Group, string> =
        if inner.Length = 0 then
            Error "Empty () group."
        else
            let rec readTokens i stack compose =
                if i >= inner.Length then
                    let composeKind = defaultArg compose ZoneComposeKind.Split
                    let stackList = List.rev stack

                    if composeKind = ZoneComposeKind.OneOf && List.length stackList < 2 then
                        Error "OneOf needs at least two channels."
                    else
                        Ok { Stack = stackList; Compose = composeKind }
                else
                    match tryReadToken inner i with
                    | None, _ -> Error $"Bad channel token near position {i}."
                    | Some tok, next ->
                        let stack = normalize tok :: stack

                        if next >= inner.Length then
                            readTokens next stack compose
                        elif inner.[next] = '/' then
                            if compose = Some ZoneComposeKind.Split then
                                Error "Mixed '+' and '/' in one group."
                            else
                                readTokens (next + 1) stack (Some ZoneComposeKind.OneOf)
                        elif inner.[next] = '+' then
                            if compose = Some ZoneComposeKind.OneOf then
                                Error "Mixed '+' and '/' in one group."
                            else
                                readTokens (next + 1) stack (Some ZoneComposeKind.Split)
                        else
                            Error $"Expected '/' or '+' after '{tok}'."

            readTokens 0 [] None

    and private inferRole surface =
        match normalize surface with
        | "f"
        | "forward"
        | "fwd"
        | "intercom"
        | "editor"
        | "work" -> AttentionDisplayRole.Forward
        | "p"
        | "pfd"
        | "sit"
        | "report"
        | "plan" -> AttentionDisplayRole.Pfd
        | "m"
        | "mfd"
        | "world"
        | "probe"
        | "shell"
        | "git"
        | "browser"
        | "mcp" -> AttentionDisplayRole.Mfd
        | "alert"
        | "ecl"
        | "eicas" -> AttentionDisplayRole.Eicas
        | _ -> AttentionDisplayRole.Unknown

    and private buildHostId role index all =
        let baseId =
            match role with
            | AttentionDisplayRole.Forward -> "forward"
            | AttentionDisplayRole.Pfd -> "pfd"
            | AttentionDisplayRole.Mfd -> "mfd"
            | AttentionDisplayRole.PmOneOf -> "pm-oneof"
            | AttentionDisplayRole.Eicas -> "eicas"
            | _ -> $"host-{index}"

        if List.filter (fun h -> h.Role = role) all |> List.length <= 1 then
            baseId
        else
            $"{baseId}-{index}"

    and private tryReadToken (s: string) i =
        let start = i
        let mutable j = i

        while j < s.Length && (System.Char.IsLetterOrDigit(s.[j]) || s.[j] = '_' || s.[j] = '-') do
            j <- j + 1

        if j = start then None, i else Some(s.Substring(start, j - start)), j

    and private normalize (s: string) = s.Trim().ToLowerInvariant()

    and private collapseWs (span: string) =
        let sb = StringBuilder(span.Length)

        for c in span do
            if not (System.Char.IsWhiteSpace c) then
                sb.Append c |> ignore

        sb.ToString()

    and private skipWs (text: string) i =
        let mutable j = i

        while j < text.Length && System.Char.IsWhiteSpace(text.[j]) do
            j <- j + 1

        j
