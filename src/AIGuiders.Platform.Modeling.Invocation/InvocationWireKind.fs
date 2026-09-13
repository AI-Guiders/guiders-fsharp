namespace AIGuiders.Platform.Modeling.Invocation

/// <summary>Which Notations branch parses user wire for this surface (ADR-0021).</summary>
type InvocationWireKind =
    | SlashPath = 1
    | ConsolePath = 2
    | KeySequence = 3
    /// <summary>MCP / direct execute — no Notations tail parser.</summary>
    | None = 0

module InvocationWireKind =

    let toWire (kind: InvocationWireKind) : string =
        match kind with
        | InvocationWireKind.SlashPath -> "slash-path"
        | InvocationWireKind.ConsolePath -> "console-path"
        | InvocationWireKind.KeySequence -> "key-sequence"
        | InvocationWireKind.None -> "none"

    let tryParse (wire: string) : InvocationWireKind option =
        if System.String.IsNullOrWhiteSpace wire then None
        else
            match wire.Trim().ToLowerInvariant() with
            | "slash-path" -> Some InvocationWireKind.SlashPath
            | "console-path" -> Some InvocationWireKind.ConsolePath
            | "key-sequence" -> Some InvocationWireKind.KeySequence
            | "none" -> Some InvocationWireKind.None
            | _ -> None
