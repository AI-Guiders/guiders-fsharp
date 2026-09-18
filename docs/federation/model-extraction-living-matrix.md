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
| attach step brokers | `AttachSchemaCatalog` + contextual pickers | `FederationAttachCatalog` + ArgSuggestions matrix | **shipped** ship-5 |
| Relation resolve seams | `ResolveCtx` + `RelationSpec` (Modeling) | `IResolveRelation` + `IMaterializeRelation` @ `Execution.LanguageIntelligence.Relations` | **shipped** ship-5 |
| FCS host IO | `FcsProbeWire` + guards (pure) | `FcsCompilerServicesHost` + probe + ProjInfo sources | **shipped** all FCS File/MSBuild IO @ Execution |
| FCS patch apply IO | `FcsSessionPatchBridge` (pure map) + `IFcsSessionPatchApplier` port | `FcsSessionPatchApplier` bound @ `FcsModelingBindings` | **shipped** |
| FCS backend text/graph IO | `IFcsSourceTextSource` + `IFcsSolutionGraphSource` ports | `FcsWorkspaceIoSource` bound @ `FcsModelingBindings` | **shipped** |
| FCS fsproj ownership IO | `IFcsProjectOwnershipSource` port; `FcsProjectResolver` thin delegate | `FcsProjectOwnershipSource` bound @ `FcsModelingBindings` | **shipped** all FCS File/Directory IO @ Execution |
| Nav seed naming | `Relations.NavSeed` in `Navigation.Scene.Seed` | `Navigation.NavSeed` seam → Relations kernel | **shipped** ship-40 |
| Kind: bracket wire | `Notations.Bracket` | `Notations.Bracket` + conformance | **shipped** canon vectors |
| ADR-0063 TO-BE | — | `GUIDERS-ADR-0063` §9–§10 RelationSpec normative | **shipped** §10 body |
| ADR-0003 tree amend | §4.8–§5 Relations/Agent/Correspondence | — | **shipped** |
| ship-2 package tree | `Modeling.Agent`, `Documentation.Correspondence`, Relations | IR.Language fork deleted | **shipped** |
| ship-5 props/shims | `UseGuidersModelingRelations` documented | `eng/Guiders.Modeling.relations.props` bundle | **shipped** |
| ship-5 product docs | GDL catalog + README | platform README + architecture hub IR.Language retired | **shipped** |
| Config schema | `Modeling.Configurations` pure predicates + `KnowledgeWire` parse | `Execution.Configurations.Workspace.Sources` IO (`KnowledgeWireSources`, `ConfigurationContractSources`) | **shipped** ship-29 |
| Session port IO | pure `DotNetSlnxGraphPort` + `WorkspaceGraphPort` builders | `Execution.Ide.Session.Sources` (`DotNetSlnxGraphSources`, `WorkspaceGraphSources`, `DotNetProjectFileSources`) | **shipped** ship-30 |
| Gdl.Authoring file IO | pure `AuthoringSource.fromText`; project types + `PathBoundary` | `Authoring.Core` (`AuthoringSource.FromFile`, `AuthoringProjectLoader`); SAT bridges read disk | **shipped** ship-31 |
| ship-3 IO-in-Modeling | zero `File.*` / `Directory.*` in `Platform.Modeling.*` | Config + session + GDL IO @ Execution sources | **shipped** ship-31 |
| relation-spec-witness | `12-relation-spec.md` + `BracketRelationWire.tryParseRelationSpec` | `relation-spec-witness.spec.json` + `RelationSpecWitnessConformanceTests` | **shipped** ship-4 |
| math §12 README map | `docs/math/ide-session/12-relation-spec.md` | — | **shipped** ship-4 |
| GoldenEvidence scan IO | — | `Execution.Documentation.Correspondence.GoldenEvidence` workspace scan | **shipped** ship-28 |
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
| Dependency kernel | `DependencyRelationKind` + E_dep sort laws in `RelationGraph` | `RoslynDependencyRelationEmitStub` + `AdapterSlotRegistry` | **shipped** ship-33 |
| TypeSystem profile schema | `TypeSystemProfile.csharpRoslynProfile` | Execution registers `AdapterSlot` only | **shipped** ship-33 |
| ReverseAnchor bridge | `ReverseAnchorBridge.tryToDocToCodeWitness` | CRS Execution unchanged; witness for attach/materialize | **shipped** ship-33 |
| Correspondence RelationType | `CorrespondenceRelationGraph` + `GraphNodeRef.AdrObligation` | sort laws for Documents/ImplementsObligation/VerifiedBy | **shipped** ship-34 |
| R5 adapter registration | — | `RelationSeamRegistry` + `RoslynDependencyRelationEmitStub.RegisterDefaults` | **shipped** ship-34 |
| ReverseAnchor materialize | `CorrespondenceMaterialize.tryMaterializeReverseAnchor` | witness → validated `Relation` in G | **shipped** ship-35 |
| Roslyn E_dep ingest v1 | `CorrespondenceMaterialize.buildUses` | `RoslynDependencyRelationIngest` field-type Uses | **shipped** ship-35 |
| Session Contents E_dep hook | `DependencyRelationOps.ingest` + `SolutionGraph.mergeRelations` | `DependencyRelationIngest` @ `FederationSessionRuntime.Open` | **shipped** ship-36 |
| Correspondence CLIMutable strip | plain F# records in `Documentation.Correspondence` | `CorrespondenceModels` constructor seam (no CLIMutable views) | **shipped** ship-37 |
| CompilerServices E_dep v2 | `buildExtends` / `buildImplementsInterface` / `buildTypeUses` | `RoslynDependencyRelationIngest.IngestProjectSources` + `IngestForProject` @ ensure | **shipped** ship-38 |
| Navigation CLIMutable strip | plain F# `Navigation.Scene` records | `NavigationModels` constructor seam | **shipped** ship-39 |
| Phase 1 checklist gate | `FederationPhase1ChecklistTests` | platform green + living matrix | **shipped** ship-39 |
| BuildDiagnostic ingest | `BuildDiagnosticOps` via `DiagnosticIndexOps.ingestMapped` | `BuildDiagnosticIngest` @ Execution | **shipped** ship-41 |
| AnchorWire SniperScope Modeling delete | removed from `Modeling.Language` | `LanguageSeamModels` Execution-only seam | **shipped** ship-42 |
| Build runtime hook | `BuildDiagnosticOps` ingest | `FederationSessionRuntime.TryIngestBuildDiagnostics` | **shipped** ship-43 |
| ReverseAnchor CRS boundary | `CorrespondenceRelationOps.ingestReverseAnchors` | `CorrespondenceRelationIngest` + `TryIngestCorrespondenceForFile` | **shipped** ship-44 |
| Phase 3–4 verification gate | `FederationPhase34ChecklistTests` IO guard + attach + FCS ports | `FederationPhase34ChecklistTests` Execution sources + attach catalog | **shipped** ship-45 |
| CompilerServices CRS hook | `CorrespondenceRelationOps` | `ApplyEnsure` ingests correspondence for active file | **shipped** ship-45 |
| ReverseAnchor wire delete | `DocToCodeWitness` + typed `CorrespondenceRelationKind`; `DocToCodeWitnessBridge` | `DocToCodeWitness` seam; `IngestDocToCodeWitnesses` | **shipped** ship-46 |
| CRS ingest on Open | `CorrespondenceRelationOps` | `Open` + `IngestFromRegistry` when workspace root found | **shipped** ship-47 |
| Correspondence.Kind canon | typed `CorrespondenceRelationKind` on witness | `CorrespondenceKind.NormalizeWire` @ resolver boundary | **shipped** ship-47 |
| BracketAnchorSpan retirement | absent from Modeling | `BracketAnchorSpan` deleted; `CodeEditResolveAxes` + `LegacyWireSpan` boundary | **shipped** ship-48 |
| Build diagnostic producer | `BuildDiagnosticOps` ingest | `BuildDiagnosticProducer` + `TryRunBuildAndIngestDiagnostics` | **shipped** ship-49 |
| LegacyWireSpan conformance retirement | `CodeEditWireEncoding` Kind axes | anchor-resolve + canon specs kind-spec only; legacy-span conformance path removed | **shipped** ship-50 |
| §10 TO-BE audit | requirement-by-requirement evidence table | `model-extraction-to-be-audit.md` + `FederationPhase10ChecklistTests` | **shipped** ship-51 (closure **BLOCKED** — 5 partials) |
| BracketResolveBoundary | Kind-first unified wire parse | `BracketResolveBoundary` + `buildCodeEdit` doc profile | **shipped** ship-52 |
| P4-01 docs refresh | ADR-0042 IR.Language retirement; math ω → DocumentRegistry | Phase10 ADR/math gates | **shipped** ship-52 |
| CDP BracketLocate migration | `BracketResolveBoundary` + `LegacyWireSpan`; no `BracketAnchorSpan` | guiders-core `Cdp.ScriptableIde` + `BracketLocateDelegationTests` | **shipped** ship-53 |
| CDP CRS witness mapping | platform `DocToCodeWitness` | `WorkspaceCorrespondence` maps `DocToCodeWitnesses` (CDP `ReverseAnchor` façade) | **shipped** ship-53 |
| Kind witness emit + format | `BuildCodeEdit` / `TryFormatCodeEdit` | CRS reverse wires Kind:; CDP `Format(preferCanonical)` | **shipped** ship-54 |
| Kind-only resolve boundary | `TryParseToAxes` Kind-spec only | legacy F/M/L ingest via `RelationWireBoundary` @ `BracketLocate.Parse` | **shipped** ship-55 |
| CDP default Kind emit | non-nav `Format` | code/xml/json/fsharp wires default `[Kind:CodeEdit; …]` | **shipped** ship-55 |
| Kind:Nav wire boundary | `NavResolveAxes` + flatten legacy nested nav | `TryParseNav` / `TryFormatNav`; CDP nav Format → Kind:Nav | **shipped** ship-56 |

**Next waves:** delete `LegacyWireSpan`, command-only nav Kind:Nav, goal closure when audit all `verified`.
