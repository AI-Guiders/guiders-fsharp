namespace AIGuiders.Platform.Modeling.Cockpit.DataBus

open System

[<CLIMutable>]
type BuildStateSnapshot =
    { IsBuilding: bool
      LastExitCode: Nullable<int>
      LastBuildSucceeded: Nullable<bool> }

    static member Empty =
        { IsBuilding = false
          LastExitCode = Nullable()
          LastBuildSucceeded = Nullable() }

module BuildStateFold =
    let private hasValue (n: Nullable<int>) = n.HasValue
    let private hasBool (n: Nullable<bool>) = n.HasValue

    let private coalesceInt (next: Nullable<int>) (prior: Nullable<int>) =
        if next.HasValue then next else prior

    let private coalesceBool (next: Nullable<bool>) (prior: Nullable<bool>) =
        if next.HasValue then next else prior

    /// Parity with legacy <c>BuildStateSnapshotUnit.Apply</c> in Platform Execution channels.
    let apply (prior: BuildStateSnapshot) (event: BuildStateChanged) =
        if event.IsBuilding then
            { prior with IsBuilding = true }
        elif hasValue event.LastExitCode || hasBool event.LastBuildSucceeded then
            { IsBuilding = false
              LastExitCode = coalesceInt event.LastExitCode prior.LastExitCode
              LastBuildSucceeded = coalesceBool event.LastBuildSucceeded prior.LastBuildSucceeded }
        else
            { prior with IsBuilding = false }
