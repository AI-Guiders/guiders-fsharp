package: AIGuiders.Platform.Modeling.Gdl.Command
tagline: Slash command descriptors: domain·object·intent naming, canonical paths with aliases, and arg-tail policies for command catalogs and palettes.

## What you get

- `CommandDescriptor` — the cross-product command row: DOI triple (`Domain`/`Object`/`Intent`), canonical `Path` + `PathAliases`, `Help`, `Group`, `ArgTail` wire, surfaces, scope tags, required capabilities, tier
- `CommandArgTailPolicy` — arg-tail semantics after the slash path:
  - `parse` — wire string → `CommandArgTailKind` (`None` / `Optional` / `Required` / `Picker` / `ImplicitSelection` / `ImplicitLineRange`); unknown falls back to `Optional`
  - `shouldAutoRunOnCommit` — the auto-run gate (exact path? trailing space? arg tail present?)
  - `extractSuggestionId` / `extractCompositeConstructorIds` — picker ids out of `picker:` / `suggest:` / `picker+constructor:` tails
- `CatalogRouteEntry` — resolved catalog row (path + command id + arg tail + DOI fields); `fromDescriptor` resolves the path role, `resolvePathRole` tells canonical from alias case-insensitively
- `CatalogSemanticFields` — the DOI triple plus path role; `DomainOmittedInPath` for elision rendering
- Picker/constructor menu data — `CommandPickerChoice`, `ArgConstructorBinding`, `ArgTailProfile` (structured arg menus from catalog profiles)
- `CommandDescriptor.allPaths` — canonical path plus non-empty aliases

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Gdl.Command

// Arg-tail wire → kind (unknown/empty falls back to Optional)
let kind = CommandArgTailPolicy.parse "picker:files"

// Commit-time auto-run gate: does "go to file " (trailing space) run the command?
let autoRun =
    CommandArgTailPolicy.shouldAutoRunOnCommit
        CommandArgTailKind.Picker   // kind
        false                       // isExactPath
        true                        // endsWithSpace
        false                       // hasArgTail

// Canonical vs alias, case-insensitive
// given descriptor : CommandDescriptor with Path = "/goto/file"
let role = CatalogRouteEntry.resolvePathRole descriptor "/GOTO/File"  // Canonical
```

## Problem

- Command palettes and chat surfaces each re-invent the command row by hand:
  - what arguments a command takes after its path — none, optional, required, a picker, an implicit selection or line range
  - whether typing the path plus a space should auto-run, or wait
  - which spelling is canonical and which are aliases
- Those answers end up as per-surface strings and if-chains, so two clients disagree about the same command.

## Solution

- One descriptor row carries the whole contract: DOI naming, canonical path + aliases, arg-tail policy, picker and constructor menus.
- Arg-tail and auto-run semantics become named, testable functions instead of per-client strings.
- Battle-tested as the command catalog spine of the Guiders federation — [CIDE ADR-0150](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0150-slash-line-canonical-resolution.md) · [CIDE ADR-0154](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0154-slash-catalog-domain-object-intent.md) · [ADR-0047](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0047-command-for-doi.md).

## When not to use

- Command file parsing (TOML/JSON profiles) — this is the IR; wire formats are parsed by catalog adapters.
- Command execution or dispatch — descriptors describe, surfaces execute.
- Keybinding capture — gesture normalization lives in `Notations.Keyboard`, bindings in `Gdl.Command.Binding`.

## Learn more

- [CIDE ADR-0150 — slash line canonical resolution](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0150-slash-line-canonical-resolution.md)
- [CIDE ADR-0154 — slash catalog DOI](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0154-slash-catalog-domain-object-intent.md)
- [ADR-0047 — command for DOI](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0047-command-for-doi.md)
- [ADR-0044 — command catalog scope](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0044-command-catalog-scope.md)
