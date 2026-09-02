namespace AIGuiders.Platform.Modeling.Cockpit.DataBus

/// <summary>CCU fold accumulator slot inside a channel snapshot.</summary>
type FoldSlot =
    | BuildSnapshot
    | TestsState
    | DebugSnapshot
    | GitLines
    | IdeHostState
    | StartupProjectPath

    member this.Id =
        match this with
        | BuildSnapshot -> "build-snapshot"
        | TestsState -> "tests-state"
        | DebugSnapshot -> "debug-snapshot"
        | GitLines -> "git-lines"
        | IdeHostState -> "ide-host-state"
        | StartupProjectPath -> "startup-project-path"

/// <summary>Target field on a channel DTO after fold/composition.</summary>
type ChannelField =
    | SolutionBuild
    | SolutionTests
    | SolutionDebug
    | WorkspaceGit
    | IdeHostLspHint
    | ScopeDecisionInput

    member this.Id =
        match this with
        | SolutionBuild -> "solution.build"
        | SolutionTests -> "solution.tests"
        | SolutionDebug -> "solution.debug"
        | WorkspaceGit -> "workspace.git"
        | IdeHostLspHint -> "ide-host.lsp-hint"
        | ScopeDecisionInput -> "scope-decision.input"

type ProjectionEdgeKind =
    | FoldsTo
    | ProjectsTo

type ProjectionEdge =
    { Kind: ProjectionEdgeKind
      Event: EventId
      Slot: FoldSlot option
      Field: ChannelField option }

type ProjectionGraph =
    { Channel: ChannelId
      Edges: ProjectionEdge list }

type ProjectionValidationIssue =
    { Message: string }

type ProjectionValidationResult =
    { Issues: ProjectionValidationIssue list }

    member this.IsValid = List.isEmpty this.Issues

    static member Ok = { Issues = [] }

    static member Fail message = { Issues = [ { Message = message } ] }

module ProjectionGraph =
    let private edge event kind slot field =
        { Kind = kind
          Event = event
          Slot = slot
          Field = field }

    let folds event slot =
        edge event FoldsTo (Some slot) None

    let projects event field =
        edge event ProjectsTo None (Some field)

    let validate (graph: ProjectionGraph) =
        let issues = ResizeArray()

        let knownEvents = Set.ofList EventCatalog.ideHealthEvents

        for e in graph.Edges do
            if not (Set.contains e.Event knownEvents) && graph.Channel = ChannelId.ideHealth then
                issues.Add(
                    { Message =
                        $"Event {e.Event.TypeName} is not in IdeHealth event catalog." }
                )

            match e.Kind, e.Slot, e.Field with
            | FoldsTo, None, _ -> issues.Add({ Message = $"FoldsTo edge for {e.Event.TypeName} requires Slot." })
            | ProjectsTo, _, None -> issues.Add({ Message = $"ProjectsTo edge for {e.Event.TypeName} requires Field." })
            | FoldsTo, Some _, Some _ -> issues.Add({ Message = $"FoldsTo edge for {e.Event.TypeName} must not set Field." })
            | ProjectsTo, Some _, _ -> issues.Add({ Message = $"ProjectsTo edge for {e.Event.TypeName} must not set Slot." })
            | _ -> ()

        let foldSlots =
            graph.Edges
            |> List.choose (fun e ->
                if e.Kind = FoldsTo then e.Slot else None)

        let dupes =
            foldSlots
            |> List.groupBy id
            |> List.choose (fun (slot, xs) -> if List.length xs > 1 then Some slot else None)

        for slot in dupes do
            issues.Add({ Message = $"Duplicate FoldsTo slot {slot.Id}." })

        { Issues = issues |> Seq.toList }

    let subscribedEvents (graph: ProjectionGraph) =
        graph.Edges |> List.map (fun e -> e.Event) |> List.distinct
