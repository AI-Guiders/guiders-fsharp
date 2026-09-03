# GUIDERS-FSHARP-ADR-0007: Open build SSOT — FTC + Correspondence (not MSBuild)

| | |
|---|---|
| **Status** | Draft |
| **Date** | 2026-09-03 |
| **Tags** | #guiders #build #ftc #correspondence #federation #msbuild-legacy |
| **Related** | [0004](./GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md) · [0006](./GUIDERS-FSHARP-ADR-0006-adr-lifecycle-verifiable-facts.md) · [ide-session §03 frozen snapshots](../math/ide-session/03-frozen-snapshots.md) · [GUIDERS-ADR-0062](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md) · [GUIDERS-ADR-0028](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0028-documentation-guild-correspondence-family.md) |

```text
facts:
  golden:
    - GS-OB1: FCS diagnostics ⊆ dotnet build on guiders-fsharp slnx (parity gate)
    - GS-OB2: WorkspaceView materializes compiler services without CDP bootstrap bypass
    - GS-OB3: SdkAssets fallback includes framework ref pack for target TFM
  hoare:
    - OB-H1: { graph revision = r ∧ frozen(ProjClosure) = π } materialize(π) { Sat(Q_compile) }
    - OB-H2: { Sat(Q_compile) } emit(workspace, target) { artifacts compatible with legacy MSBuild layout }
    - OB-H3: { ADR fact f ∧ edge verified_by(f, e) } check(e) { Sat(f) ∨ reject with evidence }
  wf:
    - WF-OB1: SSOT for design-time project truth is FTC graph + WorkspaceView, not project.assets.json alone
    - WF-OB2: MSBuild / ProjInfo is an optional port (legacy_emit), never the only source of refs
end facts
prose:
```

## Context

MSBuild is a **task graph + XML project evaluator** from the pre–AI Era. It answers *how to compile*, not *what the system is*, *what invariants hold*, or *whether an ADR is still true in code*.

Guiders already has a richer model:

- **FTC** — federation session graph, revision, frozen snapshots, `ProjClosure`
- **WorkspaceView (π_ws)** — materialized compiler-services surface for FCS/Roslyn
- **Correspondence** — doc ↔ code ↔ evidence (facts, `normates`, `verified_by`)
- **GDL** — readable catalog of transforms, gates, Hoare obligations

F# first-class (ADR-0005 / platform ADR-0062) proved the seam: bootstrap bypass produced ~94 false errors; the correct wire is **FTC → WorkspaceView → design-time ports → language backends**. MSBuild (Ionide.ProjInfo) is a **port**, not SSOT.

**Strategic bet:** an open build system where **intent + model + evidence** precede emit will replace MSBuild for greenfield the way modern package managers replaced hand-rolled Makefiles — not because MSBuild is slow, but because it cannot be **self-checking**.

Orchestrators (Cake, NUKE, FAKE) do not change this: they still delegate evaluation to MSBuild/SDK for `.csproj`/`.fsproj`.

## Decision

### 1. Layered build architecture (normative)

```text
L0 Intent     — ADR facts, Hoare obligations, invariants (Correspondence L1/L3)
L1 Model      — FTC graph, revision ledger, frozen snapshots, WorkspaceView
L2 Transform  — session patches, refactors, agent edits (Θ + gates)
L3 Evidence   — correspondence checks, parity gates, golden sessions
L4 Emit       — csc/fsc/link/publish drivers
L5 Legacy     — MSBuild / dotnet CLI (compatibility backend only)
```

**Rule:** L1 is SSOT for design-time and build-time *truth*. L5 may be used for parity and external consumers; it must never be the only path to compiler options.

### 2. Ports (current → target)

| Concern | Legacy port (now) | Target SSOT |
|---------|-------------------|-------------|
| Project graph | slnx + MSBuild discovery | FTC `SolutionGraph` |
| Refs / defines / sources | ProjInfo, SdkAssets | `WorkspaceView` + phased loader |
| Compiler options | FCS/Roslyn adapters | materialize(π_ws) |
| Artifacts on disk | `dotnet build` | L4 emit driver (future) |
| ADR conformance | tests + manual review | Correspondence `verified_by` |

### 3. Self-checking ADR (not test sprawl)

Per [ADR-0006](./GUIDERS-FSHARP-ADR-0006-adr-lifecycle-verifiable-facts.md), normative ADRs carry machine-facing `facts:` blocks. **Implemented** requires `Sat(facts)` via Correspondence evidence — not inference from Accepted prose.

This ADR pilots the pattern for **build/model** decisions: golden sessions + Hoare gates instead of duplicating MSBuild semantics in hundreds of unit tests.

### 4. Phased rollout

| Phase | Scope | Exit |
|-------|-------|------|
| **P0** (now) | F# first-class; FTC wire; SdkAssets + ProjInfo fallback; parity gate | GS-OB1..OB3 green |
| **P1** | Open Code Modeling slice via CDP; VFS overlay from `WorkspaceView.Contents` | dirty-buffer diagnostics |
| **P2** | Emit driver from WorkspaceView (csc/fsc invocation) | build without MSBuild evaluation |
| **P3** | Correspondence on ADR facts ↔ anchors; MSBuild = `legacy_emit` only | new repos need no `.fsproj` for IDE |

## Consequences

- **Positive:** one model for IDE, agents, and future build; ADRs become checkable artifacts; MSBuild drift (e.g. ref-pack version pick) is isolated to legacy ports.
- **Negative:** until P2, `dotnet build` remains parity oracle; dual-path maintenance for ProjInfo + SdkAssets.
- **Risk:** ecosystem (NuGet, analyzers, source generators) still assumes MSBuild evaluation — L5 stays required for interop long after SSOT moves to L1.

## Non-goals

- Replacing MSBuild in one PR.
- Mandating Cake/NUKE/FAKE as orchestration religion.
- Retrofitting all platform ADRs with `facts:` before P0 ships.

```text
end prose
```
