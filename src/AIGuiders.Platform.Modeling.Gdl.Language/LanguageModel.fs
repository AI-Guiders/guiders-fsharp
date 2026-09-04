namespace AIGuiders.Platform.Modeling.Gdl.Language

open System.Collections.Generic

/// <summary>How confidently a Locus is bound (GUIDERS-ADR-0025).</summary>
type ResolveTier =
    | Text = 0
    | Syntax = 1
    | Semantic = 2

/// <summary>Resolved location in a document buffer (range + optional symbol id).</summary>
type Locus =
    { Start: int
      End: int
      Tier: ResolveTier
      SymbolId: string option
      FilePath: string option }

module Locus =

    /// <summary>Text-tier locus (C# default args).</summary>
    let ofRange (start: int) (endPos: int) : Locus =
        { Start = start
          End = endPos
          Tier = ResolveTier.Text
          SymbolId = None
          FilePath = None }

    /// <summary>Semantic-tier locus with symbol id.</summary>
    let semantic (start: int) (endPos: int) (symbolId: string) : Locus =
        { Start = start
          End = endPos
          Tier = ResolveTier.Semantic
          SymbolId = Some symbolId
          FilePath = None }

/// <summary>Resolve input for anchor (raw wire). Prefer NormalizedBracketWire from IR.Bracket (ADR-0026).</summary>
type AnchorWire =
    { Value: string }

/// <summary>LSP-shaped single edit (language-neutral).</summary>
type TextEdit =
    { Start: int
      End: int
      NewText: string }

/// <summary>Buffer command result payload (replaces EditorBufferOutcome in Phase 1).</summary>
type BufferEditOutcome =
    { Text: string option
      SelectionStart: int option
      SelectionEnd: int option
      TextMode: string option
      Edits: IReadOnlyList<TextEdit> option }

module BufferEditOutcome =

    /// <summary>Text payload with selection (C# BufferEditOutcome.FromText).</summary>
    let fromText (text: string) (selectionStart: int) (selectionEnd: int) : BufferEditOutcome =
        { Text = Some text
          SelectionStart = Some selectionStart
          SelectionEnd = Some selectionEnd
          TextMode = None
          Edits = None }

/// <summary>EditSniper-style scope (CDP: from/till/wire/pad).</summary>
type SniperScope =
    { FromLine: int option
      TillLine: int option
      Wire: string option
      Pad: string option }

module SniperScope =

    /// <summary>Empty scope (all axes unset).</summary>
    let empty : SniperScope =
        { FromLine = None
          TillLine = None
          Wire = None
          Pad = None }