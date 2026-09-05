namespace AIGuiders.Platform.Modeling.Ide.Session

/// Physical layer IR (solution = logical, workspace = physical).
/// Units are file paths; links are navigation enrichments (md links, config refs),
/// not project references — those live in the solution layer.
type WorkspaceLink =
    { FromPath: string
      ToPath: string }

type WorkspaceGraph =
    { Root: string
      Files: string list
      Links: WorkspaceLink list }

module WorkspaceGraph =
    let create root files links =
        { Root = root
          Files = files
          Links = links }

    let hasFile path (graph: WorkspaceGraph) =
        List.contains path graph.Files
