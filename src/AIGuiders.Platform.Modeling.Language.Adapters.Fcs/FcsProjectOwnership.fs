namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

module FcsProjectOwnership =
    type private UnboundSource() =
        interface IFcsProjectOwnershipSource with
            member _.ResolveFsproj(_, _) = None

    let mutable private boundSource: IFcsProjectOwnershipSource option = None

    let internal activeSource () =
        match boundSource with
        | Some source -> source
        | None -> UnboundSource() :> IFcsProjectOwnershipSource

    let bindSource (source: IFcsProjectOwnershipSource) =
        if isNull (box source) then
            invalidArg "source" "FCS project ownership source cannot be null."

        boundSource <- Some source

    type private Proxy() =
        interface IFcsProjectOwnershipSource with
            member _.ResolveFsproj(filePath, solutionOrProjectPath) =
                activeSource().ResolveFsproj(filePath, solutionOrProjectPath)

    let Default = Proxy() :> IFcsProjectOwnershipSource

    let resolveFsproj filePath solutionOrProjectPath =
        Default.ResolveFsproj(filePath, solutionOrProjectPath)
