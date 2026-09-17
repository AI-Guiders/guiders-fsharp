namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Core.Identity

type TransformClass =
    | Refactor of id: string
    | Fix
    | Style
    | Config
    | Other of tag: string

type LedgerEntry =
    { Revision: SessionRevision
      Scope: InvalidationScope
      ThetaClass: TransformClass
      Patch: SessionPatch
      GitPin: GitPin }

type RevisionLedger =
    { NextRevision: SessionRevision
      Entries: LedgerEntry list }

module RevisionLedger =
    let empty = { NextRevision = 1L; Entries = [] }

    let append (scope: InvalidationScope) (thetaClass: TransformClass) (patch: SessionPatch) (gitPin: GitPin) (ledger: RevisionLedger) =
        let entry =
            { Revision = ledger.NextRevision
              Scope = scope
              ThetaClass = thetaClass
              Patch = patch
              GitPin = gitPin }

        { NextRevision = ledger.NextRevision + 1L
          Entries = ledger.Entries @ [ entry ] }

    let reserve (ledger: RevisionLedger) : SessionRevision * RevisionLedger =
        ledger.NextRevision, { ledger with NextRevision = ledger.NextRevision + 1L }

    let currentRevision (ledger: RevisionLedger) =
        if List.isEmpty ledger.Entries then
            0L
        else
            ledger.Entries |> List.last |> (fun e -> e.Revision)
