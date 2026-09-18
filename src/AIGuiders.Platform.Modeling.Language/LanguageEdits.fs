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
