# AIGuiders.Platform.Modeling.CodeCenter

A language-neutral document editing core — structural edits over a document graph, a committed-edit ledger, and replay-based undo; parsing stays behind your language profile.

## What you get

- Edit vocabulary (`EditTypes`):
  - `MechanicalEdit` — point/region/document text splice (`RemovedSpan` + `InsertedText`)
  - `StructuralEdit` — `RenameMember`, `InsertBlock`, `RemoveBlock`, `MoveMember`, `Extract`
  - `InverseQuality` — `Exact` / `Partial` / `Unspecified`
  - `ProjectionDescriptor` + `DocumentProjectionKind` (Text / Diagram / Tree / Form / Preview)
- `DocumentGraph` — `DocumentSnapshot` (text + node map + token spans + folding regions) with pure node ops: `findNodeAt`, `findNodeByName`, `listNodes`, `tryResolve`, `renameNode`, `insertBlock`, `removeBlock`, `moveMember`, `extractMember`, `applyMechanicalPatch`; `DocumentNodeIdWire` for `node:{counter}` wire round-trip
- Edit ledger (`LedgerTypes`, `RePlannableThetaRegistry`):
  - `LedgerEntryDoc` — revision, invalidation scope, theta (the edit), delta patch, phi snapshot ref, anchor, git pin, inverse edit
  - `ReplayMode` — `DeltaRequired` / `RePlannable` / `DeltaOrReplan`; `CommitPolicy`; registry rows for all 8 edit kinds plus `satisfiesEntry`
- `DocumentSession` — the aggregate: `ApplyMechanicalEdit` (ephemeral) → `CommitMechanicalBatch`, `ApplyStructural` (commits a ledger entry with inverse + quality), `TryUndo` / `ReplayToRevision` (replay from the G0 snapshot — delta first, re-plan where the registry allows), `SyncFromText` with partial-parse holdback
- Language seam (`DocumentLanguageProfile`): `IDocumentLanguageProfile` (`Rebuild`, `PlanStructural`, projection ads), `SurfaceFamily` (nine document surfaces), `NeutralDocumentLanguageProfile`, `RebuildLanguageProfile`
- C# interop helpers (`MechanicalEditBridge`, `StructuralEditBridge`), completion provider types (`Completion`), `RefreshScope` instrumentation

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.CodeCenter
open AIGuiders.Platform.Modeling.Core.Identity

// Bring your own parser: text -> snapshot. Here one node per line,
// labelled "container member" style (rename edits the member token).
let rebuild (text: string) : DocumentSnapshot =
    let mutable offset = 0

    let nodes =
        text.Split('\n')
        |> Array.mapi (fun i (line: string) ->
            let id = NodeId.mint (NumericId.ofCounter (int64 i + 1L))
            let label = line.Split(' ') |> Array.truncate 2 |> String.concat " "
            let node = { Id = id; Name = label; Start = offset; End = offset + line.Length; Parent = None }
            offset <- offset + line.Length + 1
            id, node)
        |> Map.ofArray

    { Text = text; Nodes = nodes; TokenSpans = []; FoldingRegions = [] }

let session =
    DocumentSession.create "doc-1" "let add a b = a + b\nlet mul a b = a * b" rebuild

let addNode = session.GetDocumentNodes().[0]

match session.ApplyStructural(RenameMember(addNode.Id, "sum")) with
| Ok (after, entry) ->
    printfn "rev %d: %s" after.Revision after.Text   // "let sum a b = a + b ..."
    match after.TryUndo() with
    | Ok undone -> printfn "undone to rev %d" undone.Revision
    | Error e -> printfn "%s" e
| Error e -> printfn "%s" e
```

## Problem

- Structural edits (rename, move, extract) over an editor buffer degenerate into text splices; undo becomes a fragile inverse stack that drifts from the parse tree.
- Every editor host forks this machinery per language, so replay and undo semantics never match between tools.

## Solution

- Edits are ledger entries: delta patch, optional inverse (with honest quality), and a snapshot-ref pin; undo replays from G0 — stored delta first, re-planning the edit where the registry allows — instead of trusting an inverse stack.
- The document graph is language-neutral; your `IDocumentLanguageProfile` owns parsing and structural planning, so the session stays testable in memory.
- Federation Code Center SSOT — ledger slice and replay semantics per [ADR-0066](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0066-code-center-federation-product.md); language profile seam per [ADR-0067](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0067-language-profile-federation-model.md).

## When not to use

- Expecting a parser or an editor UI: `NeutralDocumentLanguageProfile.Rebuild` yields an empty graph — nodes and structural planning come only from your profile.
- `MoveMember` / `Extract` are pilot placeholders at graph level: move ignores the target parent/index (it appends a "moved" line) and extract wraps the block as text — not real refactors.
- Single-document scope: no multi-file transactions, and `ApplyFromRelationSpec` is a stub (CodeEdit TreeNode only).

## Learn more

- [ADR-0066 — Code Center federation product](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0066-code-center-federation-product.md)
- [ADR-0067 — Language profile federation model](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0067-language-profile-federation-model.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
