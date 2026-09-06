namespace AIGuiders.Platform.Modeling.Cockpit.Channels

/// <summary>CCU decision: solution vs project scope (ADR 0095 fold shape).</summary>
[<CLIMutable>]
type IdeHealthScopeDecision =
    { IsProjectScope: bool
      ProjectPath: string }

/// <summary>One segment line before mapping to display (ADR 0089).</summary>
[<CLIMutable>]
type IdeHealthSegmentLine =
    { LineText: string
      CockpitShort: string
      IsBuildRunning: bool
      BuildFingerprint: string }

module IdeHealth =

    /// Decision algebra: project scope iff startup project is set AND a project signal exists.
    let decideScope (startupProjectPath: string) (isBuilding: bool) (hasTestSignal: bool) =
        let hasStartup = not (System.String.IsNullOrWhiteSpace startupProjectPath)
        let hasProjectSignal = isBuilding || hasTestSignal
        if hasStartup && hasProjectSignal then
            { IsProjectScope = true; ProjectPath = startupProjectPath }
        else
            { IsProjectScope = false; ProjectPath = "" }

    /// CCU decision: IDE Health summary from snapshot (idle / running / paused).
    let summarizeDebug (hasActiveSession: bool) (isExecutionStopped: bool) (stackFrameCount: int) (variableCount: int) =
        if not hasActiveSession then "idle"
        elif not isExecutionStopped then "running\u2026"
        else $"paused \u00B7 frames {stackFrameCount}, vars {variableCount}"