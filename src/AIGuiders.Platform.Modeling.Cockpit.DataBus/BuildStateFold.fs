namespace AIGuiders.Platform.Modeling.Cockpit.DataBus

type BuildStateSnapshot =
    { IsBuilding: bool
      LastExitCode: int option
      LastBuildSucceeded: bool option }

    static member Empty =
        { IsBuilding = false
          LastExitCode = None
          LastBuildSucceeded = None }

module BuildStateFold =
    /// Parity with <c>BuildStateSnapshotUnit.Apply</c> in Platform.Cockpit.Channels.
    let apply (prior: BuildStateSnapshot) (event: BuildStateChanged) =
        if event.IsBuilding then
            { prior with IsBuilding = true }
        elif event.LastExitCode.IsSome || event.LastBuildSucceeded.IsSome then
            { IsBuilding = false
              LastExitCode = event.LastExitCode |> Option.orElse prior.LastExitCode
              LastBuildSucceeded = event.LastBuildSucceeded |> Option.orElse prior.LastBuildSucceeded }
        else
            { prior with IsBuilding = false }
