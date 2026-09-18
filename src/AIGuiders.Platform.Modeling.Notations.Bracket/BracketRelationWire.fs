namespace AIGuiders.Platform.Modeling.Notations.Bracket

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

/// <summary>Kind: canon — bracket axes → RelationSpec witness (plan §3.2).</summary>
module BracketRelationWire =
    let private axisValue (wire: NormalizedBracketWire) (key: string) =
        wire.Axes
        |> Seq.tryFind (fun axis -> System.String.Equals(axis.Key, key, System.StringComparison.OrdinalIgnoreCase))
        |> Option.map (fun axis -> axis.Value.Trim())

    let private tryParseInt (raw: string) =
        match System.Int32.TryParse raw with
        | true, v -> Some v
        | _ -> None

    let private parseScopeAxis (raw: string option) =
        match raw with
        | None -> None, None
        | Some value when System.String.IsNullOrWhiteSpace value -> None, None
        | Some value ->
            let trimmed = value.Trim()
            let colon = trimmed.IndexOf(':')

            if colon < 0 then
                Some(trimmed.ToLowerInvariant()), Some 1
            else
                let kind = trimmed.Substring(0, colon).Trim().ToLowerInvariant()

                match tryParseInt (trimmed.Substring(colon + 1)) with
                | Some idx when idx > 0 -> Some kind, Some idx
                | _ -> Some kind, Some 1

    let private parseLineAxis (raw: string option) =
        match raw with
        | None -> None, None
        | Some value when System.String.IsNullOrWhiteSpace value -> None, None
        | Some value ->
            let trimmed = value.Trim()
            let dash = trimmed.IndexOf('-')

            if dash < 0 then
                match tryParseInt trimmed with
                | Some one -> Some one, Some one
                | _ -> None, None
            else
                match tryParseInt (trimmed.Substring(0, dash)), tryParseInt (trimmed.Substring(dash + 1)) with
                | Some start, Some ending -> Some start, Some ending
                | _ -> None, None

    let private codeEditMemberTarget (file: LogicalPath) memberName (wire: NormalizedBracketWire) =
        let scopeRaw = axisValue wire "Scope"
        let scopeKind, scopeIndexFromAxis = parseScopeAxis scopeRaw
        let scopeIndex =
            axisValue wire "ScopeIndex"
            |> Option.bind tryParseInt
            |> Option.orElse scopeIndexFromAxis

        let lineStart, lineEndFromAxis = parseLineAxis (axisValue wire "Line")

        let lineEnd =
            axisValue wire "LineEnd"
            |> Option.bind tryParseInt
            |> Option.orElse (axisValue wire "L2" |> Option.bind tryParseInt)
            |> Option.orElse lineEndFromAxis

        let container =
            CodeEditWireEncoding.appendHints [] scopeKind scopeIndex lineStart lineEnd

        CodeTarget.Symbol(
            DocumentRef.File file,
            { Container = container
              Name = memberName
              Arity = None }
        )

    let tryParseRelationSpec (wire: NormalizedBracketWire) : RelationSpec option =
        match axisValue wire "Kind" with
        | None -> None
        | Some kind ->
            match kind with
            | "CodeEdit" ->
                match axisValue wire "File" with
                | None -> None
                | Some file ->
                    match axisValue wire "Member", axisValue wire "Element" with
                    | Some memberName, None ->
                        RelationSpec.CodeEdit(codeEditMemberTarget (LogicalPath.Create file) memberName wire)
                        |> Some
                    | None, Some elementPath ->
                        let attr =
                            axisValue wire "Attribute"
                            |> Option.orElse (axisValue wire "Attr")

                        let upsertRole = axisValue wire "Role"

                        let target =
                            XmlWireEncoding.elementTarget (LogicalPath.Create file) elementPath attr upsertRole

                        Some(RelationSpec.CodeEdit target)
                    | Some _, Some _ -> None
                    | None, None ->
                        let hasScope = axisValue wire "Scope" |> Option.isSome
                        let hasLine = axisValue wire "Line" |> Option.isSome
                        let hasScopeIndex = axisValue wire "ScopeIndex" |> Option.isSome

                        if not hasScope && not hasLine && not hasScopeIndex then
                            None
                        else
                            RelationSpec.CodeEdit(codeEditMemberTarget (LogicalPath.Create file) "" wire)
                            |> Some
            | "Diag" ->
                match axisValue wire "DiagnosticId" with
                | Some id ->
                    match System.Int64.TryParse id with
                    | true, n -> RelationSpec.Diag(DiagnosticRef.mint(NumericId.ofCounter n)) |> Some
                    | _ -> None
                | None -> None
            | "Nav" ->
                let line =
                    axisValue wire "Line"
                    |> Option.bind (fun s -> System.Int32.TryParse(s) |> function true, v -> Some v | _ -> None)

                let column =
                    axisValue wire "Column"
                    |> Option.bind (fun s -> System.Int32.TryParse(s) |> function true, v -> Some v | _ -> None)

                let command = axisValue wire "Command"
                let go = axisValue wire "Go"

                let solution =
                    axisValue wire "Solution" |> Option.map LogicalPath.Create

                let seed path =
                    { Path = path
                      Line = line
                      Column = column
                      Command = command
                      Go = go
                      Solution = solution }

                match axisValue wire "File" with
                | Some path when not (System.String.IsNullOrWhiteSpace path) ->
                    RelationSpec.Nav(seed (LogicalPath.Create path)) |> Some
                | _ ->
                    match command, go with
                    | None, None -> None
                    | _ -> RelationSpec.Nav(seed LogicalPath.Empty) |> Some
            | _ -> None
