namespace AIGuiders.Platform.Modeling.Cockpit.Cds

/// <summary>CDS eval — the pure attention-routing circuit (ADR 0036/0097, parity with
/// CDP AttentionRoutingUnit). Circuit = input → decision wiring, extracted from C#.</summary>
module CdsRouting =

    /// Known MFD pages (parity: AttentionRoutingUnit.MfdPages).
    let mfdPages = Set.ofList [ "nav"; "sys"; "chk"; "ecl"; "qrh"; "gates" ]

    /// Layout/tile verbs — not channel routes, consumed before dispatch.
    let layoutVerbs = Set.ofList [ "tiles"; "layout"; "tile"; "seats"; "seat"; "repl"; "ccl" ]

    let private norm (s: string) = s.Trim().ToLowerInvariant()

    /// Pure routing circuit: explicit MFD wins, go-verb page routes, seats-mode
    /// defaults to nav, layout verbs null out, sys/chk/ecl/gates promote to go-verb.
    let route (input: AttentionRoutingInput) : AttentionRoutingDecision =
        let mfdDefault =
            if input.SeatsMode then "nav"
            else
                match input.DefaultMfd with
                | null -> "nav"
                | d ->
                    let t = d.Trim().ToLowerInvariant()
                    if t.Length > 0 then t else "nav"

        let mutable mfd =
            match input.MfdExplicit with
            | null -> mfdDefault
            | m -> norm m

        if not (Set.contains mfd mfdPages) then
            mfd <- "nav"

        let goRaw =
            match input.GoVerb with
            | null -> None
            | g -> Some (norm g)

        let mutable goVerb = input.GoVerb
        let mutable forceNav = false

        // 1. go-verb that is an MFD page → routes to that page.
        match goRaw with
        | Some page when Set.contains page mfdPages ->
            mfd <- page
            if page = "nav" then
                forceNav <- true
                goVerb <- null
        // 2. no go-verb + explicit MFD promoted (sys/chk/ecl/gates) → becomes go-verb.
        | _ when isNull goVerb
                 && not (isNull input.MfdExplicit)
                 && (mfd = "sys" || mfd = "chk" || mfd = "ecl" || mfd = "gates") ->
            goVerb <- mfd
        | _ -> ()

        // 3. layout/tile verbs — not channel routes, null out.
        match goVerb with
        | null -> ()
        | g -> if Set.contains (norm g) layoutVerbs then goVerb <- null

        { Mfd = mfd
          GoVerb = (match goVerb with null -> "" | g -> g)
          DeskDetailNavForced = forceNav }

    /// Pure desk_detail / nav_detail resolution (parity: DeskDetailUnit.Compute).
    let resolveDeskDetail (deskDetailRaw: string | null) (focusId: string | null) : DeskDetailDecision =
        let mutable raw =
            match deskDetailRaw with
            | null -> "slim"
            | r -> let t = norm r in if t.Length = 0 then "slim" else t

        if raw = "compact" then raw <- "slim"

        match focusId with
        | null -> ()
        | f when f.Length > 0 && (raw = "slim" || raw = "omit") -> raw <- "nav"
        | _ -> ()

        if raw = "omit" then raw <- "slim"
        if not (raw = "slim" || raw = "nav" || raw = "full") then raw <- "slim"

        { DeskDetail = raw
          WantNav = raw = "nav" || raw = "full" }