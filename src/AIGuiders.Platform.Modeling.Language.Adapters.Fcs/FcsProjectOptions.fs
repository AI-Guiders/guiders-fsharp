namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.IO

/// Federation F# project options — Execution binds materialized host source @ startup (plan §7).
module FcsProjectOptions =
    let mutable private boundSource: IFcsProjectOptionsSource option = None

    let internal activeSource () =
        match boundSource with
        | Some source -> source
        | None ->
            FcsProbeProjectOptionsSource() :> IFcsProjectOptionsSource

    /// Called from <c>Execution.Language.Adapters.Fcs</c> static init.
    let bindSource (source: IFcsProjectOptionsSource) =
        if isNull (box source) then
            invalidArg "source" "FCS project options source cannot be null."

        boundSource <- Some source

    type private Proxy() =
        interface IFcsProjectOptionsSource with
            member _.TryLoad fsprojPath = activeSource().TryLoad fsprojPath

            member _.Warm fsprojPath = activeSource().Warm fsprojPath

            member _.Invalidate(?fsprojPath: string) =
                match fsprojPath with
                | Some path -> activeSource().Invalidate(path)
                | None -> activeSource().Invalidate()

    let Default = Proxy() :> IFcsProjectOptionsSource

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
