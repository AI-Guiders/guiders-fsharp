namespace AIGuiders.Platform.Modeling.Ide.Session

open System.IO

module SessionOrchestrator =
    let loadContentsFromDisk (graph: SolutionGraph) =
        graph.FileOwnership
        |> Map.keys
        |> Seq.choose (fun path ->
            if File.Exists path then
                Some(path, File.ReadAllText path)
            else
                None)
        |> Map.ofSeq

    let create (session: SolutionSession) (contents: Map<string, string>) =
        { Session = session
          Contents = contents
          Materialized = MaterializedState.empty
          Ledger = RevisionLedger.empty }

    let preview (runtime: SessionRuntime) (patch: SessionPatch) =
        SessionPatch.apply runtime.Session.Graph runtime.Contents patch

    let applyPatch (runtime: SessionRuntime) (patch: SessionPatch) (gitPin: GitPin) =
        let scope = SessionPatch.scope patch
        let graph', contents' = SessionPatch.apply runtime.Session.Graph runtime.Contents patch

        match GraphValidation.validate graph' with
        | result when not result.IsValid ->
            PatchRejected(result.Issues |> List.map (fun i -> i.Message))
        | _ ->
            let materialized' = MaterializedState.Invalidation.forScope scope graph' runtime.Materialized
            let ledger' = RevisionLedger.append scope "refactor" patch gitPin runtime.Ledger
            let session' = { runtime.Session with Graph = graph' }

            PatchApplied
                { runtime with
                    Session = session'
                    Contents = contents'
                    Materialized = materialized'
                    Ledger = ledger' }

    let freeze (runtime: SessionRuntime) (mode: FreezeMode) =
        let revision = RevisionLedger.currentRevision runtime.Ledger + 1L

        FrozenSnapshot.freezeTree revision runtime.Session.Graph runtime.Contents mode

    /// <summary>ADR-0062 §5 — materialize <c>CompilerServices</c> for the owning project of <paramref name="filePath"/>.</summary>
    let ensureCompilerServices (runtime: SessionRuntime) (filePath: string) =
        CompilerServicesMaterialization.ensure runtime filePath
