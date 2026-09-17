namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

type SessionRevision = int64

type FreezeMode =
    | Local of ProjectId
    | ProjClosure of ProjectId
    | Solution
    | Custom of ProjectId list

type FrozenProjectSnapshot =
    { ProjectId: ProjectId
      Revision: SessionRevision
      Capabilities: CapabilityNode list
      Documents: Map<DocId, DocumentText> }

type FrozenTreeSnapshot =
    { Revision: SessionRevision
      Mode: FreezeMode
      Projects: FrozenProjectSnapshot list }

module FrozenSnapshot =
    let private dependencyClosure (graph: SolutionGraph) (root: ProjectId) =
        let byFrom =
            SolutionGraph.projectRefEdges graph
            |> List.choose (fun r ->
                match r.From, r.To with
                | GraphNodeRef.SessionProject fromPid, GraphNodeRef.SessionProject toPid -> Some(fromPid, toPid)
                | _ -> None)
            |> List.groupBy fst
            |> List.map (fun (k, edges) -> k, edges |> List.map snd)
            |> Map.ofList

        let rec visit seen queue =
            match queue with
            | [] -> seen |> Set.toList
            | id :: rest when Set.contains id seen -> visit seen rest
            | id :: rest ->
                let seen' = Set.add id seen

                let deps =
                    match Map.tryFind id byFrom with
                    | None -> []
                    | Some xs -> xs

                visit seen' (rest @ deps)

        visit Set.empty [ root ]

    let resolveProjects (graph: SolutionGraph) (mode: FreezeMode) =
        match mode with
        | Local id -> [ id ]
        | ProjClosure root -> dependencyClosure graph root
        | Solution -> graph.Projects |> List.map (fun p -> p.Id)
        | Custom ids -> ids

    let private freezeProject
        (revision: SessionRevision)
        (graph: SolutionGraph)
        (registry: DocumentRegistry)
        (contents: Map<DocId, DocumentText>)
        (projectId: ProjectId)
        =
        let capabilities =
            graph.Projects
            |> List.tryFind (fun p -> p.Id = projectId)
            |> Option.map (fun n -> n.Capabilities)
            |> Option.defaultValue []

        let projectContents = DocumentRegistryOps.contentsForProject projectId registry contents

        { ProjectId = projectId
          Revision = revision
          Capabilities = capabilities
          Documents = projectContents }

    let freezeTree
        (revision: SessionRevision)
        (graph: SolutionGraph)
        (registry: DocumentRegistry)
        (contents: Map<DocId, DocumentText>)
        (mode: FreezeMode)
        =
        let projects =
            resolveProjects graph mode
            |> List.map (freezeProject revision graph registry contents)

        { Revision = revision
          Mode = mode
          Projects = projects }
