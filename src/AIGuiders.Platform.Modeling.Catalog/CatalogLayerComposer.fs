namespace AIGuiders.Platform.Modeling.Catalog

[<RequireQualifiedAccess>]
module CatalogLayerComposer =
    let compose (profile: ICatalogProfile<'Descriptor, 'Key, 'Entry>) (layers: seq<#seq<'Descriptor>>) =
        let mutable index = CatalogIndex.Empty profile.KeyComparer

        for layer in layers do
            let next = CatalogIndex.FromDescriptors(layer, profile)
            index <- index.Merge(next, profile.MergeCollisionPolicy)

        index
