namespace AIGuiders.Platform.Modeling.Gdl.Language

/// <summary>CDP/CIDE anchor span families — code (csharp, fsharp, json) / xml / navigation (CIDE 0128 / 0186).</summary>
type BracketAxisFamily =
    | None = 0
    | Csharp = 1
    | Xml = 2
    | Navigation = 3
    | Fsharp = 4
    | Json = 5

/// <summary>Anchor span: axes carrier shared by all families (csharp M/L/S/K, fcs, X/A, J, navigation).</summary>
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
      TextNeedle: string option }

module BracketAnchorSpan =

    /// <summary>Empty span (all axes unset).</summary>
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
          TextNeedle = None }