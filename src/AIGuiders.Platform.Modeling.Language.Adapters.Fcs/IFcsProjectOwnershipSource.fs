namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

/// Owning fsproj resolution (walk-up, graph, DotNetWorkspace) — Execution binds @ startup (plan §7).
type IFcsProjectOwnershipSource =
    abstract ResolveFsproj: filePath: string * solutionOrProjectPath: string -> string option
