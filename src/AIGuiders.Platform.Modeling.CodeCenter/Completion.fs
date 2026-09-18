namespace AIGuiders.Platform.Modeling.CodeCenter

type CompletionItem =
    { Label: string
      Kind: string
      Edit: StructuralEdit option }

type StructuralCompletionItem =
    { Label: string
      Description: string
      Edit: StructuralEdit }

type DocumentCompletions = DocumentSnapshot -> SessionAnchor -> CompletionItem list

type DocumentStructuralCompletions = DocumentSnapshot -> SessionAnchor -> StructuralCompletionItem list

module Completion =
    let empty : DocumentCompletions = fun _ _ -> []

    let emptyStructural : DocumentStructuralCompletions = fun _ _ -> []
