namespace AIGuiders.Platform.Modeling.Build

open System

/// Raw build diagnostic as reported by the toolchain (Execution side).
[<CLIMutable>]
type RawDiagnostic =
    { File: string
      Line: int
      Column: int
      Code: string
      Message: string }

/// Shaped build diagnostic with resolved anchor wire — sniper-ready, no line guessing.
[<CLIMutable>]
type BuildDiagnostic =
    { File: string
      Line: int
      Column: int
      Code: string
      Message: string
      Anchor: string }

[<RequireQualifiedAccess>]
module BuildDiagnostics =

    /// Shape raw diagnostics with anchor wires — [F:file;L:line] line_literal (GUIDERS-ADR-0021).
    let shape (raw: RawDiagnostic[]) : BuildDiagnostic[] =
        raw
        |> Array.map (fun r ->
            { File = r.File
              Line = r.Line
              Column = r.Column
              Code = r.Code
              Message = r.Message
              Anchor = $"[F:{r.File};L:{r.Line}]" })
