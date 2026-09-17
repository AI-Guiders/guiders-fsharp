namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Paths

module CoreProjectId = AIGuiders.Platform.Modeling.Core.Identity.ProjectId

/// Session project id — thin alias over Core.Identity (plan §2.4).
type ProjectId = AIGuiders.Platform.Modeling.Core.Identity.ProjectId

module ProjectId =
    let create (absolutePath: string) = CoreProjectId.fromAbsolute absolutePath
    let fromLogical path = CoreProjectId.create path
    let path (ProjectId p) = p
    let value (ProjectId p) = p.Value
