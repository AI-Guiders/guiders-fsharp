namespace AIGuiders.Platform.Modeling.Ide.Session

/// <summary>Session and project lifecycle phases (GUIDERS-ADR-0062).</summary>
type LifecyclePhase =
    | Unloaded
    | DesignTime
    | CompileTime
    | RunTime
    | TestTime

module LifecyclePhase =
    let order =
        function
        | Unloaded -> 0
        | DesignTime -> 1
        | CompileTime -> 2
        | RunTime -> 3
        | TestTime -> 4

    let canAdvanceTo current target = order target >= order current
