namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

/// Ingest LRC-shaped diagnostics into session `DiagnosticIndex` (plan §2.4.2).
module DiagnosticIndexOps =
    /// LRC boundary: `Code` carries rule id (CS0246); session ref is minted here.
    type IncomingDiagnostic =
        { Code: string
          Severity: string
          Message: string
          Doc: DocId
          Span: TextSpan
          Tags: string list
          Language: string
          SurfaceVersion: SurfaceVersion }

    let private nextCounter (index: DiagnosticIndex) =
        index
        |> Map.fold (fun acc ref _ ->
            let v = DiagnosticRef.carrier ref |> NumericId.value
            max acc v) 0L

    let ingest (incoming: IncomingDiagnostic seq) (runtime: SessionRuntime) : SessionRuntime =
        let mutable counter = nextCounter runtime.Diagnostics
        let mutable index = runtime.Diagnostics

        for item in incoming do
            counter <- counter + 1L
            let ref = DiagnosticRef.mint(NumericId.ofCounter counter)

            let diagRecord : DiagnosticRecord =
                { Code = item.Code
                  Severity = item.Severity
                  Message = item.Message
                  Doc = item.Doc
                  Span = item.Span
                  Tags = item.Tags
                  Language = item.Language
                  SurfaceVersion = item.SurfaceVersion }

            index <- Map.add ref diagRecord index

        { runtime with Diagnostics = index }

    let tryFindByCode (code: string) (index: DiagnosticIndex) =
        index
        |> Map.tryPick (fun ref record ->
            if record.Code = code then Some(ref, record) else None)
