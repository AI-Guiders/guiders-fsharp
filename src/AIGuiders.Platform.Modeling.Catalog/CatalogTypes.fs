namespace AIGuiders.Platform.Modeling.Catalog

open System.Collections.Generic

type CatalogIndexCollisionPolicy =
    | ShipFirst = 0
    | OverlayWins = 1

type ICatalogProfile<'Descriptor, 'Key, 'Entry when 'Key: equality and 'Key: not null> =
    abstract KeyComparer: IEqualityComparer<'Key>
    abstract LayerCollisionPolicy: CatalogIndexCollisionPolicy
    abstract MergeCollisionPolicy: CatalogIndexCollisionPolicy
    abstract Project: 'Descriptor -> IEnumerable<struct ('Key * 'Entry)>
    abstract NormalizeKey: 'Key -> 'Key
