# Federation model extraction — living matrix (ship tracker)

Tracks ADR-0003 §4 matrix rows against `model_execution_split_audit` plan. Updated per Nested AxB ship wave.

| Domain | F# Modeling SSOT | Execution | Status |
|--------|------------------|-----------|--------|
| Relations kernel | `LanguageIntelligence.Relations` | `Execution.LanguageIntelligence.Relations` | **shipped** Phase 1 |
| Scene projection | `Ide.Session.SceneProjection` | `Navigation.Code` | **shipped** |
| Attach contract | `CommandPlane.AttachSchema` | `CommandPlane.Catalog` + `ArgSuggestions` step brokers | **shipped** |
| Attach contextual | `SessionGraphPickerChoices` + registry/diagnostic pickers | full step broker matrix incl. manual browse-all | **shipped** |
| FCS host IO | shapes in `Language.Adapters.Fcs` | `FcsCompilerServicesHost` in `Execution.Language.Adapters.Fcs` | **shipped** host + MSBuild materialize @ Execution |
| Nav seed naming | `Relations.NavSeed` | `Navigation.NavSeed` primary; `NavigationAnchor` deleted | **shipped** |
| Kind: bracket wire | `Notations.Bracket` | `Notations.Bracket` + conformance | **shipped** canon vectors |
| Config schema | `Modeling.Configurations` | `Execution.Configurations.*.Sources` | **exists** |
| C# IR fork | deleted | seam types in `Execution.LanguageIntelligence` | **shipped** IR.Language project removed |
| Legacy F/M/L wires | delete at boundary | `LegacyBracketRelationWire` transitional parse | **bounded** Obsolete + shim alias |

**Next waves:** move `FcsProbeProjectOptionsSource` IO to Execution · ADR-0063 full body · legacy F/M/L wire delete.
