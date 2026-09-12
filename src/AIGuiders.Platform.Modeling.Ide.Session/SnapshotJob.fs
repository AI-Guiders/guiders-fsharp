namespace AIGuiders.Platform.Modeling.Ide.Session

/// <summary>§04 OD3/OD4 — snapshot job lane: freeze @ r → job handle; live M untouched until apply.</summary>
/// based on adr: docs/math/ide-session/04-jobs-lifecycle.md §2.9, OD3, OD4
type SnapshotJobStatus =
    | JobPending
    | JobCompleted
    | JobFailed of reason: string

type SnapshotJob =
    { Id: string
      Kind: CapabilityKind
      ProjectId: ProjectId
      AtRevision: SessionRevision
      Theta: string
      Status: SnapshotJobStatus }

module SnapshotJob =
    let start (runtime: SessionRuntime) (kind: CapabilityKind) (projectId: ProjectId) (theta: string) (mode: FreezeMode) : SnapshotJob * SessionRuntime =
        let frozen, runtime' = SessionOrchestrator.freeze runtime mode

        let job =
            { Id = System.Guid.NewGuid().ToString("N")
              Kind = kind
              ProjectId = projectId
              AtRevision = frozen.Revision
              Theta = theta
              Status = JobPending }

        job, runtime'

    let complete (job: SnapshotJob) : SnapshotJob = { job with Status = JobCompleted }

    let fail (job: SnapshotJob) (reason: string) : SnapshotJob = { job with Status = JobFailed reason }
