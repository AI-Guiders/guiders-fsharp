namespace AIGuiders.Platform.Modeling.Language

open System.Collections.Generic

/// <summary>LSP-shaped single edit (language-neutral edit plane).</summary>
type TextEdit =
    { Start: int
      End: int
      NewText: string }

/// <summary>Buffer command result payload.</summary>
type BufferEditOutcome =
    { Text: string option
      SelectionStart: int option
      SelectionEnd: int option
      TextMode: string option
      Edits: IReadOnlyList<TextEdit> option }

module BufferEditOutcome =
    let fromText (text: string) (selectionStart: int) (selectionEnd: int) : BufferEditOutcome =
        { Text = Some text
          SelectionStart = Some selectionStart
          SelectionEnd = Some selectionEnd
          TextMode = None
          Edits = None }

/// <summary>Resolve input for anchor (raw wire). Prefer NormalizedBracketWire from Notations.Bracket.</summary>
type AnchorWire = { Value: string }

/// <summary>EditSniper-style scope (CDP: from/till/wire/pad) — transitional until CodeEdit/NavSeed canon.</summary>
type SniperScope =
    { FromLine: int option
      TillLine: int option
      Wire: string option
      Pad: string option }

module SniperScope =
    let empty : SniperScope =
        { FromLine = None
          TillLine = None
          Wire = None
          Pad = None }
