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
\mathbb{K} = \{ \mathsf{CompilerServices},\ \mathsf{StaticAnalysis},\ \mathsf{Build},\ \mathsf{TestDiscovery},\ \mathsf{TestRun},\ \mathsf{LspBridge} \}
\]

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

Три бинарных отношения на \( V \times V \):

\[
E_{\mathsf{req}},\ E_{\mathsf{inv}},\ E_{\mathsf{feed}} \subseteq V \times V
\]

Каждое ребро может нести метку \( \lambda(e) \in \mathcal{A}^* \) (словарь строк — v0).

### 2.4 Владение файлами

\[
\omega : \mathrm{paths} \rightharpoonup \mathbb{P}
\]

частичное отображение «файл → владеющий project».

### 2.5 Статический граф решения

\[
G = (\mathbb{P},\ \mathbb{C},\ E_{\mathsf{req}},\ E_{\mathsf{inv}},\ E_{\mathsf{feed}},\ \omega,\ \kappa)
\]

### 2.6 Сессия

\[
\mathcal{S} = (G,\ \sigma,\ \rho)
\]

где \( \sigma \in \Phi \) — фаза сессии, \( \rho \) — политика (lazy/eager, evict-on-close, …).

### 2.7 Materialized state (динамика, v0 sketch)

\[
M \subseteq \mathbb{C},\quad \mu : M \to \top
\]

\( M \) — множество capability, для которых оркестратор уже поднял handle; \( \mu(c) \) — **фактическая** топология после разрешения \( \mathsf{Adaptive} \).

---

## 3. Аксиомы структурной корректности (static WF)

Эти аксиомы проверяет `GraphValidation.validate` (Phase 1).

| ID | Формулировка |
|----|----------------|
| **WF1** | \( \mathrm{id} \) инъективен на \( \mathbb{P} \) |
| **WF2** | \( \forall \pi.\ |\mathrm{dom}(\kappa_\pi)| = |\{k : \kappa_\pi(k)\ \text{defined}\}| \) и каждый \( k \) встречается **не более одного раза** |
| **WF3** | \( \forall e \in E_{\mathsf{req}} \cup E_{\mathsf{inv}} \cup E_{\mathsf{feed}}.\ e = (u,v) \Rightarrow u \in V \land v \in V \) |
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
| **L3** | \( \mathrm{advance}(\cdot, \mathsf{CompileTime}) \) **может** materialize \( \mathsf{Build} \); **не обязан** materialize \( \mathsf{CompilerServices} \) in-process build |
| **L4** | После \( \mathrm{advance}(\cdot, \mathsf{CompileTime}) \) и успешного build, если \( E_{\mathsf{feed}} \) связывает Build → CompilerServices, то CompilerServices \( \notin M \) или помечен stale до refresh DesignTime |

**Интуиция:** DesignTime и CompileTime — разные «режимы»; диагностика не должна незаметно тянуть full build (согласовано с ADR-0062).

---

## 7. Adaptive — разрешение топологии

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

## 8. Решения и открытые вопросы

### 8.1 Invalidates — **решено**

**Вариант D (scope-indexed):** см. §5.2. Runtime invalidation по \(\mathsf{scope}(\delta)\); явные \( E_{\mathsf{inv}} \) в графе — структурное дополнение.

Отвергнуто для runtime:
- **A** — только явные рёбра (слишком грубо без scope);
- **B** — только phase-driven (смешивает lifecycle и file edit);
- **C** — phase + каталог без file/project CRUD (неполно).

### 8.2 Глобальные vs локальные рёбра

Сейчас \( E_* \subseteq V \times V \) **глобально** на solution. Альтернатива: рёбра только внутри \( \mathrm{subgraph}(\pi) \). v0: **глобальные** (кросс-project requires возможен, напр. shared analyzer).

### 8.3 Идентичность project

v0: \( \mathrm{id}(\pi) = \mathsf{fullpath}(\pi.\mathsf{path}) \). Rename project на диске = новый узел. Позже: stable UUID в метаданных.

---

## 9. Соответствие коду (v0)

| Математика | F# (`Modeling.Ide.Session`) |
|------------|-----------------------------|
| \( \mathbb{P} \) | `ProjectNode` list |
| \( \kappa_\pi \) | `ProjectNode.Capabilities` |
| \( V \) | `GraphNodeId` = `ProjectNode` \| `CapabilityNode` |
| \( E_{\mathsf{req}} \) | `SessionEdge` with `Kind = Requires` |
| \( \omega \) | `SolutionGraph.FileOwnership` |
| \( \mathcal{S} \) | `SolutionSession` |
| WF1–WF6 | `GraphValidation.validate` |
| \( M, \mu \) | **ещё нет** → `Execution.Ide.Session` Phase 2 |

---

## 10. Минимальный пример (для sanity)

Один F# project \( \pi \), capabilities \( \mathsf{CompilerServices} \), \( \mathsf{Build} \):

\[
E_{\mathsf{req}} = \{ (\mathsf{Build}, \mathsf{CompilerServices}) \}
\]

(WF4: DAG OK.)

Переход \( \mathsf{DesignTime} \to \mathsf{CompileTime} \): materialize Build → subprocess → при успехе feed/invalidate CompilerServices → refresh DesignTime context.

---

## 11. Эволюция документа

1. ~~Зафиксировать §8.1 (invalidates)~~ — **D scope-indexed** (§5.2).  
2. Добавить в код: `InvalidationScope`, `MaterializedState` \( M, \mu \), `invalidate(δ)`.  
3. Порт slnx: \( G \) из парсера + \( \kappa \) из `CapabilityCatalog`.  
4. Доказуемые свойства (опционально): «\(\mathsf{FileChange}\) не evict \( M \)»; «\(\mathrm{invalidate}(\delta)\) монотонна по \(\prec\) scope».

---

*Обсуждение: правь аксиомы прямо в PR/issue; F# подстраивается под формулы, не наоборот.*

**Forge render:** GitHub не рендерит LaTeX в preview — читаемая математика в human_view: [FORGE-ADR-0071](https://github.com/AI-Guiders/agent-forge/blob/main/design/FORGE-ADR-0071-math-markdown-block-renderer.md) (`markdown-math` plugin, KaTeX, `docs/math/**` profile).
