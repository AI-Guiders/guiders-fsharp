namespace AIGuiders.Platform.Modeling.Gdl.Command

open System
open System.Collections.Generic

/// <summary>Parse ArgTail wire strings (Forge + CIDE TOML).</summary>
[<RequireQualifiedAccess>]
module CommandArgTailPolicy =

    [<Literal>]
    let ImplicitSelection = "implicit:selection"

    [<Literal>]
    let ImplicitLineRange = "implicit:line_range"

    /// <summary>Wire string → kind; unknown/empty falls back to Optional (C# Parse).</summary>
    let parse (raw: string) : CommandArgTailKind =
        if String.IsNullOrWhiteSpace raw then CommandArgTailKind.Optional
        else
            let t = raw.Trim()
            if t.Equals("none", StringComparison.OrdinalIgnoreCase) then CommandArgTailKind.None
            elif t.Equals("required", StringComparison.OrdinalIgnoreCase) then CommandArgTailKind.Required
            elif t.Equals("optional", StringComparison.OrdinalIgnoreCase) then CommandArgTailKind.Optional
            elif t.Equals(ImplicitSelection, StringComparison.OrdinalIgnoreCase) then CommandArgTailKind.ImplicitSelection
            elif t.Equals(ImplicitLineRange, StringComparison.OrdinalIgnoreCase) then CommandArgTailKind.ImplicitLineRange
            elif t.StartsWith("suggest:", StringComparison.OrdinalIgnoreCase)
                 || t.StartsWith("picker+constructor:", StringComparison.OrdinalIgnoreCase)
                 || t.StartsWith("picker:", StringComparison.OrdinalIgnoreCase) then CommandArgTailKind.Picker
            else CommandArgTailKind.Optional

    /// <summary>Descriptor slot: CommandArgTailKind = Parse(ArgTail) (C# computed property).</summary>
    let kindOf (d: CommandDescriptor) : CommandArgTailKind = parse d.ArgTail

    /// <summary>Commit-time auto-run gate (C# ShouldAutoRunOnCommit).</summary>
    let shouldAutoRunOnCommit (kind: CommandArgTailKind) (isExactPath: bool) (endsWithSpace: bool) (hasArgTail: bool) : bool =
        match kind with
        | CommandArgTailKind.None -> isExactPath
        | CommandArgTailKind.Optional -> isExactPath || endsWithSpace || hasArgTail
        | CommandArgTailKind.Required -> hasArgTail
        | CommandArgTailKind.Picker -> endsWithSpace || hasArgTail
        | CommandArgTailKind.ImplicitSelection -> isExactPath
        | CommandArgTailKind.ImplicitLineRange -> isExactPath || hasArgTail
        | _ -> false

    /// <summary>Only None inserts the trailing space on commit (C# InsertsTrailingSpaceOnCommit).</summary>
    let insertsTrailingSpaceOnCommit (kind: CommandArgTailKind) : bool =
        kind = CommandArgTailKind.None

    /// <summary>Pick the id out of picker:/suggest:/picker+constructor: tails (C# ExtractSuggestionId).</summary>
    let extractSuggestionId (raw: string) : string option =
        if String.IsNullOrWhiteSpace raw then None
        else
            let text = raw.Trim()
            let rest =
                if text.StartsWith("picker+constructor:", StringComparison.OrdinalIgnoreCase) then
                    Some (text.Substring("picker+constructor:".Length).Trim())
                elif text.StartsWith("suggest:", StringComparison.OrdinalIgnoreCase) then
                    Some (text.Substring("suggest:".Length).Trim())
                elif text.StartsWith("picker:", StringComparison.OrdinalIgnoreCase) then
                    Some (text.Substring("picker:".Length).Trim())
                else None
            match rest with
            | None -> None
            | Some tail ->
                let plus = tail.IndexOf '+'
                let id = if plus < 0 then tail else tail.Substring(0, plus).Trim()
                if id.Length = 0 then None else Some id

    let extractPickerId (raw: string) : string option = extractSuggestionId raw

    /// <summary>True when the tail opens the composite picker+constructor menu.</summary>
    let isCompositePickerConstructor (raw: string) : bool =
        not (String.IsNullOrWhiteSpace raw)
        && raw.Trim().StartsWith("picker+constructor:", StringComparison.OrdinalIgnoreCase)

    /// <summary>Constructor ids after the slot segment (C# ExtractCompositeConstructorIds).</summary>
    let extractCompositeConstructorIds (raw: string) : IReadOnlyList<string> =
        if not (isCompositePickerConstructor raw) then [||] :> IReadOnlyList<string>
        else
            let text = raw.Trim().Substring("picker+constructor:".Length).Trim()
            let parts =
                text.Split('+', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries)
            if parts.Length <= 1 then [||] :> IReadOnlyList<string>
            else (parts.[1..] :> IReadOnlyList<string>)

    /// <summary>Static enum pickers are ids starting with "enum" (C# IsStaticEnumPicker).</summary>
    let isStaticEnumPicker (raw: string) : bool =
        match extractPickerId raw with
        | Some id -> id.StartsWith("enum", StringComparison.OrdinalIgnoreCase)
        | None -> false