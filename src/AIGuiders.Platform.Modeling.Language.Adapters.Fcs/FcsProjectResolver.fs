namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

/// <summary>Resolve owning fsproj — delegates to Execution-bound <see cref="IFcsProjectOwnershipSource"/>.</summary>
module FcsProjectResolver =
    let resolveFsproj (filePath: string) (solutionOrProjectPath: string) =
        FcsProjectOwnership.resolveFsproj filePath solutionOrProjectPath
