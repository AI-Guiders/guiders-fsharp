# IDE Session — матмодель и аксиоматика (v0)

| | |
|---|---|
| **Status** | Draft (evolving with implementation) |
| **Date** | 2026-09-02 |
| **SSOT code** | `AIGuiders.Platform.Modeling.Ide.Session` (guiders-fsharp) |
| **Architecture** | [GUIDERS-ADR-0062](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md) |
| **Ownership** | [GUIDERS-FSHARP-ADR-0004](../adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md) |

Документ для обсуждения **законов** модели. F# — нотация; нормативны формулы и инварианты.

---

## 0. Обозначения

- \( \mathbb{P} \) — множество project-узлов  
- \( \mathbb{C} \) — множество capability-узлов  
- \( V = \mathbb{P} \uplus \mathbb{C} \) — дизъюнктное объединение (узлы разных сортов)  
- \( \Phi \) — множество фаз lifecycle  
- \( \top \) — множество топологий materialization  
- \( \mathcal{A} \) — множество атрибутов (меток на узлах/рёбрах)  
- \( \mathcal{P} \) — множество **policy-ключей** (lazy, evict, feed, buildDriver, …) — v0: строковые ключи в \( \mathcal{A}^* \), позже typed schema  
- \( \mathrm{paths} \) — множество путей файловой системы  
- \( \mathrm{id}(\pi) \) — идентификатор project-узла \( \pi \in \mathbb{P} \)

---

## 1. Сорта и элементарные множества

### 1.1 Фазы

\[
\Phi = \{ \mathsf{Unloaded},\ \mathsf{DesignTime},\ \mathsf{CompileTime},\ \mathsf{RunTime},\ \mathsf{TestTime} \}
\]

Частичный порядок (решётка цепочки):

\[
\mathsf{Unloaded} \le \mathsf{DesignTime} \le \mathsf{CompileTime},\quad
\mathsf{DesignTime} \le \mathsf{RunTime},\quad
\mathsf{DesignTime} \le \mathsf{TestTime}
\]

\( \mathsf{RunTime} \) и \( \mathsf{TestTime} \) **не сравнимы** между собой; обе выше \( \mathsf{DesignTime} \).

**Аксиома Φ1 (монотонность сессии):** фаза сессии \( \sigma \in \Phi \) — верхняя грань фаз проектов, которые оркестратор держит активными (политика уточняется в §7).

### 1.2 Топологии

\[
\top = \{ \mathsf{InProcess},\ \mathsf{OutOfProcess},\ \mathsf{SubprocessTool},\ \mathsf{Adaptive} \}
\]

### 1.3 Виды capability

Фиксированное конечное множество \( \mathbb{K} \) (расширяемое версией схемы):

\[
\mathbb{K} = \{ \mathsf{CompilerServices},\ \mathsf{StaticAnalysis},\ \mathsf{Build},\ \mathsf{TestDiscovery},\ \mathsf{TestRun},\ \mathsf{CodeTransform},\ \mathsf{LspBridge} \}
\]

\( \mathsf{CodeTransform} \) — рефакторинги, code fixes, codegen, rename solution-wide: **тот же** on-demand паттерн, что Build/Test (§7).

### 1.4 Виды project

\[
\mathbb{T} = \{ \mathsf{DotNet},\ \mathsf{Node},\ \mathsf{Gdl},\ \mathsf{Planet} \}
\]

