# AIGuiders.Platform.Modeling.Paths

LogicalPath — a repo/workspace-relative path value with canonical forward slashes, plus the boundary mapper to OS-absolute paths.

## What you get

- `LogicalPath` — a struct, repo/workspace-relative path in canonical form:
  - `Create` / `Parse` / `TryParse` / `Normalize` — normalize on the way in
  - `Combine` — join segments without double-slash guessing
  - `StartsWith` — prefix check (ordinal-ignore-case by default)
  - `AsDocPath` — strip a leading slash for document addressing
  - `MatchesAnchor` — anchor match: exact, suffix under the anchor, or file-name anchor
  - custom equality on the canonical value (ordinal), no ordering
- `LogicalPathOps.normalize` — the canonicalization rules as one function: backslashes to `/`, repeated `/` collapsed, leading `.`/`/` and trailing `/` trimmed
- `PathBoundary` — the logical/physical boundary at a workspace root:
  - `toLogical` — absolute path under the root → `LogicalPath` (empty when the path escapes the root)
  - `toPhysical` — `LogicalPath` + workspace root → absolute path string
  - `tryCanonicalPhysical` — canonical absolute form of a raw path, null on failure
- TruePath does the physical-path heavy lifting inside `PathBoundary` but is never exposed — the public surface is string + `LogicalPath`

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Paths

// Canonical form: backslashes in, forward slashes out
let doc = LogicalPath.Create @"src\App\Program.fs"     // "src/App/Program.fs"

// Compose segments
let nested = (LogicalPath.Create "src").Combine "App/Program.fs"

// Prefix check
let inSrc = nested.StartsWith(LogicalPath.Create "src")  // true

// Cross the workspace boundary back to an OS-absolute path
let abs = PathBoundary.toPhysical @"D:\repo" nested      // "D:\repo\src\App\Program.fs"
```

## Problem

- Repo-relative identity keeps breaking in subtle ways: `src\App\Program.fs` vs `src/App/Program.fs` vs `./src//App/Program.fs` are the same file, and string-compare says no.
- `System.IO.Path` is OS-local — the same code answers differently on Windows and POSIX, so cross-machine identity (anchors, saved references, docs pointing at code) drifts.
- Without one canonical value type, every workspace feature re-rolls its own `Replace('\\','/')` + `GetFullPath` dance, each with its own edge cases.

## Solution

- One canonical logical form carried in a value type: normalize on construction, compare ordinally, join safely — and a single boundary module that owns the OS-absolute world (TruePath stays an implementation detail at the IO edge).
- The path layer of the Guiders federation ([ADR-0050](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0050-paths-guild-logical-physical.md)) — the base dependency of navigation, language-intelligence relations and IDE session modeling in this solution.

## When not to use

- Wire paths — slash commands and catalog phrases (`buffer open`, `import <grain/…>`) are not filesystem paths and deliberately live elsewhere (Notations/Authoring, see ADR-0050).
- Segment-aware prefixing — `StartsWith` is a string prefix check: `"srcFoo/x"` starts with `"src"`, and comparison is case-insensitive by default. If you need segment boundaries or case sensitivity, enforce them at your call site.
- Physical file IO — this package canonicalizes and maps; open/read/write stays in your IO layer.

## Learn more

- [ADR-0050 — Paths guild: logical · physical boundary](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0050-paths-guild-logical-physical.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
