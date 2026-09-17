namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

type TextReplacement =
    { DocId: DocId
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

type GraphStructurePatch =
    { DocumentAssignments: (string * ProjectId) list
      ProjectsAdded: ProjectNode list
      ProjectsRemoved: ProjectId list
      ProjectMetadataUpdates: ProjectNode list }

module GraphStructurePatch =
    let empty =
        { DocumentAssignments = []
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
                || not (List.isEmpty g.DocumentAssignments)

            if fileCrud then
                ProjectFileCrud
            elif not (List.isEmpty fs.Replacements) then
                FileChange
            else
                FileChange

    let private projectForNode (_graph: SolutionGraph) (node: GraphNodeId) =
        match node with
        | GraphNodeId.ProjectNode pid -> Some pid
        | GraphNodeId.CapabilityNode(pid, _) -> Some pid

    let private projectOfRef (node: GraphNodeRef) =
        match node with
        | GraphNodeRef.SessionProject pid -> Some pid
        | GraphNodeRef.SessionCapability(pid, _) -> Some pid
        | _ -> None

    let private applyProjectMutations (graph: SolutionGraph) (patch: GraphStructurePatch) =
        let removed = patch.ProjectsRemoved |> Set.ofList

        let graphAfterRemoval =
            if Set.isEmpty removed then
                graph
            else
                { graph with
                    Projects = graph.Projects |> List.filter (fun p -> not (Set.contains p.Id removed))
                    Relations =
                        graph.Relations
                        |> List.filter (fun r ->
                            match projectOfRef r.From, projectOfRef r.To with
                            | Some fromPid, Some toPid ->
                                not (Set.contains fromPid removed || Set.contains toPid removed)
                            | Some fromPid, None -> not (Set.contains fromPid removed)
                            | None, Some toPid -> not (Set.contains toPid removed)
                            | None, None -> true) }

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

    let apply
        (graph: SolutionGraph)
        (registry: DocumentRegistry)
        (contents: Map<DocId, DocumentText>)
        (counter: int64)
        (patch: SessionPatch)
        =
        let contentsAfterReplacements =
            (contents, patch.FileSystem.Replacements)
            ||> List.fold (fun acc repl ->
                match Map.tryFind repl.DocId acc with
                | None -> acc
                | Some (DocumentText text) ->
                    Map.add repl.DocId (DocumentText(text.Replace(repl.Old, repl.New))) acc)

        let mutable registry' = registry
        let mutable counter' = counter

        let registryAfterAssignments, counterAfterAssignments =
            DocumentRegistryOps.applyRegistryAssignments patch.Graph.DocumentAssignments registry' counter'

        registry' <- registryAfterAssignments
        counter' <- counterAfterAssignments

        let contentsAfterRenames =
            (contentsAfterReplacements, patch.FileSystem.PathRenames)
            ||> List.fold (fun acc (oldPath, newPath) ->
                match DocumentRegistryOps.resolvePath (LogicalPath.Create oldPath) registry' with
                | None -> acc
                | Some docId ->
                    registry' <- DocumentRegistryOps.applyPathRename oldPath newPath registry'
                    acc)

        let contentsAfterWrites =
            (contentsAfterRenames, patch.FileSystem.Writes)
            ||> List.fold (fun acc (path, text) ->
                match DocumentRegistryOps.resolvePath (LogicalPath.Create path) registry' with
                | None -> acc
                | Some docId -> Map.add docId (DocumentText text) acc)

        let contents' =
            (contentsAfterWrites, patch.FileSystem.Deletes)
            ||> List.fold (fun acc path ->
                match DocumentRegistryOps.resolvePath (LogicalPath.Create path) registry' with
                | None -> acc
                | Some docId -> Map.remove docId acc)

        let graph' = applyProjectMutations graph patch.Graph
        graph', registry', contents', counter'
