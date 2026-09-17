# Federation model extraction — living matrix (ship tracker)

Tracks ADR-0003 §4 matrix rows against `model_execution_split_audit` plan. Updated per Nested AxB ship wave.

| Domain | F# Modeling SSOT | Execution | Status |
|--------|------------------|-----------|--------|
| Relations kernel | `LanguageIntelligence.Relations` | `Execution.LanguageIntelligence.Relations` | **shipped** Phase 1 |
| Scene projection | `Ide.Session.SceneProjection` | `Navigation.Code` | **shipped** |
| Attach contract | `CommandPlane.AttachSchema` | `CommandPlane.Catalog` + `ArgSuggestions` step brokers | **shipped** verb+step pickers |
| Attach contextual | `SessionGraphPickerChoices` + registry/diagnostic pickers | full step broker matrix incl. manual browse-all | **shipped** |
| FCS host IO | shapes in `Language.Adapters.Fcs` | `FcsExecutionHost` + `FcsExecutionProjectOptionsSource` bind | **shipped** options source @ Execution boundary |
| Kind: bracket wire | `Notations.Bracket` | `Notations.Bracket` + conformance | **shipped** canon vectors |
| Config schema | `Modeling.Configurations` | `Execution.Configurations.*.Sources` | **exists** |
| Nav seed naming | `Relations.NavSeed` | `Navigation.NavSeed` + Code NavSeed-first APIs | **partial** shim + overloads |
| C# IR fork | shim only | delete post-conformance | **open** |
| Legacy F/M/L wires | delete at boundary | `LegacyBracketRelationWire` transitional parse | **bounded** Obsolete + shim alias |

**Next waves:** IR fork deletion · full `FcsCompilerServicesHost` File IO move · `NavigationAnchor` type removal · ADR-0063 full body.
