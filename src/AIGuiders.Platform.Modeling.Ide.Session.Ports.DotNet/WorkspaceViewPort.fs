namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet

open AIGuiders.Platform.Modeling.Ide.Session

/// <summary>π_ws : φ_r(T) → WorkspaceView (§2.8b).</summary>
module WorkspaceViewPort =
    let private languageId (project: ProjectNode) =
        match project.Kind with
        | DotNet { Language = CSharp } -> "csharp"
        | DotNet { Language = FSharp } -> "fsharp"
        | Node _ -> "typescript"
        | Gdl _ -> "gdl"
        | Planet { LanguageId = lid } -> lid

    let private contentsUnion (frozen: FrozenTreeSnapshot) =
        frozen.Projects
        |> List.fold (fun acc snap -> Map.fold (fun m k v -> Map.add k v m) acc snap.Contents) Map.empty

    let emit (graph: SolutionGraph) (rootProjectId: ProjectId) (frozen: FrozenTreeSnapshot) : WorkspaceView =
        let projects =
            frozen.Projects
            |> List.choose (fun snap ->
                SolutionGraph.tryFindProject snap.ProjectId graph
                |> Option.map (fun project ->
                    { ProjectId = snap.ProjectId
                      ProjectPath = project.AbsolutePath
                      LanguageId = languageId project }))

        { Revision = frozen.Revision
          AnchorPath = graph.AnchorPath
          Mode = frozen.Mode
          RootProjectId = rootProjectId
          Projects = projects
          Contents = contentsUnion frozen }
