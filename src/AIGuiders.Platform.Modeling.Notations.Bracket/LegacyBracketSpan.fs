namespace AIGuiders.Platform.Modeling.Notations.Bracket

/// <summary>Transitional span carrier for Execution anchor resolvers until Kind: RelationSpec cutover.</summary>
type BracketAxisFamily =
    | None = 0
    | Csharp = 1
    | Xml = 2
    | Navigation = 3
    | Fsharp = 4
    | Json = 5

type BracketAnchorSpan =
    { File: string option
      MemberKey: string option
      LineStart: int option
      LineEnd: int option
      ScopeKind: string option
      ScopeIndex: int option
      Role: string option
      XmlPath: string option
      Attr: string option
      Family: string option
      Command: string option
      Go: string option
      NestedAnchor: BracketAnchorSpan option
      TextNeedle: string option
      TypeKey: string option }

module BracketAnchorSpan =
    let empty : BracketAnchorSpan =
        { File = None
          MemberKey = None
          LineStart = None
          LineEnd = None
          ScopeKind = None
          ScopeIndex = None
          Role = None
          XmlPath = None
          Attr = None
          Family = None
          Command = None
          Go = None
          NestedAnchor = None
          TextNeedle = None
          TypeKey = None }
