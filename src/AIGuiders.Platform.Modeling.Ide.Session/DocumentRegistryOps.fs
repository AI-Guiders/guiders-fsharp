namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

/// Bootstrap and maintain doc identity ω on `SessionRuntime` (plan §2.4.1).
module DocumentRegistryOps =
    type BootstrapResult =
        { Registry: DocumentRegistry
          Contents: Map<DocId, DocumentText>
          NextCounter: int64 }

    let private findByPath (path: LogicalPath) (registry: DocumentRegistry) =
        registry
        |> Map.toList
        |> List.tryFind (fun (_, meta) -> meta.Path.Value = path.Value)
        |> Option.map fst

    let resolvePath (path: LogicalPath) (registry: DocumentRegistry) =
        findByPath path registry

    let tryGetPath (docId: DocId) (registry: DocumentRegistry) =
        Map.tryFind docId registry |> Option.map (fun m -> m.Path)

    /// Mint registry + contents from path-keyed bootstrap (golden tests, slnx load).
    let bootstrap (pathContents: seq<string * string>) (ownership: Map<string, ProjectId>) (startCounter: int64) =
        let mutable counter = startCounter
        let mutable registry = Map.empty
        let mutable contents = Map.empty

        for path, text in pathContents do
            let logical = LogicalPath.Create path

            let owner =
                match Map.tryFind path ownership with
                | Some id -> id
                | None ->
                    match ownership |> Map.tryFind logical.Value with
                    | Some id -> id
                    | None -> failwith $"No project owner for document path '{path}'."

            counter <- counter + 1L
            let docId = DocId.mint (NumericId.ofCounter counter)

            let meta =
                { Path = logical
                  Owner = owner
                  SurfaceVersion = SurfaceVersion 1L }

            registry <- Map.add docId meta registry
            contents <- Map.add docId (DocumentText text) contents

        { Registry = registry
          Contents = contents
          NextCounter = counter + 1L }

    let applyPathRename (oldPath: string) (newPath: string) (registry: DocumentRegistry) =
        let oldLogical = LogicalPath.Create oldPath
        let newLogical = LogicalPath.Create newPath

        match findByPath oldLogical registry with
        | None -> registry
        | Some docId ->
            registry
            |> Map.change docId (function
                | None -> None
                | Some meta -> Some { meta with Path = newLogical })

    let applyRegistryAssignments (assignments: (string * ProjectId) list) (registry: DocumentRegistry) counter =
        let mutable next = counter
        let mutable reg = registry

        for path, owner in assignments do
            let logical = LogicalPath.Create path

            match findByPath logical reg with
            | Some _ -> ()
            | None ->
                next <- next + 1L

                let docId = DocId.mint (NumericId.ofCounter next)

                reg <-
                    Map.add
                        docId
                        { Path = logical
                          Owner = owner
                          SurfaceVersion = SurfaceVersion 1L }
                        reg

        reg, next + 1L

    let ownerOfPath (path: string) (registry: DocumentRegistry) =
        let logical = LogicalPath.Create path

        registry
        |> Map.toList
        |> List.tryFind (fun (_, meta) -> meta.Path.Value = logical.Value)
        |> Option.map (fun (_, meta) -> meta.Owner)

    let pathsForProject (projectId: ProjectId) (registry: DocumentRegistry) =
        registry
        |> Map.toList
        |> List.filter (fun (_, meta) -> meta.Owner = projectId)
        |> List.map (fun (docId, meta) -> docId, meta.Path)

    let contentsForProject (projectId: ProjectId) (registry: DocumentRegistry) (contents: Map<DocId, DocumentText>) =
        pathsForProject projectId registry
        |> List.choose (fun (docId, _) -> Map.tryFind docId contents |> Option.map (fun t -> docId, t))
        |> Map.ofList

    /// Attach picker rows scoped to session document registry ω (plan §4.4).
    type RegistryPickerChoice = { Id: string; Label: string }

    let documentPathPickerChoices (registry: DocumentRegistry) : RegistryPickerChoice list =
        registry
        |> Map.toList
        |> List.map (fun (_, meta) ->
            let path = meta.Path.Value
            let name = System.IO.Path.GetFileName path

            let label =
                if System.String.IsNullOrEmpty name then path
                else $"{name} — {path}"

            { Id = path; Label = label })
        |> List.sortBy (fun choice -> choice.Label)
