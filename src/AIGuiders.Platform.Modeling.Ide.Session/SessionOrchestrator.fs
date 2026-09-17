namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

module SessionOrchestrator =
    let create (session: SolutionSession) (pathContents: seq<string * string>) (ownership: Map<string, ProjectId>) =
        SessionBootstrap.createFromPaths session pathContents ownership

    let preview (runtime: SessionRuntime) (patch: SessionPatch) =
        let graph', registry', contents', _ =
            SessionPatch.apply runtime.Session.Graph runtime.Registry runtime.Contents runtime.DocIdCounter patch

        graph', registry', contents'

    let applyPatch (runtime: SessionRuntime) (patch: SessionPatch) (gitPin: GitPin) =
        let scope = SessionPatch.scope patch

        let graph', registry', contents', counter' =
            SessionPatch.apply runtime.Session.Graph runtime.Registry runtime.Contents runtime.DocIdCounter patch

        match GraphValidation.validate graph' registry' with
        | result when not result.IsValid ->
            PatchRejected(result.Issues |> List.map (fun i -> i.Message))
        | _ ->
            let materialized' =
                MaterializedState.Invalidation.forScope scope graph' registry' patch runtime.Materialized

            let ledger' =
                RevisionLedger.append scope (TransformClass.Refactor "refactor") patch gitPin runtime.Ledger

            let session' = { runtime.Session with Graph = graph' }

            PatchApplied
                { runtime with
                    Session = session'
                    Registry = registry'
                    Contents = contents'
                    DocIdCounter = counter'
                    Materialized = materialized'
                    Ledger = ledger' }

    let freeze (runtime: SessionRuntime) (mode: FreezeMode) : FrozenTreeSnapshot * SessionRuntime =
        let revision, ledger' = RevisionLedger.reserve runtime.Ledger

        let snapshot =
            FrozenSnapshot.freezeTree revision runtime.Session.Graph runtime.Registry runtime.Contents mode

        snapshot, { runtime with Ledger = ledger' }
