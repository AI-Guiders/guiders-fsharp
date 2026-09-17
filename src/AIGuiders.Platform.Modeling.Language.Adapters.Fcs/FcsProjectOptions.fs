namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

/// Federation F# project options — Execution binds materialized host source @ startup (plan §7).
module FcsProjectOptions =
    type private UnboundSource() =
        interface IFcsProjectOptionsSource with
            member _.TryLoad _ =
                Error
                    { Message =
                        "FCS project options source not bound. Load Execution.Language.Adapters.Fcs (FcsExecutionHost) first." }

            member _.Warm _ = ()

            member _.Invalidate _ = ()

    let mutable private boundSource: IFcsProjectOptionsSource option = None

    let internal activeSource () =
        match boundSource with
        | Some source -> source
        | None -> UnboundSource() :> IFcsProjectOptionsSource

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
