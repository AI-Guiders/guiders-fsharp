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
| Nav | File, Line?, Column?, Command?, Go?, Solution? | `Nav` |

Legacy `F:`/`M:`/`L:` wires are not SSOT — transitional Execution parsers only.

## Scene projection

Navigation scene edges are **projections** of `Relation` or provisional related-neighbor discovery — see `SceneProjection.relationTypeWire`.
