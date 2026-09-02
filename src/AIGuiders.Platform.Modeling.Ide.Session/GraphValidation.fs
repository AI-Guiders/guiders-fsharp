namespace AIGuiders.Platform.Modeling.Ide.Session

type GraphValidationIssue =
    { Message: string }

type GraphValidationResult =
    { Issues: GraphValidationIssue list }

    member this.IsValid = List.isEmpty this.Issues

    static member Ok = { Issues = [] }

    static member single message = { Issues = [ { Message = message } ] }

module GraphValidation =
    let private issue message = { Message = message }

    let private edgeKey (e: SessionEdge) =
        $"{e.Kind}-{e.From}-{e.To}"

    let private requiresEdges (graph: SolutionGraph) =
        graph.Edges |> List.filter (fun e -> e.Kind = Requires)

    let private hasNode (graph: SolutionGraph) (node: GraphNodeId) =
        match node with
        | GraphNodeId.ProjectNode pid -> graph |> SolutionGraph.tryFindProject pid |> Option.isSome
        | GraphNodeId.CapabilityNode(pid, kind) ->
            match graph |> SolutionGraph.tryFindProject pid with
            | None -> false
            | Some project -> project.Capabilities |> List.exists (fun c -> c.Kind = kind)

    let private projectForNode (node: GraphNodeId) =
        match node with
        | GraphNodeId.ProjectNode pid -> pid
        | GraphNodeId.CapabilityNode(pid, _) -> pid

    let private detectProjectCycle (graph: SolutionGraph) =
        let adj =
            graph.ProjectEdges
            |> List.groupBy (fun e -> ProjectId.value e.From)
            |> List.map (fun (from, edges) -> from, edges |> List.map (fun e -> ProjectId.value e.To))
            |> Map.ofList

        let rec visit stack nodeKey =
            if Set.contains nodeKey stack then
                Some nodeKey
            else
                match Map.tryFind nodeKey adj with
                | None -> None
                | Some targets -> targets |> List.tryPick (fun t -> visit (Set.add nodeKey stack) t)

        graph.ProjectEdges
        |> List.tryPick (fun e -> visit Set.empty (ProjectId.value e.From))
        |> Option.map id

    let private detectRequiresCycle (graph: SolutionGraph) =
        let requires = requiresEdges graph

        let adj =
            requires
            |> List.groupBy (fun e -> GraphNodeId.key e.From)
            |> List.map (fun (from, edges) -> from, edges |> List.map (fun e -> GraphNodeId.key e.To))
            |> Map.ofList

        let rec visit (stack: Set<string>) nodeKey =
            if Set.contains nodeKey stack then
                Some nodeKey
            else
                match Map.tryFind nodeKey adj with
                | None -> None
                | Some targets ->
                    targets
                    |> List.tryPick (fun t -> visit (Set.add nodeKey stack) t)

        requires
        |> List.tryPick (fun e -> visit Set.empty (GraphNodeId.key e.From))
        |> Option.map id

    let validate (graph: SolutionGraph) =
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

        for edge in graph.Edges do
            if not (hasNode graph edge.From) then
                issues.Add(issue $"Edge '{edgeKey edge}' references missing From node.")

            if not (hasNode graph edge.To) then
                issues.Add(issue $"Edge '{edgeKey edge}' references missing To node.")

            // WF7 — capability edges local to subgraph(π)
            if projectForNode edge.From <> projectForNode edge.To then
                issues.Add(issue $"WF7: capability edge '{edgeKey edge}' crosses project subgraphs.")

        match detectRequiresCycle graph with
        | Some nodeKey -> issues.Add(issue $"Cycle detected in requires edges near node '{nodeKey}'.")
        | None -> ()

        let knownProjects = Set.ofList projectIds

        for edge in graph.ProjectEdges do
            if not (Set.contains edge.From knownProjects) then
                issues.Add(issue $"WF8: project edge From '{ProjectId.value edge.From}' is unknown.")

            if not (Set.contains edge.To knownProjects) then
                issues.Add(issue $"WF8: project edge To '{ProjectId.value edge.To}' is unknown.")

        match detectProjectCycle graph with
        | Some nodeKey -> issues.Add(issue $"WF8: cycle detected in project edges near '{nodeKey}'.")
        | None -> ()

        for kv in graph.FileOwnership do
            let filePath, ownerId = kv.Key, kv.Value

            match graph |> SolutionGraph.tryFindProject ownerId with
            | None ->
                issues.Add(
                    issue
                        $"File ownership for '{filePath}' references missing project '{ProjectId.value ownerId}'."
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
