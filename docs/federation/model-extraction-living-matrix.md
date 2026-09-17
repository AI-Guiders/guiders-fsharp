# Federation model extraction — living matrix (ship tracker)

Tracks ADR-0003 §4 matrix rows against `model_execution_split_audit` plan. Updated per Nested AxB ship wave.

| Domain | F# Modeling SSOT | Execution | Status |
|--------|------------------|-----------|--------|
| Relations kernel | `LanguageIntelligence.Relations` | `Execution.LanguageIntelligence.Relations` | **shipped** Phase 1 |
| Scene projection | `Ide.Session.SceneProjection` | `Navigation.Code` | **shipped** |
| SceneProjectionBridge | `SceneProjection.toEdge` SSOT | `NavigationSceneBuilder.BuildSubgraph` + related neighbor projection | **shipped** ship-20 |
| Homonyms codemod | `TypeSystemRelationKind` vs `CorrespondenceRelationKind` | platform `TypeSystemHomonymTests` gate | **shipped** ship-20 |
| DocumentRegistry + SessionContents | `DocumentRegistryOps` + `SessionRuntime.Contents` | `SessionContentsLoader` + `FederationSessionRuntime.Open` | **shipped** |
| DiagnosticIndex ingest | `DiagnosticIndexOps.ingest` + `refresh` (Modeling) | `DiagnosticIndexIngest` + `FederationSessionRuntime.TryRefreshDiagnosticIndex` + LRC hook @ `LanguageResolverCenter` | **shipped** ship-22 |
| Attach contract | `CommandPlane.AttachSchema` | `CommandPlane.Catalog` + `ArgSuggestions` step brokers | **shipped** |
| Attach contextual | `SessionGraphPickerChoices` + registry/diagnostic pickers | full step broker matrix incl. manual browse-all | **shipped** |
| FCS host IO | `FcsProbeWire` + guards (pure) | `FcsCompilerServicesHost` + probe + ProjInfo sources | **shipped** all FCS File/MSBuild IO @ Execution |
| FCS patch apply IO | `FcsSessionPatchBridge` (pure map) + `IFcsSessionPatchApplier` port | `FcsSessionPatchApplier` bound @ `FcsModelingBindings` | **shipped** |
| FCS backend text/graph IO | `IFcsSourceTextSource` + `IFcsSolutionGraphSource` ports | `FcsWorkspaceIoSource` bound @ `FcsModelingBindings` | **shipped** |
| FCS fsproj ownership IO | `IFcsProjectOwnershipSource` port; `FcsProjectResolver` thin delegate | `FcsProjectOwnershipSource` bound @ `FcsModelingBindings` | **shipped** all FCS File/Directory IO @ Execution |
| Nav seed naming | `Relations.NavSeed` | `Navigation.NavSeed` primary; `NavigationAnchor` deleted | **shipped** |
| Kind: bracket wire | `Notations.Bracket` | `Notations.Bracket` + conformance | **shipped** canon vectors |
| ADR-0063 TO-BE | — | `GUIDERS-ADR-0063` §9–§10 RelationSpec normative | **shipped** §10 body |
| ADR-0003 tree amend | §4.8–§5 Relations/Agent/Correspondence | — | **shipped** |
| ship-2 package tree | `Modeling.Agent`, `Documentation.Correspondence`, Relations | IR.Language fork deleted | **shipped** |
| ship-5 props/shims | `UseGuidersModelingRelations` documented | `eng/Guiders.Modeling.relations.props` bundle | **shipped** |
| ship-5 product docs | GDL catalog + README | platform README + architecture hub IR.Language retired | **shipped** |
| Config schema | `Modeling.Configurations` | `Execution.Configurations.*.Sources` | **exists** |
| GoldenEvidence scan IO | — | `Execution.Documentation.Correspondence.GoldenEvidence` workspace scan | **shipped** ship-28 |

**Next waves:** Plan §10 Phase 1 checklist · ship-3 Config sources + FCS host split remainder.
| C# IR fork | deleted | seam types in `Execution.LanguageIntelligence` | **shipped** IR.Language project removed |
| Legacy F/M/L wires | Kind: canon in `Notations.Bracket` | `RelationWireBoundary` parse boundary only | **shipped** ship-23 shim deleted |
| BracketLocate Kind-first | Kind: wire parse in Modeling | CDP `BracketLocate.Parse` → `RelationSpecWireBoundary` then legacy fallback | **shipped** ship-18 |
| RelationSpec resolve path | `RelationSpec` + Kind: wire | `RelationSpecWireBoundary` + `RelationSpecLegacyBridge` + C# + Xml `TryResolve(spec)` | **shipped** v1 bridge |
| Language C#/Xml resolve packages | — | `Execution.Language.CSharp.Relations` + `Language.Xml.Relations`; Anchors shims TypeForwardedTo | **shipped** ship-16 |
| Documentation resolve packages | — | `Execution.Documentation.Relations`; Anchors shim TypeForwardedTo | **shipped** ship-17 |
| anchor-resolve Kind vectors | Kind: wire in conformance spec | `RelationResolveSpecConformance` kind-spec mode + legacy-span dual path | **shipped** |
| Architecture hub Relations rows | — | hub EN/RU: `*.Relations` SSOT + Anchors shims + `RelationWireBoundary` | **shipped** ship-19 |

| CDP federation pulse | `SessionRuntime.Registry` (DocumentRegistry ω) | `FederationSessionBridge` pulse/scene `document_registry_count` | **shipped** ship-24 |

| SessionEdgeKind migration | `Relation` in `SolutionGraph.Relations` | `ISolutionInfoProvider.Relations` + test fixtures Relation-first | **shipped** ship-25 |

| BracketAnchorSpan Modeling delete | `XmlWireEncoding` only in `Notations.Bracket` | `BracketAnchorSpan` + `BracketAxisFamily` + `RelationSpecLegacyBridge` @ Execution | **shipped** ship-26 |

| Legacy edge shims deleted | `RelationGraph.projectRef` + orchestration helpers | ports/tests use `Relation` only | **shipped** ship-27 |
