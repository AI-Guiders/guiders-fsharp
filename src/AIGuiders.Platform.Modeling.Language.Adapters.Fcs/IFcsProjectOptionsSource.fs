namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open FSharp.Compiler.CodeAnalysis

[<CLIMutable>]
type FcsProjectOptionsLoadError = { Message: string }

/// Loads fsproj context for FCS semantic checks (references, defines, source files).
type IFcsProjectOptionsSource =
    abstract TryLoad: fsprojPath: string -> Result<FSharpProjectOptions, FcsProjectOptionsLoadError>

    /// Optional session warm — same load path, no separate semantics.
    abstract Warm: fsprojPath: string -> unit

    /// Drop cached options for one project or all projects in this source.
    abstract Invalidate: ?fsprojPath: string -> unit
