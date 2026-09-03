namespace AIGuiders.Platform.Modeling.Catalog

open System
open System.Collections.Generic

[<RequireQualifiedAccess>]
module private CatalogMerge =
    let mergeEntry<'Key, 'Entry when 'Key: equality and 'Key: not null>
        (byKey: Dictionary<'Key, 'Entry>)
        (key: 'Key)
        (entry: 'Entry)
        (policy: CatalogIndexCollisionPolicy)
        =
        match policy with
        | CatalogIndexCollisionPolicy.ShipFirst -> byKey.TryAdd(key, entry) |> ignore
        | CatalogIndexCollisionPolicy.OverlayWins -> byKey[key] <- entry
        | _ -> raise (ArgumentOutOfRangeException(nameof policy, policy, null))

type CatalogIndex<'Key, 'Entry when 'Key: equality and 'Key: not null>
    (byKey: Dictionary<'Key, 'Entry>, comparer: IEqualityComparer<'Key>) =
    member _.Entries = byKey.Values :> IReadOnlyCollection<_>

    member _.Keys = byKey.Keys :> IEnumerable<_>

    member _.TryGet(key: 'Key, entry: byref<'Entry>) =
        match byKey.TryGetValue key with
        | true, value ->
            entry <- value
            true
        | false, _ ->
            entry <- Unchecked.defaultof<'Entry>
            false

    member this.Merge(overlay: CatalogIndex<'Key, 'Entry>, policy: CatalogIndexCollisionPolicy) =
        let merged = Dictionary<'Key, 'Entry>(byKey, comparer)

        for KeyValue(key, value) in overlay.ByKey do
            CatalogMerge.mergeEntry merged key value policy

        CatalogIndex(merged, comparer)

    member this.MergeShipFirst(overlay: CatalogIndex<'Key, 'Entry>) =
        this.Merge(overlay, CatalogIndexCollisionPolicy.ShipFirst)

    member this.MergeOverlayWins(overlay: CatalogIndex<'Key, 'Entry>) =
        this.Merge(overlay, CatalogIndexCollisionPolicy.OverlayWins)

    member internal _.ByKey = byKey

    static member Empty(comparer: IEqualityComparer<'Key>) =
        CatalogIndex(Dictionary<'Key, 'Entry>(comparer), comparer)

    static member FromMap(entries: IDictionary<'Key, 'Entry>, comparer: IEqualityComparer<'Key>) =
        CatalogIndex(Dictionary<'Key, 'Entry>(entries, comparer), comparer)

    static member FromDescriptors(descriptors: seq<'Descriptor>, profile: ICatalogProfile<'Descriptor, 'Key, 'Entry>) =
        let byKey = Dictionary<'Key, 'Entry>(profile.KeyComparer)

        for descriptor in descriptors do
            for key, entry in profile.Project descriptor do
                let normalized = profile.NormalizeKey key
                CatalogMerge.mergeEntry byKey normalized entry profile.LayerCollisionPolicy

        CatalogIndex(byKey, profile.KeyComparer)
