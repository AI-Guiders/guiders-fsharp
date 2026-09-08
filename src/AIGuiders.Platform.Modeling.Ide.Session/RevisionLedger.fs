namespace AIGuiders.Platform.Modeling.Ide.Session

type GitPin = { Commit: string option }

type LedgerEntry =
    { Revision: SessionRevision
      Scope: InvalidationScope
      ThetaClass: string
      Patch: SessionPatch
      GitPin: GitPin }

type RevisionLedger =
    { NextRevision: SessionRevision
      Entries: LedgerEntry list }

module RevisionLedger =
    let empty = { NextRevision = 1L; Entries = [] }

    let append (scope: InvalidationScope) (thetaClass: string) (patch: SessionPatch) (gitPin: GitPin) (ledger: RevisionLedger) =
        let entry =
            { Revision = ledger.NextRevision
              Scope = scope
              ThetaClass = thetaClass
              Patch = patch
              GitPin = gitPin }

        { NextRevision = ledger.NextRevision + 1L
          Entries = ledger.Entries @ [ entry ] }

    /// Freeze is not a Δ (§2.12 Λ = Δ-stream of applied patches), but its revision
    /// must still come from the same monotonic counter — reserve it atomically.
    let reserve (ledger: RevisionLedger) : SessionRevision * RevisionLedger =
        ledger.NextRevision, { ledger with NextRevision = ledger.NextRevision + 1L }

    let currentRevision (ledger: RevisionLedger) =
        if List.isEmpty ledger.Entries then
            0L
        else
            ledger.Entries |> List.last |> (fun e -> e.Revision)