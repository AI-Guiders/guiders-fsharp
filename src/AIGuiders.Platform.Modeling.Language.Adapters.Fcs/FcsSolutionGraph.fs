namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Ide.Session

module FcsSolutionGraph =
    type private UnboundSource() =
        interface IFcsSolutionGraphSource with
            member _.TryLoadGraph _ = None
            member _.TryLoadOwnership _ = None

    let mutable private boundSource: IFcsSolutionGraphSource option = None

    let internal activeSource () =
        match boundSource with
        | Some source -> source
        | None -> UnboundSource() :> IFcsSolutionGraphSource

    let bindSource (source: IFcsSolutionGraphSource) =
        if isNull (box source) then
            invalidArg "source" "FCS solution graph source cannot be null."

        boundSource <- Some source

    type private Proxy() =
        interface IFcsSolutionGraphSource with
            member _.TryLoadGraph anchorPath = activeSource().TryLoadGraph anchorPath

            member _.TryLoadOwnership anchorPath = activeSource().TryLoadOwnership anchorPath

    let Default = Proxy() :> IFcsSolutionGraphSource

    let tryLoadGraph anchorPath = Default.TryLoadGraph anchorPath

    let tryLoadOwnership anchorPath = Default.TryLoadOwnership anchorPath
