namespace AIGuiders.Platform.Modeling.Build

open System
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

/// Raw build diagnostic as reported by the toolchain (Execution side).
[<CLIMutable>]
type RawDiagnostic =
    { File: string
      Line: int
      Column: int
      Code: string
      Message: string }

/// Shaped build diagnostic — RelationSpec.Diag witness (no legacy F/L anchor wires).
[<CLIMutable>]
type BuildDiagnostic =
    { File: string
      Line: int
      Column: int
      Code: string
      Message: string
      Spec: RelationSpec }

[<RequireQualifiedAccess>]
module BuildDiagnostics =
    let private specFor (index: int) (_raw: RawDiagnostic) =
        RelationSpec.Diag(DiagnosticRef.mint(NumericId.ofCounter (int64 index)))

    /// Shape raw diagnostics into RelationSpec.Diag witnesses for session ingest.
    let shape (raw: RawDiagnostic[]) : BuildDiagnostic[] =
        raw
        |> Array.mapi (fun i r ->
            { File = r.File
              Line = r.Line
              Column = r.Column
              Code = r.Code
              Message = r.Message
              Spec = specFor i r })
