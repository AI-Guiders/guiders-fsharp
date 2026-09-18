namespace AIGuiders.Platform.Modeling.CodeCenter

/// Instrumentation scope for perf SLA (D35 / V15).
type RefreshScope =
    | Point
    | Region
    | Document

module RefreshScope =
    let ofEditScope =
        function
        | EditScope.Point -> Point
        | EditScope.Region -> Region
        | EditScope.Document -> Document
