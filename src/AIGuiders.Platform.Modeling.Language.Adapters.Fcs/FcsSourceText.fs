namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

module FcsSourceText =
    type private UnboundSource() =
        interface IFcsSourceTextSource with
            member _.TryRead _ = None

    let mutable private boundSource: IFcsSourceTextSource option = None

    let internal activeSource () =
        match boundSource with
        | Some source -> source
        | None -> UnboundSource() :> IFcsSourceTextSource

    let bindSource (source: IFcsSourceTextSource) =
        if isNull (box source) then
            invalidArg "source" "FCS source text source cannot be null."

        boundSource <- Some source

    type private Proxy() =
        interface IFcsSourceTextSource with
            member _.TryRead path = activeSource().TryRead path

    let Default = Proxy() :> IFcsSourceTextSource

    let tryRead path =
        match Default.TryRead path with
        | Some text -> Some text
        | None -> None
