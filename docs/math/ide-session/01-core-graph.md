# Core graph, WF, capability attributes

| | |
|---|---|
| **Package** | [IDE Session axioms](README.md) |
| **Hub** | [ide-session-axioms-v0.md](../ide-session-axioms-v0.md) |

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
\mathbb{K} = \{ \mathsf{CompilerServices},\ \mathsf{StaticAnalysis},\ \mathsf{Build},\ \mathsf{TestDiscovery},\ \mathsf{TestRun},\ \mathsf{CodeTransform},\ \mathsf{CodeStyle},\ \mathsf{LspBridge} \}
\]

\( \mathsf{CodeTransform} \) — рефакторинги, fixes, codegen (классы \( \Theta \) — §2.10). \( \mathsf{CodeStyle} \) — EditorConfig / naming / formatting как **операции над тем же** \( G \) (§2.11). Оба — on-demand §7.

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

### 2.3 Рёбра (два уровня)

**Уровень project** — связи между \( \pi \):

\[
E_{\mathsf{proj}} \subseteq \mathbb{P} \times \mathbb{P}
\]

`ProjectReference`, slnx membership, restore/build order. Кросс-project **только здесь**, не через capability-рёбра.

**Уровень capability** — внутри \( \mathrm{subgraph}(\pi) \):

\[
\mathrm{subgraph}(\pi) = \{\pi\} \cup \{ c \in \mathbb{C} : c \text{ принадлежит } \pi \}
\]

\[
E_{\mathsf{req}},\ E_{\mathsf{inv}},\ E_{\mathsf{feed}} \subseteq \mathrm{subgraph}(\pi) \times \mathrm{subgraph}(\pi)
\]

для **каждого** \( \pi \) (локальные рёбра; **запрещены** \( u \in \mathrm{subgraph}(\pi_1), v \in \mathrm{subgraph}(\pi_2), \pi_1 \neq \pi_2 \)).

**Policy overlay** (может быть кросс-\( \pi \)):

\[
E_{\mathsf{gov}} \subseteq V \times V
\]

\( E_{\mathsf{gov}} \) — session/catalog → project/capability; не substitute для \( E_{\mathsf{proj}} \) или \( E_{\mathsf{feed}} \).

Solution-wide capability (`Scope=Solution`, shared analyzer) — **атрибут** на узле + policy, **не** глобальное \( E_{\mathsf{feed}} \) между чужими \( \pi \).

Каждое ребро несёт метку \( \lambda(e) \in \mathcal{A}^* \) (v0).

### 2.4 Владение файлами

\[
\omega : \mathrm{paths} \rightharpoonup \mathbb{P}
\]

частичное отображение «файл → владеющий project».

### 2.5 Статический граф решения

\[
G = (\mathbb{P},\ \mathbb{C},\ E_{\mathsf{proj}},\ E_{\mathsf{req}},\ E_{\mathsf{inv}},\ E_{\mathsf{feed}},\ E_{\mathsf{gov}},\ \omega,\ \kappa,\ \psi)
\]

где \( \psi : V \rightharpoonup \mathcal{A}^* \) — **policy-атрибуты на узлах** (в т.ч. на \( \pi \) и \( c \)); дублируют/уточняют поля в \( \kappa \).

### 2.6 Сессия

\[
\mathcal{S} = (G,\ \sigma,\ \rho_0)
\]

где \( \sigma \in \Phi \) — фаза сессии; \( \rho_0 \subseteq \mathcal{A}^* \) — **defaults сессии** (anchor overlay), не отдельная «магическая» политика. Полный state с журналом: §2.12 \( (G, \sigma, \rho_0, \Lambda) \).

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

**Замечание:** WF4 — \( E_{\mathsf{req}} \) ацикличен **внутри каждого** \( \mathrm{subgraph}(\pi) \) (эквивалентно WF7 + union). WF8 — project DAG отдельно.

---

## 3. Аксиомы структурной корректности (static WF)

Эти аксиомы проверяет `GraphValidation.validate` (Phase 1).

| ID | Формулировка |
|----|----------------|
| **WF1** | \( \mathrm{id} \) инъективен на \( \mathbb{P} \) |
| **WF2** | \( \forall \pi.\ |\mathrm{dom}(\kappa_\pi)| = |\{k : \kappa_\pi(k)\ \text{defined}\}| \) и каждый \( k \) встречается **не более одного раза** |
| **WF3** | \( \forall e \in E_{\mathsf{req}} \cup E_{\mathsf{inv}} \cup E_{\mathsf{feed}} \cup E_{\mathsf{gov}}.\ e=(u,v) \Rightarrow u,v \in V \); \( \forall e \in E_{\mathsf{proj}}.\ e=(\pi_1,\pi_2) \Rightarrow \pi_1,\pi_2 \in \mathbb{P} \) |
| **WF4** | Для каждого \( \pi \): граф \( (\mathrm{subgraph}(\pi), E_{\mathsf{req}}|_{\pi}) \) — **ацикличен** (local DAG на \( \mathsf{requires} \)) |
| **WF5** | \( \forall f \in \mathrm{dom}(\omega).\ \omega(f) \in \mathbb{P} \) |
| **WF6** | Если \( \alpha = \kappa_\pi(k) \) и \( \alpha.\mathsf{topology} = \mathsf{Adaptive} \), то \( \alpha.\mathsf{rules} \neq \emptyset \) |
| **WF7** | \( \forall e \in E_{\mathsf{req}} \cup E_{\mathsf{inv}} \cup E_{\mathsf{feed}}.\ \exists \pi.\ u,v \in \mathrm{subgraph}(\pi) \) для \( e=(u,v) \) — capability-рёбра **локальны** |
| **WF8** | \( E_{\mathsf{proj}} \subseteq \mathbb{P} \times \mathbb{P} \); граф \( (\mathbb{P}, E_{\mathsf{proj}}) \) ацикличен (v0) |

**Замечание:** WF4 — \( E_{\mathsf{req}} \) ацикличен **внутри каждого** \( \mathrm{subgraph}(\pi) \) (эквивалентно WF7 + union). WF8 — project DAG отдельно.

---

## 4. Аксиомы атрибутов capability

Пусть \( \alpha.\mathsf{phase} \in \Phi \), \( \alpha.\mathsf{topology} \in \top \).

| ID | Формулировка |
|----|----------------|
| **A1** | \( \kappa_\pi(\mathsf{CompilerServices}) \) определено для каждого \( \pi \) с \( \tau \in \{\mathsf{DotNet},\mathsf{Node}\} \) **по умолчанию каталога** (не аксиома жёсткости — convention каталога) |
| **A2** | Для \( \mathsf{CompilerServices} \): \( \alpha.\mathsf{phase} = \mathsf{DesignTime} \) и \( \alpha.\mathsf{topology} \in \{\mathsf{InProcess},\ \mathsf{OutOfProcess},\ \mathsf{Adaptive}\} \) |
| **A3** | Для \( \mathsf{Build} \): \( \alpha.\mathsf{phase} = \mathsf{CompileTime} \), \( \alpha.\mathsf{topology} = \mathsf{SubprocessTool} \) |
| **A4** | \( \mathsf{SubprocessTool} \) допустима только при \( \alpha.\mathsf{phase} \in \{\mathsf{CompileTime},\ \mathsf{TestTime}\} \) |

