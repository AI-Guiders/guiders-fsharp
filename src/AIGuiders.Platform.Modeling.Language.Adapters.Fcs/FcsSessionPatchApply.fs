namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open AIGuiders.Platform.Modeling.Ide.Session

/// Execution-bound applier for FCS rename-symbol patch apply (plan §7 — no File IO in Modeling).
module FcsSessionPatchApply =
    type private UnboundApplier() =
        interface IFcsSessionPatchApplier with
            member _.TryApply(_, _, _) =
                Error
                    "FCS session patch applier not bound. Load Execution.Language.Adapters.Fcs (FcsExecutionHost) first."

    let mutable private boundApplier: IFcsSessionPatchApplier option = None

    let internal activeApplier () =
        match boundApplier with
        | Some applier -> applier
        | None -> UnboundApplier() :> IFcsSessionPatchApplier

    /// Called from <c>Execution.Language.Adapters.Fcs</c> static init.
    let bindApplier (applier: IFcsSessionPatchApplier) =
        if isNull (box applier) then
            invalidArg "applier" "FCS session patch applier cannot be null."

        boundApplier <- Some applier

    type private Proxy() =
        interface IFcsSessionPatchApplier with
            member _.TryApply(anchorPath, patch, overrides) =
                activeApplier().TryApply(anchorPath, patch, overrides)

    let Default = Proxy() :> IFcsSessionPatchApplier

    let tryApply anchorPath patch overrides =
        Default.TryApply(anchorPath, patch, overrides)
