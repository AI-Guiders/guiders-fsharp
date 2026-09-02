# Conformance — golden sessions

| | |
|---|---|
| **Package** | [IDE Session axioms](README.md) |
| **Hub** | [ide-session-axioms-v0.md](../ide-session-axioms-v0.md) |

## 11. Conformance: golden sessions

**Принцип:** математика без executable proof — декорация. Каждая нормативная тройка Хоара / ST-аксиома, которую мы реально ship'им, должна иметь **golden session** — фикстура \( (G, \mathrm{contents}, \theta) \) + тест.

### 11.1 Каркас (F#)

| Математика | Код |
|------------|-----|
| \( \mathsf{Sat}(P,G,Q) \) | `HoareChecker.sat*` / `refactorPreserves` |
| \( \Delta = (\Delta_{\mathsf{fs}}, \Delta_G) \) | `SessionPatch` + `GraphPatch` |
| \( \mathsf{plan}_{\mathsf{rename}} \) | `RefactorPlan.planRename` |
| \( \mathsf{plan}_{\mathsf{move}} \) | `RefactorPlan.planMoveTypeToFile` / `planMovePath` |
| ST3 / ST5 style gate | `StyleConformance.evaluateAutoApply` |
| Golden fixture | `GoldenSession` + `tests/.../GoldenSessions` |

`TypecheckVerdict` — **port** (FCS/Roslyn); в golden tests stub `Passed` / `NotRun`.

### 11.2 Обязательные golden sessions (v0)

| ID | Сценарий | Assert |
|----|----------|--------|
| **GS1** | `plan(rename local)` на F# project | `Conformance.runRenameGolden` → `Satisfied` @ `TypecheckVerdict.Passed` |
| **GS2** | rename без typecheck | `Violated` с `Q_types` |
| **GS3** | vendor style output, typecheck `NotRun` | `StyleApplyDecision.Rejected` (ST5) |
| **GS4** | vendor + `Passed` | `PreviewOnly` (ST3 — не default commit path) |
| **GS5** | proven + `Passed` | `AutoApplyAllowed` |
| **GS6** | `plan(move type → file)` | `Conformance.runMoveTypeGolden` → `Satisfied`; \( \omega \) + `validate(G')` |

### 11.3 Расширение (Phase 2+)

| Golden | Аксиома |
|--------|---------|
| `FileChange` не evict \( M \) | §5.2 + materialized state stub |
| build @ \( \varphi_r \) survive edit | OD4 |
| `freeze_tree` + mixed τ slnx | §2.8b, §9.12 |
| invalid `Δ` breaks WF | RF3 |

Новая аксиома в `06-transforms.md` / `02-invalidation.md` **без** строки в этой таблице = **draft**, не shipped law.

### 11.4 CI

`dotnet test tests/AIGuiders.Platform.Modeling.Ide.Session.Tests` — conformance suite обязателен на PR, затрагивающий `Modeling.Ide.Session` или `docs/math/ide-session/**`.

---

*Math proposes; golden sessions dispose.*
