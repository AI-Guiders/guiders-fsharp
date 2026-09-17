namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

/// Merge Execution-ingested E_dep / correspondence relations into session graph G (plan §2.3).
module DependencyRelationOps =
    let ingest (incoming: Relation list) (runtime: SessionRuntime) =
        let graph' = SolutionGraph.mergeRelations incoming runtime.Session.Graph

        { runtime with
            Session = { runtime.Session with Graph = graph' } }
