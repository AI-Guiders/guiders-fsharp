namespace AIGuiders.Platform.Modeling.Ide.Session

type PatchApplyResult =
    | PatchApplied of SessionRuntime
    | PatchRejected of reasons: string list

and SessionRuntime =
    { Session: SolutionSession
      Contents: Map<string, string>
      Materialized: MaterializedState
      Ledger: RevisionLedger }
