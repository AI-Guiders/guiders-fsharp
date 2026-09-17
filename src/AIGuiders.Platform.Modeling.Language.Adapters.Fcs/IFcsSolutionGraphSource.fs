namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Ide.Session

/// Solution graph + document ownership load — Execution binds @ startup (plan §7).
type IFcsSolutionGraphSource =
    abstract TryLoadGraph: anchorPath: string -> SolutionGraph option

    abstract TryLoadOwnership: anchorPath: string -> Map<string, ProjectId> option
