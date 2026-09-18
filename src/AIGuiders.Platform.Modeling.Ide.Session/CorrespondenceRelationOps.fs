namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Documentation.Correspondence

/// Merge CRS doc→code witnesses into session graph G as validated correspondence relations (plan Phase 2).
module CorrespondenceRelationOps =
    /// Materialize doc→code witnesses → Relations in G; returns updated runtime + counts.
    let ingestDocToCodeWitnesses (witnesses: DocToCodeWitness array) (runtime: SessionRuntime) : SessionRuntime * int * int =
        let mutable materialized = 0
        let mutable skipped = 0
        let relations = ResizeArray()

        for witness in witnesses do
            match CorrespondenceMaterialize.tryMaterializeDocToCodeWitness witness with
            | Some relation ->
                relations.Add relation
                materialized <- materialized + 1
            | None -> skipped <- skipped + 1

        let updated = DependencyRelationOps.ingest (relations |> Seq.toList) runtime
        updated, materialized, skipped