Язык (C#, F#, …) — **метка** на \( \mathsf{DotNet} \) или \( \mathsf{Planet} \), не отдельный сорт графа.

---

## 2. Структуры

### 2.1 Project-узел

\[
\pi = (\mathrm{id},\ \tau,\ \mathsf{path},\ \phi_\pi,\ \kappa_\pi)
\]

где \( \tau \in \mathbb{T} \), \( \phi_\pi \in \Phi \), \( \kappa_\pi : \mathbb{K} \rightharpoonup \mathcal{A} \) — **частичное** отображение (не каждый project обязан иметь все capability).

### 2.2 Capability как точка в \( \mathbb{C} \)

\[
c = (\mathrm{id}(\pi),\ k,\ \alpha)
\quad\text{с}\quad k \in \mathbb{K},\ \alpha \in \mathcal{A}
\]

Узел capability **уникально** задаётся парой \( (\mathrm{id}(\pi), k) \).

### 2.3 Рёбра

Четыре бинарных отношения на \( V \times V \) (v0: три семантических + policy overlay):

\[
E_{\mathsf{req}},\ E_{\mathsf{inv}},\ E_{\mathsf{feed}},\ E_{\mathsf{gov}} \subseteq V \times V
\]

\( E_{\mathsf{gov}} \) — **policy governs**: «этот узел/политика **управляет** поведением того» (session → capability, catalog → project, …). Может быть пустым, если всё задано атрибутами.

Каждое ребро несёт метку \( \lambda(e) \in \mathcal{A}^* \) (словарь строк — v0); policy-ключи — подмножество ключей в \( \lambda \).

### 2.4 Владение файлами

\[
\omega : \mathrm{paths} \rightharpoonup \mathbb{P}
\]

частичное отображение «файл → владеющий project».

### 2.5 Статический граф решения

\[
G = (\mathbb{P},\ \mathbb{C},\ E_{\mathsf{req}},\ E_{\mathsf{inv}},\ E_{\mathsf{feed}},\ E_{\mathsf{gov}},\ \omega,\ \kappa,\ \psi)
\]

где \( \psi : V \rightharpoonup \mathcal{A}^* \) — **policy-атрибуты на узлах** (в т.ч. на \( \pi \) и \( c \)); дублируют/уточняют поля в \( \kappa \).

### 2.6 Сессия

\[
\mathcal{S} = (G,\ \sigma,\ \rho_0)
\]

где \( \sigma \in \Phi \) — фаза сессии; \( \rho_0 \subseteq \mathcal{A}^* \) — **defaults сессии** (anchor overlay), не отдельная «магическая» политика.

**Эффективная политика** — разрешение по иерархии:

\[
\rho_{\mathsf{eff}}(v, p) =
  \psi(v)(p)
  \;\|\;
  \lambda(e)(p)\ \text{для } e \in E_{\mathsf{gov}},\ e=(u,v)
  \;\|\;
  \rho_0(p)
  \;\|\;
  \rho_{\mathsf{catalog}}(k, p)
\]

(первое определённое значение; \( v \in \mathbb{C} \Rightarrow \) смотрим также project-родителя \( \pi \)).

| Носитель | Примеры policy-ключей |
|----------|----------------------|
| \( \rho_0 \) (session) | `designTimeLoad=lazy`, `evictOnClose=true` |
| \( \psi(\pi) \) | `lut.enabled`, `restoreOnOpen` |
| \( \kappa_\pi(k) \subseteq \psi(c) \) | `buildDriver`, `topology`, `feedAfterBuild` |
| \( \lambda(e) \) на \( E_{\mathsf{gov}} \) | `(session → CS): warm=eager` |
| \( \lambda(e) \) на \( E_{\mathsf{inv}} \)/\( E_{\mathsf{feed}} \) | `onSuccess=evict`, `coalesce=true` |

**Принцип:** политика — **те же** атрибуты и отношения, что и остальной граф; оркестратор не читает отдельный конфиг в обход \( G \).

### 2.7 Materialized state (динамика, v0 sketch)

\[
M \subseteq \mathbb{C},\quad \mu : M \to \top
\]

\( M \) — множество capability, для которых оркестратор уже поднял handle; \( \mu(c) \) — **фактическая** топология после разрешения \( \mathsf{Adaptive} \).

### 2.8 Frozen snapshot (on-demand jobs)

Для фоновых и явно запрошенных операций — **не** живой буфер редактора, а **замороженный** срез поддерева:

\[
\varphi_r(\pi) = (\mathrm{subtree}(\pi),\ \omega_r,\ \mathrm{contents}_r,\ \kappa_{\pi,r})
\]

где \( r \) — ревизия (монотонный счётчик сессии или hash снимка).

\[
\mathsf{freeze}(\pi) \to \varphi_r(\pi)
\]

**Свойства:**

- \( \varphi_r \) **иммутабелен** после создания; правки файлов после \( r \) не меняют уже запущенный job.
- Job \( \mathsf{Build}(\pi)@\varphi_r \) **не читает** dirty live-state, пока политика не разрешит `BuildWithDirty` (v0: **запрещено** по умолчанию).
- Несколько job'ов на одном \( \pi \) с разными \( r \) допустимы; оркестратор может **коалесцировать** (оставить только последний \( r \)).

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
| \( \mathsf{StaticAnalysis} \) (heavy) | report / diagnostics snapshot | нет (или merge в per-file cache) |

**Preview:** \( \mathsf{job}(\mathsf{CodeTransform}, \ldots) \) с флагом `dryRun` — чистая функция на \( \varphi \), без `apply`.

**Requires:** \( \mathsf{CodeTransform} \) обычно \( E_{\mathsf{req}} \) → \( \mathsf{CompilerServices} \) на **том же** \( \varphi \) (transient semantic model), не обязательно live \( M \).

---

## 3. Аксиомы структурной корректности (static WF)

Эти аксиомы проверяет `GraphValidation.validate` (Phase 1).

| ID | Формулировка |
|----|----------------|
| **WF1** | \( \mathrm{id} \) инъективен на \( \mathbb{P} \) |
| **WF2** | \( \forall \pi.\ |\mathrm{dom}(\kappa_\pi)| = |\{k : \kappa_\pi(k)\ \text{defined}\}| \) и каждый \( k \) встречается **не более одного раза** |
| **WF3** | \( \forall e \in E_{\mathsf{req}} \cup E_{\mathsf{inv}} \cup E_{\mathsf{feed}} \cup E_{\mathsf{gov}}.\ e = (u,v) \Rightarrow u \in V \land v \in V \) |
| **WF4** | Граф \( (V, E_{\mathsf{req}}) \) — **ацикличен** (DAG на \( \mathsf{requires} \)) |
| **WF5** | \( \forall f \in \mathrm{dom}(\omega).\ \omega(f) \in \mathbb{P} \) |
| **WF6** | Если \( \alpha = \kappa_\pi(k) \) и \( \alpha.\mathsf{topology} = \mathsf{Adaptive} \), то \( \alpha.\mathsf{rules} \neq \emptyset \) |

**Замечание:** WF4 — именно про \( E_{\mathsf{req}} \). Циклы в \( E_{\mathsf{inv}} \) или \( E_{\mathsf{feed}} \) в v0 **не запрещены** (обсуждаемо).

---

## 4. Аксиомы атрибутов capability

Пусть \( \alpha.\mathsf{phase} \in \Phi \), \( \alpha.\mathsf{topology} \in \top \).

| ID | Формулировка |
|----|----------------|
| **A1** | \( \kappa_\pi(\mathsf{CompilerServices}) \) определено для каждого \( \pi \) с \( \tau \in \{\mathsf{DotNet},\mathsf{Node}\} \) **по умолчанию каталога** (не аксиома жёсткости — convention каталога) |
| **A2** | Для \( \mathsf{CompilerServices} \): \( \alpha.\mathsf{phase} = \mathsf{DesignTime} \) и \( \alpha.\mathsf{topology} \in \{\mathsf{InProcess},\ \mathsf{OutOfProcess},\ \mathsf{Adaptive}\} \) |
| **A3** | Для \( \mathsf{Build} \): \( \alpha.\mathsf{phase} = \mathsf{CompileTime} \), \( \alpha.\mathsf{topology} = \mathsf{SubprocessTool} \) |
| **A4** | \( \mathsf{SubprocessTool} \) допустима только при \( \alpha.\mathsf{phase} \in \{\mathsf{CompileTime},\ \mathsf{TestTime}\} \) |

---

## 5. Аксиомы рёбер (семантика)

### 5.1 Requires

\( (u,v) \in E_{\mathsf{req}} \) означает: **нельзя materialize \( v \), пока не materialized \( u \)**.

| ID | Формулировка |
|----|----------------|
| **R1** | \( E_{\mathsf{req}} \) ацикличен (дубль WF4) |
| **R2** | Materialize orchestrator соблюдает топологический порядок \( E_{\mathsf{req}} \) на \( M \) |

### 5.2 Invalidates — **решено: scope-indexed (вариант D)**

\( (u,v) \in E_{\mathsf{inv}} \) в **статическом** графе — структурные зависимости (каталог / явные рёбра).

**Runtime invalidation** — отдельно: событие \( \delta \) с **областью** \( \mathsf{scope}(\delta) \); сбрасывается **минимальный** \( \Delta \subseteq M \), а не вся сессия.

#### Области (от мелкой к крупной)

| Scope | CRUD / change | Примеры | Что трогаем в \( G \) | Что invalidates в \( M \) |
|-------|----------------|---------|------------------------|---------------------------|
| **FileChange** | содержимое файла | edit, save, watcher | dirty-bit на пути \( f \) (не обязательно менять \( G \)) | кэш диагностик / symbols **для \( f \)**; \( M \) capability **не evict** |
| **ProjectFileCrud** | файлы ↔ project | add/delete/rename `.fs`, glob hit | обновить \( \omega \), список sources в \( \kappa_\pi \) | \( \mathsf{CompilerServices}(\pi) \) → **stale**; evict только если stale не достаточно |
| **ProjectCrud** | сам project | csproj/fsproj, TFM, PackageReference, restore | перечитать \( \kappa_\pi \), возможно \( \mathrm{id}(\pi) \) | \( M \cap \mathrm{subtree}(\pi) \) — evict |
| **SolutionProjectCrud** | project ↔ solution | add/remove в slnx, rename project path | \( \mathbb{P} \), \( \omega \), рёбра на затронутых \( \pi \) | removed \( \pi \): evict subtree; added \( \pi \): lazy warm |

**Иерархия scope:**  
\(\mathsf{FileChange} \prec \mathsf{ProjectFileCrud} \prec \mathsf{ProjectCrud} \prec \mathsf{SolutionProjectCrud}\)  
(событие крупнее — blast radius не меньше).

#### Аксиомы invalidation

| ID | Формулировка |
|----|----------------|
| **I1** | \(\mathrm{invalidate}(\delta)\) вычисляет \(\Delta \subseteq M\) по \(\mathsf{scope}(\delta)\) и затронутым узлам; **запрещён** blanket evict всей сессии, если \(\mathsf{scope} \neq \mathsf{SolutionProjectCrud}\) multi-project |
| **I2** | Явные \( E_{\mathsf{inv}} \) **дополняют** scope-правила: если materialize Build и \( ( \mathsf{Build}, c) \in E_{\mathsf{inv}} \), успешный build может evict \( c \) даже при \(\mathsf{FileChange}\)-only политике на других файлах |
| **I3** | \(\mathsf{FileChange}\) **никогда** не требует `dotnet build` / CompileTime advance |
| **I4** | \(\mathsf{ProjectCrud}\) **может** потребовать re-materialize \(\mathsf{CompilerServices}(\pi)\), не трогая \(\pi' \neq \pi\) |
| **I5** | Phase advance (\(\mathsf{DesignTime} \to \mathsf{CompileTime}\)) — **отдельный** класс событий; не подменяет file CRUD, но может union с \(\Delta\) по \( E_{\mathsf{feed}} \cup E_{\mathsf{inv}} \) |

#### Операционная схема (два уровня CRUD)

```text
Solution ── Project CRUD     (slnx graph, membership)
    │
    └── Project ── File CRUD   (ω, sources, ownership)
            │
            └── File ── Changes   (dirty, per-file diag cache)
```

**Ответ на вопрос «invalidates когда?»** — когда пришло событие с известным scope; orchestrator выбирает \(\Delta\) по таблице выше + транзитивное замыкание по \( E_{\mathsf{inv}} \) **внутри** blast radius, не по всему \( V \).

| ID | Формулировка |
|----|----------------|
| **I1′** | Evict\( v \) удаляет \( v \) из \( M \); зависимые по \( E_{\mathsf{req}} \) **от** \( v \) помечаются stale, не обязательно немедленный evict (политика) |

*(Старые I1–I2 из черновика заменены I1–I5 выше.)*

### 5.3 Feeds

\( (u,v) \in E_{\mathsf{feed}} \) означает: **выход \( u \) обновляет вход \( v \)** (артефакты build → sources compiler).

| ID | Формулировка |
|----|----------------|
| **F1** | После успешного materialize \( u \) оркестратор помечает \( v \) как **stale** до refresh |
| **F2** | \( (\mathsf{Build}, \mathsf{CompilerServices}) \in E_{\mathsf{feed}} \) опционально по каталогу; не обязательно для v0 |

---

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
| **OD3** | Запрос → \( \varphi \leftarrow \mathsf{freeze}(\pi) \) → \( \mathsf{job}(k,\pi,\varphi,\theta) \); live \( M \) не трогаем до `apply` |
| **OD4** | Job @ \( \varphi_r \) **не invalidate** при последующих \( \mathsf{FileChange} \); результат помечен `atRevision = r` |
| **OD5** | **LUT:** `freeze → Build → TestDiscovery → TestRun` (debounced) |
| **OD6** | **Refactor / fix:** semantic work на \( \varphi \); `apply(\Delta)` порождает \( \mathsf{FileChange} \) / \( \mathsf{ProjectFileCrud} \) — invalidation **после** apply, не до |

```text
Live session (DesignTime)          Snapshot job lane
─────────────────────────          ──────────────────
FileChange → per-file stale        freeze(subtree) → φ_r
CompilerServices(π) ∈ M            job(k, π, φ_r, θ):
  (continues uninterrupted)          Build      → artifacts
                                   TestRun    → report
                                   CodeTransform → Δ (preview or apply)
```

**Следствие:** blanket «CompileTime invalidates DesignTime» **отвергнут** для interactive path.

### 7.2 Policy — атрибут или ребро, не отдельный мир

Все «если / когда / как» из §5–§7 — **читаются** через \( \rho_{\mathsf{eff}} \):

| Поведение | Где живёт policy |
|-----------|------------------|
| Lazy vs eager warm | \( \rho_0 \) или \( E_{\mathsf{gov}} \)(session → \( \mathsf{CompilerServices} \)) |
| `BuildWithDirty` | \( \psi(c_{\mathsf{Build}}) \) |
| Feed после build | \( \lambda \) на \( E_{\mathsf{feed}} \) или `feedAfterBuild` в \( \kappa \) |
| Coalesce snapshot jobs | \( \psi(\pi) \) или session default |
| Adaptive rules | уже в \( \kappa \); это **policy внутри** capability-атрибута |

| ID | Формулировка |
|----|----------------|
| **P1** | Оркестратор принимает решения только через \( \rho_{\mathsf{eff}}(v,p) \) и аксиомы §3–§7, не через скрытые globals |
| **P2** | Смена policy = изменение \( \psi \), \( \lambda \), или \( \rho_0 \) — **не** смена сортов графа |
| **P3** | \( E_{\mathsf{gov}} \) ацикличен по project-ownership (как \( E_{\mathsf{req}} \) на capability DAG) |
| **P4** | User/settings overlay мержится в \( \rho_0 \) / \( \psi \) при `Open`; не отдельный runtime channel |

```text
ρ_catalog  →  ρ_0 (session)  →  ψ(π)  →  κ_π(k)  →  λ(edge)
   defaults      anchor           project    capability   governs/feed
```

### 7.3 Build substrate — MSBuild не SSOT

**SSOT сессии:** \( G \), \( \omega \), \( \kappa \) — наш граф решения. MSBuild / `dotnet` — **один из backend'ов** capability \( \mathsf{Build} \), не модель мира.

\[
\alpha.\mathsf{buildDriver} \in \{ \mathsf{MsBuildInterop},\ \mathsf{DotNetCli},\ \mathsf{GuidersBuild},\ \mathsf{Npm},\ \ldots \}
\]

| Слой | Роль MSBuild (legacy) | Целевое состояние |
|------|----------------------|-------------------|
| **DesignTime** | `MsBuildWorkspace`, in-proc evaluation | **Ports.DotNet** → options для Roslyn/FCS из \( G \) + sdk assets; MSBuild **не** в hot path |
| **CompileTime** | `dotnet build`, MSBuild.exe | **Pluggable** driver; свой DAG над \( \varphi \) (`GuidersBuild`) когда готов |
| **Project file** | `.csproj` / `.fsproj` как источник правды | **Порт импорта** в \( \kappa_\pi \); позже — native project descriptor в графе |

**Аксиомы:**

| ID | Формулировка |
|----|----------------|
| **BS1** | Оркестратор **не** держит MSBuild evaluation graph как часть \( G \) |
| **BS2** | Смена `buildDriver` — атрибут capability, не смена сортов графа |
| **BS3** | \( \mathsf{GuidersBuild} \) строит по \( \varphi \) и внутреннему compile DAG (targets как данные); subprocess MSBuild — fallback port |
| **BS4** | Refactor / analyzer / emit **не обязаны** знать про MSBuild, только про semantic model @ \( \varphi \) |

**Прагматика миграции:** tactical `dotnet build` остаётся в Phase 2–4; стратегически MSBuild уходит за границу **port** (как сейчас FCS уже на Sdk loader, не Ionide probe). «Забыть как страшный сон» = **архитектурно** выкинуть из SSOT, не обязательно удалить бинарник завтра.

### 7.4 Incremental build & compile

Свой compile DAG + кэш артефактов — **естественное** продолжение \( \varphi \) и scope-invalidation, не отдельная подсистема.

#### Compile graph (наш DAG, не MSBuild)

\[
\Gamma_\pi = (U_\pi,\ E_\pi,\ \mathsf{emit})
\]

\( U_\pi \) — **compilation units** (файл, модуль, project node — гранулярность policy); \( E_\pi \subseteq U_\pi \times U_\pi \) — зависимости; \( \mathsf{emit} : U \to \mathsf{Artifact} \).

#### Artifact cache

\[
\mathcal{H} : \mathsf{hash}(\mathsf{inputs}(u)) \rightharpoonup (\mathsf{IL},\ \mathsf{pdb},\ \ldots)
\]

Кэш привязан к **замороженным** входам @ \( \varphi_r \), не к live dirty (кроме `BuildWithDirty`).

#### Incremental build (unit-level)

При \( \varphi_{r'} \) после \( \varphi_r \):

\[
\Delta_{\mathsf{files}} = \{ f : \mathrm{contents}_{r'}(f) \neq \mathrm{contents}_r(f) \}
\]

\[
U' = \{ u \in U_\pi : u \cap \Delta_{\mathsf{files}} \neq \emptyset \} \cup \mathrm{descendants}_{E_\pi}(U')
\]

**Job:** \( \mathsf{Build}(\pi)@\varphi_{r'} \) пересобирает только \( U' \); остальное — **reuse** из \( \mathcal{H} \).

| ID | Формулировка |
|----|----------------|
| **IB1** | Incremental build — **diff двух snapshot'ов** + transitive closure на \( \Gamma_\pi \), не полный rebuild по умолчанию |
| **IB2** | LUT / background lane **коалесцирует** \( r' \) и запускает один incremental job на последний diff |
| **IB3** | `buildDriver=MsBuildInterop` может делегировать incrementality MSBuild; `GuidersBuild` держит \( \Gamma_\pi \), \( \mathcal{H} \) как SSOT |

#### Incremental **compilation** (finer than project)

DesignTime уже держит **incremental semantic model** (Roslyn/FCS) в \( \mathsf{CompilerServices} \). Emit — отдельный слой:

\[
\mathsf{compile}(u,\ \mathsf{symbols@}\varphi) \to \mathsf{IL}_u
\]

- **Не обязан** перекомпилировать весь \( \pi \), если изменился один \( u \) и public API \( u \) не сломан (reference assembly / metadata-only deps).
- **IL как interchange:** ECMA-335 + `System.Reflection.Metadata` / Cecil — открытый контракт; линковка, diff, hot-reload patch — над **артефактами**, не над MSBuild XML.

| ID | Формулировка |
|----|----------------|
| **IC1** | \( \mathsf{CompilerServices} \) (DesignTime) и \( \mathsf{emit} \) (CompileTime) **разделяют** dependency graph, но не обязаны один handle |
| **IC2** | Incremental emit затрагивает минимальный \( U' \); invalidation public surface → расширить \( U' \) по \( E_\pi \) |
| **IC3** | PDB / sequence points — часть артефакта в \( \mathcal{H} \); debug feed опционален (policy) |

```text
FileChange (live)     →  stale semantic cache (per-file)
freeze r → r'         →  Δ_files
Build@φ_r'            →  U' = closure(Δ) on Γ_π
                      →  compile only U' → IL → link/reuse H
LUT                   →  TestRun on new IL subset
```

**Следствие:** MSBuild incremental — **одна реализация** `IB*` через port; целевое состояние — наш \( \Gamma_\pi + \mathcal{H} \), IL-native pipeline.

---

## 8. Adaptive — разрешение топологии

Пусть \( \alpha.\mathsf{rules} = \{ r_1,\ldots,r_n \} \) упорядоченный список правил вида

\[
r_i : \mathsf{pred}_i(\mathcal{S}, \pi, k) \Rightarrow t_i \in \top \setminus \{\mathsf{Adaptive}\}
\]

| ID | Формулировка |
|----|----------------|
| **AD1** | \( \mu(c) = t_i \) для **минимального** \( i \) с истинным \( \mathsf{pred}_i \) |
| **AD2** | Если ни одно правило не истинно — materialize **отклоняется** (ошибка политики) |
| **AD3** | Примеры предикатов (не исчерпывающе): \( |\mathrm{files}(\pi)| < N \), \( c \in M \) (warm), full-solution scan flag |

---

## 9. Решения и открытые вопросы

### 9.1 Invalidates — **решено**

**Вариант D (scope-indexed):** см. §5.2. Runtime invalidation по \(\mathsf{scope}(\delta)\); явные \( E_{\mathsf{inv}} \) в графе — структурное дополнение.

Отвергнуто для runtime:
- **A** — только явные рёбра (слишком грубо без scope);
- **B** — только phase-driven (смешивает lifecycle и file edit);
- **C** — phase + каталог без file/project CRUD (неполно).

### 9.2 On-demand Build — **решено**

Build / Test — subprocess over \( \varphi \), не автомат phase advance (§7, L3–L5). Phase \( \mathsf{CompileTime} \) в v0 — **политический флаг** сессии («разрешены compile-job'ы»), не триггер materialize.

### 9.5 Policy as graph data — **направление зафиксировано**

\( \rho \) раскладывается на \( \rho_0 \), \( \psi \), \( \lambda \), \( E_{\mathsf{gov}} \) (§2.6, §7.2). Код v0: `SessionPolicy` — временный face на \( \rho_0 \); цель — merge в граф.

### 9.6 Build substrate — **направление зафиксировано**

MSBuild / `dotnet` — port (`buildDriver`), не SSOT (§7.3). Свой build DAG + \( \mathcal{H} \) + incremental \( U' \) (§7.4).

### 9.7 Incremental build/compile — **направление зафиксировано**

§7.4: diff snapshot'ов → minimal \( U' \) on compile DAG; IL as open interchange (ECMA-335).

### 9.8 Глобальные vs локальные рёбра

Сейчас \( E_* \subseteq V \times V \) **глобально** на solution. Альтернатива: рёбра только внутри \( \mathrm{subgraph}(\pi) \). v0: **глобальные** (кросс-project requires возможен, напр. shared analyzer).

### 9.9 Идентичность project

v0: \( \mathrm{id}(\pi) = \mathsf{fullpath}(\pi.\mathsf{path}) \). Rename project на диске = новый узел. Позже: stable UUID в метаданных.

---

## 10. Соответствие коду (v0)

| Математика | F# (`Modeling.Ide.Session`) |
|------------|-----------------------------|
| \( \mathbb{P} \) | `ProjectNode` list |
| \( \kappa_\pi \) | `ProjectNode.Capabilities` |
| \( V \) | `GraphNodeId` = `ProjectNode` \| `CapabilityNode` |
| \( E_{\mathsf{req}} \) | `SessionEdge` with `Kind = Requires` |
| \( \omega \) | `SolutionGraph.FileOwnership` |
| \( \mathcal{S} \) | `SolutionSession` |
| \( \rho_0 \) | `SessionPolicy` (v0 face; → merge в graph) |
| \( \psi \), \( E_{\mathsf{gov}} \) | **ещё нет** |
| WF1–WF6 | `GraphValidation.validate` |
| \( M, \mu \) | **ещё нет** → `Execution.Ide.Session` Phase 2 |
| \( \varphi_r \), `freeze` | **ещё нет** → Phase 2 job substrate |

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
6. Код: `InvalidationScope`, `FrozenSnapshot`, `CompileGraph`, `ArtifactCache`, `SnapshotJob`.  
7. Порт slnx: \( G \) из парсера + \( \kappa \) из `CapabilityCatalog`.  
8. Доказуемые свойства (опционально): «\(\mathsf{FileChange}\) не evict \( M \)»; «build @ \( \varphi_r \) не invalidate при edit @ \( r' > r \)».

### Будущие ветки (не v0, не блокируют Dash Studio)

| Ветка | Суть | Сейчас |
|-------|------|--------|
| **GuidersBuild** | \( \Gamma_\pi + \mathcal{H} \), incremental; emit via **Roslyn/FCS** drivers | Phase 2–4 orchestrator |
| **GDL → IL** | declare-time GDL как frontend напрямую в ECMA-335; язык-агностичный interchange | **отдельная идея / fork**; capture: [ANUI GDL/IL rethink](../../../../../../../agent-notes/knowledge/work/projects/door-to-singularity/ai-native-ui/note-anui-gdl-il-rethink-v0.md) |
| **Свой frontend** | полный compiler с нуля | только если GDL/Planet станет продуктом, не платформой |

**v0 emit policy:** reuse Roslyn/FCS; не писать свой `csc`/`fsc` — иначе не ship'им платформу.

**Теория GDL → IL:** IL как lingua franca — любой язык **в** экосистему через emit, любой **из** через metadata/reflection; F#/C# становятся optional surface, не SSOT. Заманчиво, но **после** session graph + Dash Studio vertical slice.

---

*Обсуждение: правь аксиомы прямо в PR/issue; F# подстраивается под формулы, не наоборот.*

**Forge render:** GitHub не рендерит LaTeX в preview — читаемая математика в human_view: [FORGE-ADR-0071](https://github.com/AI-Guiders/agent-forge/blob/main/design/FORGE-ADR-0071-math-markdown-block-renderer.md) (`markdown-math` plugin, KaTeX, `docs/math/**` profile).
