# Federation model extraction — living matrix (ship tracker)

Tracks ADR-0003 §4 matrix rows against `model_execution_split_audit` plan. Updated per Nested AxB ship wave.

| Domain | F# Modeling SSOT | Execution | Status |
|--------|------------------|-----------|--------|
| Relations kernel | `LanguageIntelligence.Relations` | `Execution.LanguageIntelligence.Relations` | **shipped** Phase 1 |
| Scene projection | `Ide.Session.SceneProjection` | `Navigation.Code` | **shipped** |
| Attach contract | `CommandPlane.AttachSchema` | `CommandPlane.Catalog` + `ArgSuggestions` step brokers | **shipped** |
| Attach contextual | `SessionGraphPickerChoices` + registry/diagnostic pickers | full step broker matrix incl. manual browse-all | **shipped** |
| FCS host IO | `FcsProbeWire` + guards (pure) | `FcsCompilerServicesHost` + probe + ProjInfo sources | **shipped** all FCS File/MSBuild IO @ Execution |
| FCS patch apply IO | `FcsSessionPatchBridge` (pure map) + `IFcsSessionPatchApplier` port | `FcsSessionPatchApplier` bound @ `FcsExecutionHost` | **shipped** |
| FCS backend text/graph IO | `IFcsSourceTextSource` + `IFcsSolutionGraphSource` ports | `FcsWorkspaceIoSource` bound @ `FcsExecutionHost` | **shipped** |
| FCS fsproj ownership IO | `IFcsProjectOwnershipSource` port; `FcsProjectResolver` thin delegate | `FcsProjectOwnershipSource` bound @ `FcsModelingBindings` | **shipped** all FCS File/Directory IO @ Execution |
| Nav seed naming | `Relations.NavSeed` | `Navigation.NavSeed` primary; `NavigationAnchor` deleted | **shipped** |
| Kind: bracket wire | `Notations.Bracket` | `Notations.Bracket` + conformance | **shipped** canon vectors |
| ADR-0063 TO-BE | — | `GUIDERS-ADR-0063` §9–§10 RelationSpec normative | **shipped** §10 body |
| ADR-0003 tree amend | §4.8–§5 Relations/Agent/Correspondence | — | **shipped** |
| ship-2 package tree | `Modeling.Agent`, `Documentation.Correspondence`, Relations | IR.Language fork deleted | **shipped** code; README/catalog drift cleanup pending |
| Config schema | `Modeling.Configurations` | `Execution.Configurations.*.Sources` | **exists** |
| C# IR fork | deleted | seam types in `Execution.LanguageIntelligence` | **shipped** IR.Language project removed |
| Legacy F/M/L wires | Kind: canon in `Notations.Bracket` | `LegacyBracketRelationWire` boundary parse only | **shipped** obsolete shims deleted |

**Next waves:** ship-5 product stubs · delete `LegacyBracketRelationWire` after RelationSpec resolve path · Gdl catalog doc drift · `FcsProjectOptionsSdkSource` / remaining DotNetWorkspace in Modeling trim.
