namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.IO

module FcsProjectOptions =
    let private source = FcsWorkspaceMaterializedOptionsSource() :> IFcsProjectOptionsSource

    let Default = source

    let tryGet (fsprojPath: string) =
        match Default.TryLoad fsprojPath with
        | Ok options -> Some options
        | Error _ -> None

    let warm (_fsprojPath: string) = ()

    let invalidate () = Default.Invalidate()

    let invalidateProject (_fsprojPath: string) = ()

    /// <summary>Direct ProjInfo load for probes/tests only — not the federation hot path.</summary>
    let tryLoadViaProjInfo (fsprojPath: string) =
        if String.IsNullOrWhiteSpace fsprojPath || not (File.Exists fsprojPath) then
            None
        else
            let loader = FcsProbeProjectOptionsSource() :> IFcsProjectOptionsSource

            match loader.TryLoad fsprojPath with
            | Ok options -> Some options
            | Error _ -> None
