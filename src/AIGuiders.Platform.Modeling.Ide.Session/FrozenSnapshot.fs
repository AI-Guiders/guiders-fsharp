namespace AIGuiders.Platform.Modeling.Ide.Session

type SessionRevision = int64

type FreezeMode =
    | Local of ProjectId
    | ProjClosure of ProjectId
    | Solution
    | Custom of ProjectId list

type FrozenProjectSnapshot =
    { ProjectId: ProjectId
      Revision: SessionRevision
      Ownership: Map<string, ProjectId>
      Contents: Map<string, string> }

type FrozenTreeSnapshot =
    { Revision: SessionRevision
      Mode: FreezeMode
      Projects: FrozenProjectSnapshot list }

module FrozenSnapshot =
    let private dependencyClosure (graph: SolutionGraph) (root: ProjectId) =
        let byFrom =
            graph.ProjectEdges
            |> List.groupBy (fun e -> e.From)
            |> List.map (fun (k, edges) -> k, edges |> List.map (fun e -> e.To))
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
        (contents: Map<string, string>)
        (projectId: ProjectId)
        =
        let ownership =
            graph.FileOwnership
            |> Map.filter (fun _ owner -> owner = projectId)

        let projectContents =
            ownership
            |> Map.keys
            |> Seq.choose (fun path ->
                match Map.tryFind path contents with
                | None -> None
                | Some text -> Some(path, text))
            |> Map.ofSeq

        { ProjectId = projectId
          Revision = revision
          Ownership = ownership
          Contents = projectContents }

    let freezeTree
        (revision: SessionRevision)
        (graph: SolutionGraph)
        (contents: Map<string, string>)
        (mode: FreezeMode)
        =
        let projects =
            resolveProjects graph mode
            |> List.map (freezeProject revision graph contents)

        { Revision = revision
          Mode = mode
          Projects = projects }
