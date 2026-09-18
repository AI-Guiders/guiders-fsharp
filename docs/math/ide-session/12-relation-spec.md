# RelationSpec witness (relation_spec_i)

Federation attach + resolve uses typed witnesses before graph persistence.

## Notation

For session graph `G`, document registry `ω`, and relation witness `R`:

```text
relation_spec_i : RelationSpec
  | CodeEdit(codeTarget)
  | DocToCode(docPlace, codeTarget)
  | Diag(diagnosticRef)
  | Address(addressRef, artifactHint)
  | Nav(navSeed)
  | Resource(resourcePath, codeTail?)
```

Persisted edges use `Relation` in `G` with `RelationType` — not bracket axis strings.

## Wire projection

Bracket `Kind:` canon maps to `relation_spec_i` at parse boundary only:

| Kind | Keys | Witness |
|------|------|---------|
| CodeEdit | File, Member, Scope? | `CodeEdit` |
| Diag | DiagnosticId | `Diag` |
| Nav | File, Line?, Column?, Member?, Command?, Go?, Solution? | `Nav` |

Legacy `F:`/`M:`/`L:` wires are not SSOT — transitional Execution ingest only (`RelationWireBoundary.TryParseDocScan`); nav emit/parse canon is `Kind:Nav` / `Kind:CodeEdit` only (ship-60).

## Document identity (`doc_id` ↔ `DocumentRef`)

Session registry `ω` assigns stable document ids:

```text
doc_id : DocId = Identity<Document, NumericId>
```

`DocumentRef` in witnesses is either:

| Form | When |
|------|------|
| `DocId doc` | Post-bootstrap registry identity (graph `ω`, diagnostic index ingest) |
| `File path` | Wire parse boundary before numeric id assignment |

Kind-wire `File:` keys map to `DocumentRef.File(LogicalPath.Create path)` at parse time. Ingest binds `LogicalPath` → `DocId` via `DocumentRegistryOps`; subsequent `RelationSpec` materialization prefers `DocId` in stored `Locus`.

## Scene projection

Navigation scene edges are **projections** of `Relation` or provisional related-neighbor discovery — see `SceneProjection.relationTypeWire`.
