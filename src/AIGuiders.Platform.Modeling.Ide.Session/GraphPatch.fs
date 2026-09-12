namespace AIGuiders.Platform.Modeling.Ide.Session

type TextReplacement =
    { Path: string
      Old: string
      New: string }

type FileSystemPatch =
    { Replacements: TextReplacement list
      PathRenames: (string * string) list
      Writes: (string * string) list
      Deletes: string list }

module FileSystemPatch =
    let empty =
        { Replacements = []
          PathRenames = []
          Writes = []
          Deletes = [] }

/// <summary>Graph structure mutations beyond file ownership — §5.2 operational CRUD ladder.</summary>
/// based on adr: docs/math/ide-session/02-invalidation.md §5.2
type GraphStructurePatch =
    { FileOwnershipUpdates: (string * ProjectId) list
      ProjectsAdded: ProjectNode list
      ProjectsRemoved: ProjectId list
      ProjectMetadataUpdates: ProjectNode list }

module GraphStructurePatch =
    let empty =
        { FileOwnershipUpdates = []
          ProjectsAdded = []
          ProjectsRemoved = []
          ProjectMetadataUpdates = [] }

type SessionPatch =
    { FileSystem: FileSystemPatch
      Graph: GraphStructurePatch }

module SessionPatch =
    let empty =
        { FileSystem = FileSystemPatch.empty
          Graph = GraphStructurePatch.empty }

    /// §5.2 scope for orchestrator invalidation after apply.
    /// based on adr: docs/math/ide-session/02-invalidation.md §5.2
    let scope (patch: SessionPatch) : InvalidationScope =
        let fs = patch.FileSystem
        let g = patch.Graph

        if not (List.isEmpty g.ProjectsAdded) || not (List.isEmpty g.ProjectsRemoved) then
            SolutionProjectCrud
        elif not (List.isEmpty g.ProjectMetadataUpdates) then
            ProjectCrud
        else
            let fileCrud =
                not (List.isEmpty fs.PathRenames)
                || not (List.isEmpty fs.Writes)
                || not (List.isEmpty fs.Deletes)
                || not (List.isEmpty g.FileOwnershipUpdates)

            if fileCrud then
                ProjectFileCrud
            elif not (List.isEmpty fs.Replacements) then
                FileChange
            else
                FileChange

    let private projectForNode (graph: SolutionGraph) (node: GraphNodeId) =
        match node with
        | GraphNodeId.ProjectNode pid -> Some pid
        | GraphNodeId.CapabilityNode(pid, _) -> Some pid

    let private applyProjectMutations (graph: SolutionGraph) (patch: GraphStructurePatch) =
        let removed = patch.ProjectsRemoved |> Set.ofList

        let graphAfterRemoval =
            if Set.isEmpty removed then
                graph
            else
                { graph with
                    Projects = graph.Projects |> List.filter (fun p -> not (Set.contains p.Id removed))
                    FileOwnership =
                        graph.FileOwnership
                        |> Map.filter (fun _ owner -> not (Set.contains owner removed))
                    ProjectEdges =
                        graph.ProjectEdges
                        |> List.filter (fun e -> not (Set.contains e.From removed || Set.contains e.To removed))
                    Edges =
                        graph.Edges
                        |> List.filter (fun e ->
                            match projectForNode graph e.From, projectForNode graph e.To with
                            | Some fromPid, Some toPid ->
                                not (Set.contains fromPid removed || Set.contains toPid removed)
                            | _ -> true) }

        let graphAfterAdds =
            if List.isEmpty patch.ProjectsAdded then
                graphAfterRemoval
            else
                { graphAfterRemoval with
                    Projects = graphAfterRemoval.Projects @ patch.ProjectsAdded }

        if List.isEmpty patch.ProjectMetadataUpdates then
            graphAfterAdds
        else
            let updates =
                patch.ProjectMetadataUpdates
                |> List.map (fun p -> p.Id, p)
                |> Map.ofList

            { graphAfterAdds with
                Projects =
                    graphAfterAdds.Projects
                    |> List.map (fun p ->
                        match Map.tryFind p.Id updates with
                        | None -> p
                        | Some updated -> updated) }

    let apply (graph: SolutionGraph) (contents: Map<string, string>) (patch: SessionPatch) =
        let contentsAfterReplacements =
            (contents, patch.FileSystem.Replacements)
            ||> List.fold (fun acc repl ->
                match Map.tryFind repl.Path acc with
                | None -> acc
                | Some text -> Map.add repl.Path (text.Replace(repl.Old, repl.New)) acc)

        let contentsAfterWrites =
            (contentsAfterReplacements, patch.FileSystem.Writes)
            ||> List.fold (fun acc (path, text) -> Map.add path text acc)

        let contentsAfterRenames, ownershipAfterRenames =
            ((contentsAfterWrites, graph.FileOwnership), patch.FileSystem.PathRenames)
            ||> List.fold (fun (accContents, accOmega) (oldPath, newPath) ->
                match Map.tryFind oldPath accContents with
                | None -> accContents, accOmega
                | Some text ->
                    let owner =
                        match Map.tryFind oldPath accOmega with
                        | Some id -> id
                        | None -> failwith $"Path rename '{oldPath}' → '{newPath}' has no ω owner."

                    Map.remove oldPath accContents |> Map.add newPath text,
                    accOmega |> Map.remove oldPath |> Map.add newPath owner)

        let contents' =
            (contentsAfterRenames, patch.FileSystem.Deletes)
            ||> List.fold (fun acc path -> Map.remove path acc)

        let ownershipAfterFileUpdates =
            (ownershipAfterRenames, patch.Graph.FileOwnershipUpdates)
            ||> List.fold (fun acc (path, owner) -> Map.add path owner acc)

        let graphAfterFileUpdates = { graph with FileOwnership = ownershipAfterFileUpdates }
        let graph' = applyProjectMutations graphAfterFileUpdates patch.Graph
        graph', contents'
