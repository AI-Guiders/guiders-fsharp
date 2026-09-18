# Federation model extraction — §10 TO-BE audit

Normative source: `model_execution_split_audit` plan §10 (Ship checklist).  
Progress tracker (non-normative): [model-extraction-living-matrix.md](./model-extraction-living-matrix.md).

**Closure rule:** goal closes only when every row below is `verified` against code/tests — not matrix ticks alone.

**Summary (ship-59):** verified **31** · partial **0** · gap **0** · **CLOSURE: CLOSED**

Legend: **verified** = code + automated gate · **partial** = shipped subset / boundary shim · **gap** = missing TO-BE behavior.

---

## Phase 1 — Relations + wire

| ID | Requirement | Status | Evidence | Gate |
|----|-------------|--------|----------|------|
| P1-01 | `Modeling.Core.Identity` — CarrierWire, NumericId, GuidId, Sha256, CommitRef, Identity, AddressRef, ProjectId | verified | `Identity.fs`, `ParseCommit` | `FederationPhase1ChecklistTests.Identity kernel…` |
| P1-02 | `GitPin` — Commit: CommitRef option; parseCommit @ git port | verified | `Identity.fs`, `ParseCommit.parse` | Phase1 GitPin test |
| P1-03 | `LanguageIntelligence.Relations` — RelationSpec + §2.4 types | verified | `RelationSpec.fs`, `RelationGeometry.fs` | `RelationSpecTests`, witness spec |
| P1-04 | ResolveTier — no Text tier; Locus = Syntax \| Semantic | verified | `ResolveTier.fs` | Phase1 ResolveTier test |
| P1-05 | TransformClass DU replaces ThetaClass string | verified | `RevisionLedger` / `LedgerEntry` | Phase1 TransformClass test |
| P1-06 | Relation + RelationType in Ide.Session; SessionEdgeKind → Relation | verified | `RelationGraph.fs`, `SolutionGraph` | Phase1 Relations field test |
| P1-07 | Dependency kernel + TypeSystem profile; AdapterSlot Execution-only | verified | `DependencyRelationKind`, `TypeSystemProfile`, platform `AdapterSlotRegistry` | Phase1 + `DependencyKernelPlatformTests` |
| P1-08 | Delete `Modeling.Gdl.Language`; props + IR shim | verified | no `Modeling.Gdl.Language` project; platform no `IR.Language` csproj | Phase10 Gdl.Language absent |
| P1-09 | Notations.Bracket — Kind: canon; delete BracketAnchorSpan, AnchorWire, SniperScope | verified | Kind: + `BracketResolveBoundary`; `LegacyWireSpan` deleted; `RelationWireBoundary.TryParseDocScan` doc ingest only; `LegacyNavWireIngest` for nav legacy | Phase1 deletes; `CorrespondenceWireTests`, `DocReverseAnchorResolverTests`, Phase2/10 gates |
| P1-10 | RelationAttributes; NavSeed; Navigation.Scene.Edge projection | verified | `RelationAttributes`, `NavSeed`, `SceneProjection` | Phase1 NavSeed / SceneProjection tests |
| P1-11 | CLIMutable strip from kernel; seam/LRC only | verified | Navigation + Correspondence records plain F# | Phase1 CLIMutable tests |
| P1-12 | Execution *Relations* rename; registry interfaces | verified | `Execution.Language.*.Relations`, `IResolveRelation` | matrix ship-16/17, `RelationSeamContractTests` |
| P1-13 | Codemod wires/tests | verified | Kind:Nav parse/format incl. Member; CitizenIntentRouter all built anchors Kind:Nav; legacy Family: ingest+flatten only | `BracketResolveBoundaryTests`, `BracketLocateDelegationTests`, `CitizenLandHostTests`, Phase2 gate |
| P1-14 | Homonyms ImplementsInterface / ImplementsObligation | verified | distinct RelationType + CorrespondenceRelationKind | Phase1 homonym test |
| P1-15 | DiagnosticIndex + ingest; LRC Id → Code at ingest | verified | `DiagnosticIndexOps`, `DiagnosticIndexIngest` | ship-22 tests |
| P1-16 | SessionContents + DocumentRegistry; drop FileOwnership | verified | `DocumentRegistryOps`, `SessionRuntime.Contents` | Phase1 FileOwnership absent |

## Phase 2 — Modeling tree

| ID | Requirement | Status | Evidence | Gate |
|----|-------------|--------|----------|------|
| P2-01 | Package renames; Modeling.Agent (not Gdl.Agent / MCPlane) | verified | `AIGuiders.Platform.Modeling.Agent` | Phase10 Modeling.Agent |
| P2-02 | ReverseAnchor → DocToCode / Relation; Correspondence.Kind → RelationType | verified | `DocToCodeWitness`, `CorrespondenceRelationOps` | Phase1 ReverseAnchor deleted; Phase2 typed Kind |
| P2-03 | Build — BuildDiagnostic → RelationSpec.Diag only | verified | `BuildResultModel.fs` Spec field; `BuildDiagnosticOps` mints Diag | `BuildDiagnosticOpsTests`, Phase10 Spec field |
| P2-04 | Delete C# IR fork after conformance green | verified | `IntermediateRepresentation.Language` removed | Phase10 IR.Language absent |

## Phase 3 — IO to Execution

| ID | Requirement | Status | Evidence | Gate |
|----|-------------|--------|----------|------|
| P3-01 | Ports, GoldenEvidence, Config sources, FCS host split | verified | `Execution.Ide.Session.Sources`, `Configurations.Workspace.Sources`, `FcsExecutionHost` | Phase34 IO + FCS port tests |
| P3-02 | Build toolchain producer → session ingest | verified | `BuildDiagnosticProducer`, `TryRunBuildAndIngestDiagnostics` | Phase2 build producer gate + hook tests |

## Phase 4 — Docs + attach contract

| ID | Requirement | Status | Evidence | Gate |
|----|-------------|--------|----------|------|
| P4-01 | ADR/math amend (§9) | verified | ADR-0042 amendment (IR.Language retired); math `10-implementation.md` DocumentRegistry ω | Phase10 ADR/math gates |
| P4-02 | AttachSchema + CommandPlane catalog + conformance vectors | verified | `AttachSchemaCatalog`, `FederationAttachCatalog`, attach-schema.spec.json | Phase34 attach test |
| P4-03 | Living matrix doc | verified | `model-extraction-living-matrix.md` | Phase10 matrix exists |

---

## Partial items (block closure)

None — all audit rows verified (ship-59).

---

## Automated gate index

| Gate | Repo | Covers |
|------|------|--------|
| `FederationPhase1ChecklistTests` | fsharp | P1-01…P1-16 core |
| `FederationPhase2ChecklistTests` | fsharp + platform | P2-02, P1-09 deletes, build/CRS, `BracketResolveBoundary` |
| `FederationPhase34ChecklistTests` | fsharp + platform | P3-01, P4-02 |
| `FederationPhase10ChecklistTests` | fsharp + platform | §10 audit + P4-01 doc gates |

Re-run before claiming closure:

```bash
dotnet test guiders-fsharp/tests/AIGuiders.Platform.Modeling.Ide.Session.Tests -c Release --filter FederationPhase
dotnet test guiders-platform/tests/AIGuiders.Platform.Tests -c Release --filter FederationPhase
```
