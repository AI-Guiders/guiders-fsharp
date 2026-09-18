namespace AIGuiders.Platform.Modeling.Ide.Session

open System
open AIGuiders.Platform.Modeling.Build
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

/// Build toolchain diagnostics → session DiagnosticIndex + RelationSpec.Diag witnesses (plan §2.4.2).
module BuildDiagnosticOps =
    type IngestResult =
        { Runtime: SessionRuntime
          Diagnostics: BuildDiagnostic list
          SkippedUnregistered: int }

    let private severityForCode (code: string) =
        if code.StartsWith("warning", StringComparison.OrdinalIgnoreCase) then "warning"
        else "error"

    let private tryIncoming (raw: RawDiagnostic) (runtime: SessionRuntime) =
        let logical = LogicalPath.Create raw.File

        match DocumentRegistryOps.resolvePath logical runtime.Registry with
        | Some docId ->
            match Map.tryFind docId runtime.Registry with
            | Some meta ->
                let incoming : DiagnosticIndexOps.IncomingDiagnostic =
                    { Code = raw.Code
                      Severity = severityForCode raw.Code
                      Message = raw.Message
                      Doc = docId
                      Span =
                        { StartLine = raw.Line
                          StartCol = raw.Column
                          EndLine = raw.Line
                          EndCol = raw.Column }
                      Tags = []
                      Language = "build"
                      SurfaceVersion = meta.SurfaceVersion }

                Some incoming
            | None -> None
        | None -> None

    /// Ingest build diagnostics into DiagnosticIndex; Spec carries minted DiagnosticRef (not array-index stub).
    let ingest (raw: RawDiagnostic[]) (runtime: SessionRuntime) : IngestResult =
        let incoming = ResizeArray()
        let alignedRaw = ResizeArray()
        let mutable skipped = 0

        for item in raw do
            match tryIncoming item runtime with
            | Some mapped ->
                incoming.Add mapped
                alignedRaw.Add item
            | None -> skipped <- skipped + 1

        let updated, refs = DiagnosticIndexOps.ingestMapped (incoming |> Seq.toList) runtime

        let shaped =
            List.zip (alignedRaw |> Seq.toList) refs
            |> List.map (fun (rawItem, ref) ->
                { File = rawItem.File
                  Line = rawItem.Line
                  Column = rawItem.Column
                  Code = rawItem.Code
                  Message = rawItem.Message
                  Spec = RelationSpec.Diag ref })

        { Runtime = updated
          Diagnostics = shaped
          SkippedUnregistered = skipped }
