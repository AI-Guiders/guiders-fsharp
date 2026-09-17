namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

type PatchApplyResult =
    | PatchApplied of SessionRuntime
    | PatchRejected of reasons: string list

and SessionRuntime =
    { Session: SolutionSession
      Registry: DocumentRegistry
      Contents: Map<DocId, DocumentText>
      Diagnostics: DiagnosticIndex
      Materialized: MaterializedState
      Ledger: RevisionLedger
      DocIdCounter: int64 }

module SessionRuntime =
    let emptyDiagnostics = Map.empty

module SessionBootstrap =
    open AIGuiders.Platform.Modeling.Paths

    /// Path-keyed bootstrap for tests and slnx ports (plan §2.4.1).
    let createFromPaths
        (session: SolutionSession)
        (pathContents: seq<string * string>)
        (ownership: Map<string, ProjectId>)
        =
        let boot = DocumentRegistryOps.bootstrap pathContents ownership 0L

        { Session = session
          Registry = boot.Registry
          Contents = boot.Contents
          Diagnostics = SessionRuntime.emptyDiagnostics
          Materialized = MaterializedState.empty
          Ledger = RevisionLedger.empty
          DocIdCounter = boot.NextCounter }

    let createFromRegistry
        (session: SolutionSession)
        (registry: DocumentRegistry)
        (contents: Map<DocId, DocumentText>)
        (nextCounter: int64)
        =
        { Session = session
          Registry = registry
          Contents = contents
          Diagnostics = SessionRuntime.emptyDiagnostics
          Materialized = MaterializedState.empty
          Ledger = RevisionLedger.empty
          DocIdCounter = nextCounter }
