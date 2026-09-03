namespace AIGuiders.Platform.Modeling.Ide.Session

/// <summary>Runtime invalidation scope (§5.2 variant D), coarsest to finest promotion.</summary>
type InvalidationScope =
    | FileChange
    | ProjectFileCrud
    | ProjectCrud
    | SolutionProjectCrud

module InvalidationScope =
    let rank =
        function
        | FileChange -> 0
        | ProjectFileCrud -> 1
        | ProjectCrud -> 2
        | SolutionProjectCrud -> 3

    /// True when <paramref name="incoming" /> is strictly coarser than <paramref name="current" />.
    let promotes (current: InvalidationScope) (incoming: InvalidationScope) =
        rank incoming > rank current

    let max scopeA scopeB =
        if rank scopeA >= rank scopeB then scopeA else scopeB
