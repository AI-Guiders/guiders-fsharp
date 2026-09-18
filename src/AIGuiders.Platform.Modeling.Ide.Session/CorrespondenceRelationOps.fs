namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Documentation.Correspondence

/// Merge CRS reverse anchors into session graph G as validated correspondence relations (plan Phase 2).
module CorrespondenceRelationOps =
    /// Materialize reverse anchors → Relations in G; returns updated runtime + counts.
    let ingestReverseAnchors (anchors: ReverseAnchor array) (runtime: SessionRuntime) : SessionRuntime * int * int =
        let mutable materialized = 0
        let mutable skipped = 0
        let relations = ResizeArray()

        for anchor in anchors do
            match CorrespondenceMaterialize.tryMaterializeReverseAnchor anchor with
            | Some relation ->
                relations.Add relation
                materialized <- materialized + 1
            | None -> skipped <- skipped + 1

        let updated = DependencyRelationOps.ingest (relations |> Seq.toList) runtime
        updated, materialized, skipped
