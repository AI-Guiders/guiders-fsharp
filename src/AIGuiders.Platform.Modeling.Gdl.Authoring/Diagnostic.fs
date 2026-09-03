namespace AIGuiders.Platform.Modeling.Gdl.Authoring

type AuthoringDiagnosticCode =
    | TopologyWireInvalid
    | MissingDeckHeader
    | MissingCatalogHeader
    | MissingGrammarDeclaration
    | GrammarWireMismatch
    | UnknownGrammarId
    | MissingTableColumn
    | UnknownSection
    | DuplicateRow
    | InvalidSyntax
    | UnknownBundle
    | UnknownProfile
    | EntryFileNotFound
    | EntryOutsideWorkspace

type AuthoringDiagnostic =
    { Code: AuthoringDiagnosticCode
      Message: string
      Line: int
      Column: int
      Section: string option }

module AuthoringDiagnostic =
    let create code message line =
        { Code = code
          Message = message
          Line = line
          Column = 0
          Section = None }
