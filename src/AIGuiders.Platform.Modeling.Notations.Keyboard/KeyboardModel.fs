namespace AIGuiders.Platform.Modeling.Notations.Keyboard

open System.Collections.Generic

/// <summary>
/// Modifier keys of a normalized chord (flags-style: Control|Alt etc.).
/// Numeric values follow the conventional flags layout — verify against the
/// Keyboard package wire when interop lands.
/// </summary>
type ChordModifierKeys =
    | None = 0
    | Control = 1
    | Alt = 2
    | Shift = 4
    | Meta = 8

/// <summary>One step of a normalized key sequence: chord (modifiers + key) or plain key.</summary>
type NormalizedSequenceStep =
    | ChordStep of Modifiers: ChordModifierKeys * KeySymbol: string
    | PlainKeyStep of KeySymbol: string

/// <summary>Normalized gesture wire: ordered steps (legacy NormalizedKeySequence shape).</summary>
type NormalizedKeySequence =
    { Steps: IReadOnlyList<NormalizedSequenceStep> }

module NormalizedKeySequence =

    /// <summary>Empty sequence (C# NormalizedKeySequence.Empty).</summary>
    let empty : NormalizedKeySequence =
        { Steps = [||] :> IReadOnlyList<NormalizedSequenceStep> }