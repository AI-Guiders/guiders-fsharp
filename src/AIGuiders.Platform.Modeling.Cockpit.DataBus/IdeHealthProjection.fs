namespace AIGuiders.Platform.Modeling.Cockpit.DataBus

module IdeHealthProjection =
    open ProjectionGraph

    /// Canonical wiring for <c>IdeHealthSnapshotUnit</c> (ADR 0097) — declarative SSOT.
    let graph : ProjectionGraph =
        { Channel = ChannelId.ideHealth
          Edges =
            [ folds BuildStateChanged FoldSlot.BuildSnapshot
              folds TestsStateChanged FoldSlot.TestsState
              folds DebugStateChanged FoldSlot.DebugSnapshot
              folds GitStateChanged FoldSlot.GitLines
              folds IdeHostStateChanged FoldSlot.IdeHostState
              folds StartupProjectPathChanged FoldSlot.StartupProjectPath
              projects BuildStateChanged ChannelField.SolutionBuild
              projects TestsStateChanged ChannelField.SolutionTests
              projects DebugStateChanged ChannelField.SolutionDebug
              projects GitStateChanged ChannelField.WorkspaceGit
              projects IdeHostStateChanged ChannelField.IdeHostLspHint
              projects StartupProjectPathChanged ChannelField.ScopeDecisionInput ] }

    let validation = validate graph

    let subscribedEvents = subscribedEvents graph
