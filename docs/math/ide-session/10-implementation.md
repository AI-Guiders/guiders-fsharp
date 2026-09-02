# Code mapping, example, evolution

| | |
|---|---|
| **Package** | [IDE Session axioms](README.md) |
| **Hub** | [ide-session-axioms-v0.md](../ide-session-axioms-v0.md) |

## 10. Соответствие коду (v0)

| Математика | F# (`Modeling.Ide.Session`) |
|------------|-----------------------------|
| \( \mathbb{P} \) | `ProjectNode` list |
| \( \kappa_\pi \) | `ProjectNode.Capabilities` |
| \( V \) | `GraphNodeId` = `ProjectNode` \| `CapabilityNode` |
| \( E_{\mathsf{proj}} \) | `ProjectEdge` list; port `Ports.DotNet.DotNetSlnxGraphPort` |
| \( E_{\mathsf{req}} \) | `SessionEdge` with `Kind = Requires`; WF7 local-only |
| \( \omega \) | `SolutionGraph.FileOwnership` |
| \( \mathcal{S} \) | `SolutionSession` |
| \( \rho_0 \) | `SessionPolicy` (v0 face; → merge в graph) |
| \( \psi \), \( E_{\mathsf{gov}} \) | **ещё нет** |
| WF1–WF8 | `GraphValidation.validate` (WF7–WF8 shipped Phase 1b) |
| Invalidation scope lattice | `InvalidationScope` (`promotes`, `max`) |
| \( M, \mu \) | **ещё нет** → `Execution.Ide.Session` Phase 2 |
| \( \Sigma_\pi \), `refine` | **ещё нет** → port `CompilerServices` / semantic substrate (Phase 2) |
| \( \varphi_r \), `freeze` / `freeze_tree` | **ещё нет** → `FrozenSnapshot`, `FrozenTreeComposition`, `FreezeMode` (Phase 2) |
| \( \pi_{\mathsf{ws}} \) | **ещё нет** → port `WorkspaceView` / materialize facade (Phase 2) |
| \( \mathsf{Refactor} \), \( \oplus \) | `RefactorPlan`, `SessionPatch`, `FileSystemPatch`, `GraphStructurePatch` |
| \( \mathsf{scope}(\Delta) \) | `SessionPatch.scope` |
| RF1–RF8, ST1–ST5, LD1–LD6 | `HoareChecker`, `StyleConformance`, `Conformance` (GS1–GS5); ledger **ещё нет** |
| \( \Lambda \), Timeline | **ещё нет** → `RevisionLedger`, `GitPin`, `CodeHistoryTimeline` (Phase 2–3) |

---

## 11. Минимальный пример (для sanity)

Один F# project \( \pi \), capabilities \( \mathsf{CompilerServices} \), \( \mathsf{Build} \):

\[
E_{\mathsf{req}} = \{ (\mathsf{Build}, \mathsf{CompilerServices}) \}
\]

(WF4: DAG OK.)

Пользователь жмёт build (или LUT scheduler): \( \varphi \leftarrow \mathsf{freeze}(\pi) \) → \( \mathsf{Build}(\pi)@\varphi \) в фоне; CompilerServices остаётся в \( M \). Feed в DesignTime — только если политика / \( E_{\mathsf{feed}} \) и явный refresh.

---

## 12. Эволюция документа

1. ~~Зафиксировать invalidates~~ — **D scope-indexed** (§5.2).  
2. ~~On-demand Build over frozen snapshot~~ — §2.8–2.9, §7, L3–L5.  
3. ~~Snapshot jobs + build substrate~~ — §2.9, §7.3, OD6, BS1–BS4.  
4. ~~Policy as attributes / governs edges~~ — §2.6, §7.2, P1–P4.  
5. ~~Incremental build/compile + IL cache~~ — §7.4, IB1–IC3.  
6. ~~Capability edges local + \( E_{\mathsf{proj}} \)~~ — §2.3, §9.8, WF7–WF8.  
6b. ~~Frozen Tree Composition~~ — §2.8b, §9.10 (shared analyzer, build closure).  
6c. ~~Sub-file refinement (Σ_π, syntax/semantic nodes)~~ — §5.2a, I6–I8.  
6d. ~~Sniper-scoped emit (semantic node → minimal U')~~ — §7.4, SN1–SN6.  
6e. ~~Refactor as G → G' (plan/apply, Δ_fs + Δ_G)~~ — §2.10, RF1–RF8, Θ classes.  
6f. ~~Code Style & EditorConfig in G~~ — §2.11, ST1–ST5.  
6g. ~~Revision ledger + Git subgraph + Timeline~~ — §2.12, §9.11, LD1–LD6.  
6h. ~~Planet / Gdl как \( \tau \) без special case~~ — §1.5, §9.12, T1–T3; `ProjectCapabilityCatalog.forKind`.  
7. ~~Код: `InvalidationScope`, …, `E_proj`~~ — Phase 1b (`InvalidationScope`, `ProjectEdges`, `Ports.DotNet`).  
8. ~~Порт slnx / gdlproj~~ — `DotNetSlnxGraphPort.load` / `loadSession`; gdlproj **ещё нет**.  
9. ~~`GraphValidation`: WF7, WF8~~ — shipped.  
10. Orchestrator host: `Execution.Ide.Session.SolutionSessionHost.Open` (platform).  
11. ~~Доказуемые свойства~~ — golden sessions §11 (`ConformanceGoldenSessionTests`); runtime props (FileChange/M) Phase 2+.

### Будущие ветки (не v0, не блокируют Dash Studio)

| Ветка | Суть | Сейчас |
|-------|------|--------|
| **GuidersBuild** | \( \Gamma_\pi + \mathcal{H} \), incremental; emit via **Roslyn/FCS** drivers | Phase 2–4 orchestrator |
| **GDL → IL** | declare-time GDL как frontend напрямую в ECMA-335; язык-агностичный interchange | **отдельная идея / fork**; capture: [ANUI GDL/IL rethink](../../../../../../../agent-notes/knowledge/work/projects/door-to-singularity/ai-native-ui/note-anui-gdl-il-rethink-v0.md) |
| **Свой frontend** | полный compiler с нуля | только если GDL/Planet станет продуктом, не платформой |

**v0 emit policy:** reuse Roslyn/FCS; не писать свой `csc`/`fsc` — иначе не ship'им платформу.

**Теория GDL → IL:** IL как lingua franca — любой язык **в** экосистему через emit, любой **из** через metadata/reflection; F#/C# становятся optional surface, не SSOT. Заманчиво, но **после** session graph + Dash Studio vertical slice.

### Federation reframe of CDP (стратегия)

Pre-federation CDP = habitat без graph SSOT. После Modeling слоя почти каждая CDP-фича нормализуется в \(G\) + ports + conformance — CDP остаётся dogfood host, не владельцем модели.

См. **[GUIDERS-FSHARP-ADR-0005](../adr/GUIDERS-FSHARP-ADR-0005-federation-reframe-cdp-features.md)** (feature map, Correspondence pilot, migration rules).

---

*Обсуждение: правь аксиомы прямо в PR/issue; F# подстраивается под формулы, не наоборот.*

**Forge render:** GitHub не рендерит LaTeX в preview — читаемая математика в human_view: [FORGE-ADR-0071](https://github.com/AI-Guiders/agent-forge/blob/main/design/FORGE-ADR-0071-math-markdown-block-renderer.md) (`markdown-math` plugin, KaTeX, `docs/math/**` profile).