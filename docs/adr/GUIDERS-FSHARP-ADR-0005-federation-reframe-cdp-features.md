# GUIDERS-FSHARP-ADR-0005: Federation reframe of CDP features

| | |
|---|---|
| **Status** | Accepted · In progress |
| **Implementation** | CDP `FederationSessionBridge` + `cdp_ide_session_scene` (0.5.750); Correspondence / buffer→patch next |
| **Date** | 2026-09-02 |
| **Tags** | #guiders #fsharp #federation #cdp #modeling #ide #correspondence |
| **Related** | [GUIDERS-FSHARP-ADR-0004](./GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md) · [GUIDERS-ADR-0062](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md) · [GUIDERS-ADR-0063](https://github.com/AI-Guiders/guiders-platform/blob/main/docs/adr/GUIDERS-ADR-0063-anchors-federation-reincarnation.md) · [GUIDERS-ADR-0061](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0061-language-resolver-center.md) · [ide-session axioms](../math/ide-session/README.md) |

## Context

**CDP** (`cdp-mcp`) grew in the **pre-federation** era: excellent habitat (buffer, MCP, cockpit, durable jobs, citizen routes), but **no federation SSOT** for solution/session, anchors, or cross-cutting semantics.

Artifacts from that era share a pattern ([GUIDERS-ADR-0063](https://github.com/AI-Guiders/guiders-platform/blob/main/docs/adr/GUIDERS-ADR-0063-anchors-federation-reincarnation.md)):

- tactical wires between host and tools
- planet-local DTOs and stringly MCP payloads
- heuristics where a graph would give blast radius and proofs

With **`Platform.Modeling.*` (F#)** and the **ide-session** axiom package, federation can **reframe almost every CDP-facing feature** — not as a rewrite of CDP, but as **strict typing + graph semantics** with CDP as **dogfood host**, not model owner.

**Norm:** math without executable conformance is decoration ([§11 conformance](../math/ide-session/11-conformance.md)).

## Decision

Adopt a **three-layer stack** for all IDE-adjacent capabilities:

```text
Modeling (F# IR, axioms, conformance)     ← federation SSOT, typed graph G
        ↑ contracts / ports
Execution (orchestrator, materialize, jobs) ← policy + lifecycle runtime
        ↑ ingress / habitat
CDP (buffer, MCP, cockpit, citizen)        ← first host; not parallel SSOT
```

**Pre-federation CDP behavior is normalized into Modeling + Execution**, not duplicated forever in host conditionals.

New federation features **start in Modeling** (types + golden sessions), land in **Execution** (orchestrator ports), and surface in **CDP** (verbs/MCP) last.

## Feature reframe map

| Pre-federation (CDP / platform tactical) | Federation (graph + Modeling) | CDP role after reframe |
|------------------------------------------|-------------------------------|-------------------------|
| Buffer open/edit/sniper | `apply(Δ)` + `scope(δ)` + revision ledger Λ ([§2.12](../math/ide-session/07-revision-ledger.md)) | ingress; mutate via orchestrator contract |
| Peel / file_lines pressure | `refine(δ)` → semantic closure; kind-aware quality gates | pulse only; no LOC on prose |
| Anchors / loci | `Anchor` entity ↔ \(N_{\mathsf{sem}} \cup N_{\mathsf{syn}}\) (ADR-0063) | resolve/placement consumer |
| Language warm / LRC switches | \( \pi \), \( \tau \), \( \kappa_\pi \); LRC = port on `CompilerServices` (ADR-0061) | host wiring; no lifecycle SSOT |
| Solution / slnx load | \( G = (\mathbb{P}, E_{\mathsf{proj}}, \omega, \kappa) \) (ADR-0062) | parser port → graph |
| GDL / Planet workspaces | \( \tau \in \mathbb{T} \); same orchestrator (§1.5 T1–T3) | not a second IDE session |
| Build / test / LUT | on-demand `job(k, π, φ_r, θ)`; live \(M\) not evicted (§7) | `cdp_build` / scheduler triggers |
| Refactor / fix / style | \( \Theta \) classes + Hoare + `StylePath` (§2.10–2.11); **readable gates:** [`ide-session.catalog.gdl`](../gdl/ide-session.catalog.gdl) | preview/apply verbs |
| **Correspondence** (doc ↔ code) | typed edges in \(G\): doc fragment ↔ anchor / symbol id; shared resolve with sniper & LRC | UI projection + MCP read; **not** ad-hoc string match SSOT |
| Cockpit / deck / rules | typed cockpit circuit graph IR (ADR-0002) | mount + bus; rules eval on IR |
| Conformance / vectors | golden sessions per shipped axiom (§11 GS*) | CI gate on Modeling packages |

## Correspondence — recommended next pilot

`Documentation.Correspondence.*` today is **pre-federation wiring**. Federation shape:

1. **Nodes:** doc regions (path + stable fragment id), code symbols (\(n \in N_{\mathsf{sem}}\)), optional GDL declare entities, **ADR lifecycle state** (L1′ per [FSHARP-0006](./GUIDERS-FSHARP-ADR-0006-adr-lifecycle-verifiable-facts.md)).
2. **Edges:** `CorrespondsTo` ⊆ (doc × code) scoped under \( \pi \) or solution via \(E_{\mathsf{proj}}\) closure — kinds `normates`, `verified_by`, `implements` ([wire JSON](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/wire/correspondence/correspondence-kinds.v1.json)).
3. **Invalidation:** doc `FileChange` → refine correspondence index; code `refine(δ)` → same anchor ids as ADR-0063; ADR status change → update L1′ without rescanning code.
4. **Conformance:** golden session `given G + doc patch, assert edge set Sat(Q_corr)`; `facts:` block ↔ GS* via `verified_by`.

Target packages (see [ADR-0003 matrix](./GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md)): `Modeling.Documentation.Correspondence` IR in F#; Execution hosts resolve; CDP exposes peek/navigate.

## Migration rules

1. **No second SSOT** — if Modeling has the type, CDP must not fork a parallel DTO “for speed”.
2. **Ports, not forks** — MSBuild, Roslyn, FCS, GDL parser, vendor formatters stay **ports** (`buildDriver`, `CompilerServices`, `styleDriver`).
3. **Shim until normalized** — legacy C# paths remain until conformance vectors pass; then delete shim, don’t wrap forever.
4. **Layer order** — Modeling IR + golden session → Execution orchestrator hook → CDP verb/MCP surface.
5. **Big-bang rejected** — vertical slices (session graph, anchors, correspondence, …) ship independently behind the same \(G\) laws.

## Consequences

- CDP codebase shrinks in **semantic ownership** over time; habitat value (MCP, jobs, cockpit) stays.
- New IDE features without Modeling types + conformance tests are **draft**, not federation law.
- `guiders-fsharp` is the primary home for **graph-shaped** IR (session, GDL, correspondence, cockpit circuit, conformance).
- Platform `Execution.*` owns **when** things run; Modeling owns **what** is true.

## Non-goals (this ADR)

- Rewriting all of `cdp-mcp` in one pass.
- Replacing Roslyn/FCS with a custom compiler (see ide-session §12 emit policy).
- ANUI GDL→IL fork — orthogonal; session federation proceeds regardless.

---

*Pre-federation built the roads; federation names the map.*
