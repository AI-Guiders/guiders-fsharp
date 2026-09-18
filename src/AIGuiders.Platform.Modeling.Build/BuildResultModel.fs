namespace AIGuiders.Platform.Modeling.Build

open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

/// Raw build diagnostic as reported by the toolchain (Execution boundary).
type RawDiagnostic =
    { File: string
      Line: int
      Column: int
      Code: string
      Message: string }

/// Shaped build diagnostic — RelationSpec.Diag witness minted via DiagnosticIndex ingest (BuildDiagnosticOps).
type BuildDiagnostic =
    { File: string
      Line: int
      Column: int
      Code: string
      Message: string
      Spec: RelationSpec }
