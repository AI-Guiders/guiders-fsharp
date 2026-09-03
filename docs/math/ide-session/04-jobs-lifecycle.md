# Snapshot jobs, lifecycle, on-demand orchestration

| | |
|---|---|
| **Package** | [IDE Session axioms](README.md) |
| **Hub** | [ide-session-axioms-v0.md](../ide-session-axioms-v0.md) |

### 2.9 Snapshot job (общий шаблон)

Любая on-demand операция над \( \varphi \):

\[
\mathsf{job}(k, \pi, \varphi_r, \theta) \to \mathsf{Result}
\]

где \( k \in \mathbb{K} \), \( \theta \) — спецификация (build target, refactor id, fix id, …).

| \( k \) | \( \mathsf{Result} \) | Apply к live |
|---------|----------------------|--------------|
| \( \mathsf{Build} \) | артефакты (dll, js bundle, …) | опционально feed; **не** трогает sources |
| \( \mathsf{TestRun} \) | test report | нет |
| \( \mathsf{CodeTransform} \) | **patch** \( \Delta_{\mathsf{fs}} \) (edits + optional renames) | `apply(\Delta)` → scope §5.2 |
| \( \mathsf{CodeStyle} \) | **patch** \( \Delta_{\mathsf{fs}} \) (format / style) | `apply(\Delta)` → \( \mathsf{FileChange} \); semantic path → Hoare §2.11 |
| \( \mathsf{StaticAnalysis} \) (heavy) | report / diagnostics snapshot | нет (или merge в per-file cache) |

**Preview:** \( \mathsf{job}(\mathsf{CodeTransform}, \ldots) \) с флагом `dryRun` — чистая функция на \( \varphi \), без `apply`.

**Requires:** \( \mathsf{CodeTransform} \) обычно \( E_{\mathsf{req}} \) → \( \mathsf{CompilerServices} \) на **том же** \( \varphi \) (transient semantic model), не обязательно live \( M \).

## 6. Аксиомы lifecycle (переходы)

Пусть \( \mathrm{advance}(\mathcal{S}, \phi') \) — операция оркестратора.

| ID | Формулировка |
|----|----------------|
| **L1** | Допустим только если \( \phi' \ge \sigma \) **или** явный rollback по политике |
| **L2** | \( \mathrm{advance}(\cdot, \mathsf{DesignTime}) \) не требует \( \mathsf{CompileTime} \) materialization |
| **L3** | \( \mathrm{advance}(\cdot, \mathsf{CompileTime}) \) **не materialize** \( \mathsf{Build} \) автоматически; Build — **on-demand** (\( \mathsf{EnsureCapability}(\mathsf{Build}, \varphi) \)) |
| **L4** | \( \mathsf{Build}(\pi)@\varphi_r \) выполняется **в фоне**; live \( \mathsf{CompilerServices}(\pi) \in M \) **не evict** из-за старта/завершения build |
| **L5** | После успешного build: feed по \( E_{\mathsf{feed}} \) (Build → CompilerServices) — **опционально** и только по политике; иначе артефакт живёт отдельно от DesignTime |

**Интуиция:** DesignTime — интерактивный слой; CompileTime/TestTime — **запрашиваемые** subprocess-job'ы над frozen snapshot, не побочный эффект каждого edit.

---

## 7. On-demand snapshot jobs (Build, Test, Refactor, …)

CompileTime / TestTime / transform capability **не привязаны** к invalidation scope из §5.2.

| ID | Формулировка |
|----|----------------|
| **OD1** | \( \mathsf{FileChange} \), \( \mathsf{ProjectFileCrud} \) **не** materialize \( \mathsf{Build} \), \( \mathsf{TestRun} \), \( \mathsf{CodeTransform} \) |
| **OD2** | Materialize on-demand только по явному запросу (user, `cdp_build`, refactor preview, LUT, CI) |
| **OD3** | Запрос → \( \varphi \leftarrow \mathsf{freeze\_tree}(m, \Pi_0) \) (§2.8b) → \( \mathsf{job}(k,\pi,\varphi,\theta) \); live \( M \) не трогаем до `apply` |
| **OD4** | Job @ \( \varphi_r \) **не invalidate** при последующих \( \mathsf{FileChange} \); результат помечен `atRevision = r` |
| **OD5** | **LUT:** `freeze → Build → TestDiscovery → TestRun` (debounced) |
| **OD6** | **Refactor / fix:** \( \mathsf{plan}(\theta, \varphi) \to \Delta \); \( \mathsf{apply} \) → \( G' = G \oplus \Delta_G \) (§2.10); invalidation **после** apply по \( \mathsf{scope}(\Delta) \) |

```text
Live session (DesignTime)          Snapshot job lane
─────────────────────────          ──────────────────
FileChange → refine Σ_π (nodes)   freeze_tree(m, Π₀) → φ_r(T)
CompilerServices(π) ∈ M            job(k, π, φ_r(T), θ):
  (continues uninterrupted)          Build      → artifacts
                                   TestRun    → report
                                   CodeTransform → Δ (preview or apply)
```

**Следствие:** blanket «CompileTime invalidates DesignTime» **отвергнут** для interactive path.

