package: AIGuiders.Platform.Modeling.Ide.Session.Ports.Workspace
tagline: Workspace document graph port: pure link extraction between documents (markdown links, quoted path literals) from pre-loaded snapshots — .md/.json/.toml/.yaml.

## What you get

- `WorkspaceDocumentSnapshot` — one pre-loaded document: `Path`, `Content`, `LastWriteUtc`
- `WorkspaceLinks.linksFromSnapshots` — the physical layer of the workspace graph:
  - markdown documents: inline `[text](target)` links
  - json/toml/yaml: quoted string literals ending in a document extension
  - external refs (`http(s)`, `mailto:`, `#anchor`) dropped; targets resolved against the document dir and kept only when they are known documents
- `WorkspaceGraphPort.build` — snapshots → `WorkspaceGraph` (root + paths + links)
- `WorkspaceGraphPort.fingerprint` — `root|docs=N|latestWriteUtc` change stamp
- `WorkspaceLinks.extensions` / `isDocument` — the closed document extension set (`.md` `.json` `.toml` `.yaml` `.yml`)

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Ide.Session.Ports.Workspace

// Pre-loaded snapshots (you read the files; the port stays pure)
let snapshots =
    [ { Path = @"D:\docs\README.md";  Content = "See [setup](setup.md).";   LastWriteUtc = System.DateTime.UtcNow }
      { Path = @"D:\docs\setup.md";   Content = "# Setup";                  LastWriteUtc = System.DateTime.UtcNow } ]

let graph = WorkspaceGraphPort.build @"D:\docs" snapshots
// README.md --link--> setup.md is now an edge in the workspace graph

WorkspaceGraphPort.fingerprint @"D:\docs" snapshots   // "D:\docs|docs=2|<latest>"
```

## Problem

- Document-centric workspaces (docs repos, knowledge bases, config folders) hide their structure:
  - which files reference which files is only visible by grepping
  - link extraction is re-implemented per tool, mixing IO with parsing so it can't be tested
- So "what breaks if I rename this file" has no cheap answer.

## Solution

- Link extraction is one pure function over snapshots: IO at the edge, graph building testable with synthetic content.
- The result plugs into the session `WorkspaceGraph` like any other layer.
- Battle-tested as the workspace port of the Guiders session model — [FSHARP ADR-0004](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md).

## When not to use

- Reading the files — snapshots come pre-loaded from the Execution side; no IO here.
- Code-level references (project references, usings) — that is `Ports.DotNet` / `Ide.Session` relation graphs.
- Rich markdown (reference-style links, includes, wikilinks) — the extractor knows inline links and quoted path literals; anything fancier needs your own pass.

## Learn more

- [FSHARP ADR-0004 — ide-session modeling ownership](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md)
