namespace AIGuiders.Platform.Modeling.Gdl.Parse.Catalog

open System
open AIGuiders.Platform.Modeling.Gdl.Authoring

/// Phase 2 port of Platform CatalogGrammarValidator + CatalogParseContext.ValidateChannels.
/// Keyboard/melody wire checks take pluggable predicates — the wire parsers live
/// execution-side (ADR-0002: Modeling owns rules, Execution owns parsers). With
/// `keyboardParse = None` the wire checks are skipped ( Modeling stays parser-free);
/// Execution plugs its Notations.Keyboard wrappers when consuming.
[<RequireQualifiedAccess>]
module CatalogGrammarValidator =

    /// grammarId -> wire -> (ok, looksLike hint when the wire parses under a DIFFERENT grammar).
    type WireParse = string -> string -> bool * string option

    let private nonBlank (s: string option) = s |> Option.exists (fun v -> v.Trim().Length > 0)

    let private mismatch (line: int) (target: string) (grammar: string) (wire: string) (looksLike: string option) =
        let looks =
            looksLike
            |> Option.filter (fun l -> l.Length > 0)
            |> Option.map (fun l -> $" (looks like `{l}`)")
            |> Option.defaultValue ""

        AuthoringDiagnostic.create
            GrammarWireMismatch
            $"`{target}` wire `{wire}` does not match grammar `{grammar}`{looks}."
            line

    /// Defaults + bindings/melodies wire rules (mirrors CatalogGrammarValidator.Validate).
    let validate (keyboardParse: WireParse option) (doc: CatalogDocument) : AuthoringDiagnostic list =
        let diags = ResizeArray<AuthoringDiagnostic>()

        if doc.Bindings.Length > 0 && not (nonBlank doc.Defaults.GrammarKeyboardBinding) then
            diags.Add(
                AuthoringDiagnostic.create
                    MissingGrammarDeclaration
                    "Section `bindings` requires `grammar.keyboard.binding` in defaults."
                    1)

        if doc.Melodies.Length > 0 && not (nonBlank doc.Defaults.GrammarKeyboardMelody) then
            diags.Add(
                AuthoringDiagnostic.create
                    MissingGrammarDeclaration
                    "Section `melodies` requires `grammar.keyboard.melody` in defaults."
                    1)

        match keyboardParse, doc.Defaults.GrammarKeyboardBinding with
        | Some parse, Some bg when bg.Trim().Length > 0 ->
            match doc.Defaults.BindingChordRoot with
            | Some root when root.Trim().Length > 0 ->
                let ok, looks = parse bg root

                if not ok then diags.Add(mismatch 1 "binding.chord-root" bg root looks)
            | _ -> ()

            doc.Bindings
            |> List.iteri (fun i row ->
                let ok, looks = parse bg row.Gesture

                if not ok then diags.Add(mismatch (i + 1) $"bindings row {i + 1}" bg row.Gesture looks))
        | _ -> ()

        match keyboardParse, doc.Defaults.GrammarKeyboardMelody with
        | Some parse, Some mg when mg.Trim().Length > 0 ->
            doc.Melodies
            |> List.iteri (fun i row ->
                let wire = row.Slug

                if String.IsNullOrWhiteSpace wire then
                    diags.Add(mismatch (i + 1) $"melodies row {i + 1}" mg wire None)
                else
                    // keyboard-key-gesture family: plain slug (letter/digit/_/-) always passes.
                    let slugOk =
                        mg.Equals("keyboard-key-gesture", StringComparison.OrdinalIgnoreCase)
                        && Seq.forall (fun c -> Char.IsLetterOrDigit c || c = '_' || c = '-') wire

                    let ok, looks = if slugOk then true, None else parse mg wire

                    if not ok then diags.Add(mismatch (i + 1) $"melodies row {i + 1}" mg wire looks))
        | _ -> ()

        List.ofSeq diags

    /// Channels rule (mirrors CatalogParseContext.ValidateChannels): every non-palette
    /// channel must declare an inner `grammar` block with command and argument.
    let validateChannels (doc: CatalogDocument) : AuthoringDiagnostic list =
        [ for ch in doc.Channels do
            if ch.Surface.Length > 0
               && not (ch.Surface.Equals("palette", StringComparison.OrdinalIgnoreCase)) then
                let hasCmd = ch.CommandGrammar |> Option.exists (fun g -> g.Trim().Length > 0)
                let hasArg = ch.ArgumentGrammar |> Option.exists (fun g -> g.Trim().Length > 0)

                if not hasCmd || not hasArg then
                    let name = ch.Surface + (ch.Sub |> Option.map (fun s -> "." + s) |> Option.defaultValue "")

                    AuthoringDiagnostic.create
                        MissingGrammarDeclaration
                        $"Channel `{name}` missing `grammar` block with command and argument."
                        1 ]
