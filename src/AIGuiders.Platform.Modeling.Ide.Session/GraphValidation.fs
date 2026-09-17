namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

type GraphValidationIssue =
    { Message: string }

type GraphValidationResult =
    { Issues: GraphValidationIssue list }

    member this.IsValid = List.isEmpty this.Issues

    static member Ok = { Issues = [] }

    static member single message = { Issues = [ { Message = message } ] }

module GraphValidation =
    let private issue message = { Message = message }

    let private relationKey (r: Relation) = $"{r.Type}-{r.From}-{r.To}"

    let private requiresRelations (graph: SolutionGraph) =
        graph.Relations |> List.filter (fun r -> r.Type = RelationType.Requires)

    let private hasNodeRef (graph: SolutionGraph) (node: GraphNodeRef) =
        match node with
        | GraphNodeRef.SessionProject pid -> graph |> SolutionGraph.tryFindProject pid |> Option.isSome
        | GraphNodeRef.SessionCapability(pid, kind) ->
            match graph |> SolutionGraph.tryFindProject pid with
            | None -> false
            | Some project -> project.Capabilities |> List.exists (fun c -> c.Kind = kind)
        | _ -> false

    let private projectOfRef (node: GraphNodeRef) =
        match node with
        | GraphNodeRef.SessionProject pid -> Some pid
        | GraphNodeRef.SessionCapability(pid, _) -> Some pid
        | _ -> None

    let private detectProjectCycle (graph: SolutionGraph) =
        let adj =
            SolutionGraph.projectRefEdges graph
            |> List.groupBy (fun r ->
                match r.From with
                | GraphNodeRef.SessionProject pid -> ProjectId.value pid
                | _ -> "")
            |> List.filter (fun (k, _) -> k <> "")
            |> List.map (fun (from, edges) ->
                from,
                edges
                |> List.choose (fun r ->
                    match r.To with
                    | GraphNodeRef.SessionProject pid -> Some(ProjectId.value pid)
                    | _ -> None))
            |> Map.ofList

        let rec visit stack nodeKey =
            if Set.contains nodeKey stack then
                Some nodeKey
            else
                match Map.tryFind nodeKey adj with
                | None -> None
                | Some targets -> targets |> List.tryPick (fun t -> visit (Set.add nodeKey stack) t)

        SolutionGraph.projectRefEdges graph
        |> List.tryPick (fun r ->
            match r.From with
            | GraphNodeRef.SessionProject pid -> visit Set.empty (ProjectId.value pid)
            | _ -> None)
        |> Option.map id

    let private detectRequiresCycle (graph: SolutionGraph) =
        let requires = requiresRelations graph

        let nodeKey ref =
            match ref with
            | GraphNodeRef.SessionProject pid -> GraphNodeId.key (GraphNodeId.project pid)
            | GraphNodeRef.SessionCapability(pid, kind) -> GraphNodeId.key (GraphNodeId.capability pid kind)
            | _ -> ""

        let adj =
            requires
            |> List.groupBy (fun r -> nodeKey r.From)
            |> List.filter (fun (k, _) -> k <> "")
            |> List.map (fun (from, edges) -> from, edges |> List.map (fun r -> nodeKey r.To))
            |> Map.ofList

        let rec visit (stack: Set<string>) nodeKey =
            if Set.contains nodeKey stack then
                Some nodeKey
            else
                match Map.tryFind nodeKey adj with
                | None -> None
                | Some targets -> targets |> List.tryPick (fun t -> visit (Set.add nodeKey stack) t)

        requires
        |> List.tryPick (fun r ->
            let k = nodeKey r.From
            if k = "" then None else visit Set.empty k)
        |> Option.map id

    let validate (graph: SolutionGraph) (registry: DocumentRegistry) =
        let issues = ResizeArray()

        let projectIds =
            graph.Projects |> List.map (fun p -> p.Id)

        let dupProjectIds =
            projectIds
            |> List.groupBy id
            |> List.choose (fun (_, xs) -> if List.length xs > 1 then Some xs.Head else None)

        for pid in dupProjectIds do
            issues.Add(issue $"Duplicate project id '{ProjectId.value pid}'.")

        for project in graph.Projects do
            let dupCaps =
                project.Capabilities
                |> List.groupBy (fun c -> c.Kind)
                |> List.choose (fun (kind, xs) -> if List.length xs > 1 then Some kind else None)

            for kind in dupCaps do
                issues.Add(
                    issue
                        $"Duplicate capability '{CapabilityKind.id kind}' on project '{ProjectId.value project.Id}'."
                )

        for relation in SolutionGraph.orchestrationEdges graph do
            if not (hasNodeRef graph relation.From) then
                issues.Add(issue $"Relation '{relationKey relation}' references missing From node.")

            if not (hasNodeRef graph relation.To) then
                issues.Add(issue $"Relation '{relationKey relation}' references missing To node.")

            match projectOfRef relation.From, projectOfRef relation.To with
            | Some fromProject, Some toProject when fromProject <> toProject ->
                issues.Add(issue $"WF7: orchestration relation '{relationKey relation}' crosses project subgraphs.")
            | _ -> ()

            match RelationGraph.validateRelation relation with
            | Ok () -> ()
            | Error e -> issues.Add(issue e.Message)

        match detectRequiresCycle graph with
        | Some nodeKey -> issues.Add(issue $"Cycle detected in requires edges near node '{nodeKey}'.")
        | None -> ()

        let knownProjects = Set.ofList projectIds

        for relation in SolutionGraph.projectRefEdges graph do
            match relation.From, relation.To with
            | GraphNodeRef.SessionProject fromPid, GraphNodeRef.SessionProject toPid ->
                if not (Set.contains fromPid knownProjects) then
                    issues.Add(issue $"WF8: project edge From '{ProjectId.value fromPid}' is unknown.")

                if not (Set.contains toPid knownProjects) then
                    issues.Add(issue $"WF8: project edge To '{ProjectId.value toPid}' is unknown.")
            | _ -> issues.Add(issue $"WF8: invalid project ref relation '{relationKey relation}'.")

        match detectProjectCycle graph with
        | Some nodeKey -> issues.Add(issue $"WF8: cycle detected in project edges near '{nodeKey}'.")
        | None -> ()

        for _, meta in Map.toSeq registry do
            match graph |> SolutionGraph.tryFindProject meta.Owner with
            | None ->
                issues.Add(
                    issue
                        $"Document '{meta.Path.Value}' references missing project '{ProjectId.value meta.Owner}'."
                )
            | Some _ -> ()

        for project in graph.Projects do
            for cap in project.Capabilities do
                if cap.Attributes.Topology = Adaptive && List.isEmpty cap.Attributes.AdaptiveRules then
                    issues.Add(
                        issue
                            $"Capability '{CapabilityKind.id cap.Kind}' on '{ProjectId.value project.Id}' is Adaptive but has no rules."
                    )

        { Issues = issues |> Seq.toList }
