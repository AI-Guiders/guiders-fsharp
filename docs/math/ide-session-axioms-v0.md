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

### 2.8b Frozen Tree Composition (FTC)

Локальный \( \mathsf{freeze}(\pi) \) — **лист** композиции. Кросс-project job'ы (build с refs, solution-wide analyzer) собирают **дерево** из нескольких замороженных поддеревьев — **без** глобальных capability-рёбер (§2.3, §9.8).

**Замыкание по project-графу:**

\[
\mathrm{closure}_{\mathsf{proj}}(\Pi) = \Pi \cup \{ \pi' : \exists \pi \in \Pi.\ (\pi, \pi') \in E_{\mathsf{proj}}^* \}
\]

(транзитивные **зависимости** — edges «consumer → dependency»; направление порта задаёт \( E_{\mathsf{proj}} \).)

**Режимы композиции** \( \mathsf{FreezeMode} \):

| Mode | Множество \( T \subseteq \mathbb{P} \) | Типичный job |
|------|----------------------------------|--------------|
| \( \mathsf{Local}(\pi) \) | \( \{\pi\} \) | CodeTransform, per-project preview |
| \( \mathsf{ProjClosure}(\pi_0) \) | \( \mathrm{closure}_{\mathsf{proj}}(\{\pi_0\}) \) | Build, TestRun, incremental \( \Gamma_\pi \) |
| \( \mathsf{Solution} \) | \( \mathbb{P} \) (или policy-subset) | `Scope=Solution` shared analyzer |
| \( \mathsf{Custom}(\Pi) \) | явное \( \Pi \) | CI slice, selective scan |

**Составной snapshot:**

\[
\varphi_r(T) = \bigoplus_{\pi \in T} \varphi_r(\pi)
\]

дизъюнктное объединение: \( \omega_r \), \( \mathrm{contents}_r \), \( \kappa_{\pi,r} \) на узлах \( T \); **иммутабельность** наследуется от листьев.

\[
\mathsf{freeze\_tree}(m, \Pi_0) \to \varphi_r(T)
\]

где \( T \) выводится из mode \( m \) и корней \( \Pi_0 \).

**Ревизия:** \( r \) — монотонный счётчик сессии **или** вектор \( (r_\pi)_{\pi \in T} \); job помечен `atRevision = r`. При изменении одного \( \pi \) — перекомпоновать с новым \( \varphi_{r'}(\pi) \), остальные листья **reuse** (IB2).

**Shared analyzer** (`Scope=Solution`): capability на **session/solution** узле (не ребро между \( \pi_1 \) и \( \pi_2 \)); materialize → \( \mathsf{job}(\mathsf{StaticAnalysis}, \_, \mathsf{freeze\_tree}(\mathsf{Solution}, \emptyset), \theta) \). Оркестратор **даёт ему всё нужное** — policy решает \( T \) и объём; потолка в модели нет.

**Workspace projection (facade):** потребитель job'а (analyzer, build driver, test host) **не обязан** знать про \( \mathbb{P} \), FTC, federation. Порт материализует **видимую копию**:

\[
\pi_{\mathsf{ws}} : \varphi_r(T) \to \mathsf{WorkspaceView}
\]

— layout путей, sln/csproj, contents @ \( r \), как если бы у него была **своя** изолированная копия (temp dir, overlay FS, in-memory VFS — деталь порта). Анализатору не важно, что снимок **собран** оркестратором из чужих \( \pi \); контракт порта — «нормальный workspace @ revision \( r \)».

**Принцип:** FTC — **эфемерный вход job lane**, не статическое \( E_{\mathsf{feed}} \) / \( E_{\mathsf{req}} \) между чужими \( \mathrm{subgraph}(\pi) \). Federation — **внутренняя** алгебра сессии; наружу — только projection.

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

### 2.10 Refactor как морфизм графа

\[
\mathsf{Refactor} : \mathcal{S} \times \Theta \to \mathcal{S}'
\qquad\text{где}\qquad
\mathcal{S} = (G,\ \sigma,\ \rho_0),\quad
\mathcal{S}' = (G',\ \sigma,\ \rho_0)
\]

**План** — чистая функция на frozen snapshot (preview = apply отсутствует):

\[
\mathsf{plan}(\theta,\ \varphi_r) \to \Delta
\qquad
\Delta = (\Delta_{\mathsf{fs}},\ \Delta_G)
\]

| Компонент | Носитель | Примеры |
|-----------|----------|---------|
| \( \Delta_{\mathsf{fs}} \) | текст + symbols @ paths | rename, extract method, fix, codegen |
| \( \Delta_G \) | патч **структуры** \( G \) | \( \omega \) (move file), \( E_{\mathsf{proj}} \), \( \mathbb{P} \) (редко) |

**Apply** (живёт сессия):

\[
G' = G \oplus \Delta_G
\qquad
\mathrm{contents}' = \mathrm{contents} \oplus \Delta_{\mathsf{fs}}
\]

\[
\mathsf{apply}(\mathcal{S},\ \Delta) = (G',\ \sigma,\ \rho_0)
\quad\text{+}\quad
\mathrm{invalidate}(\mathsf{scope}(\Delta))
\]

**Сокращённая запись** (то, что ты имел в виду):

\[
\mathsf{Refactor}(G,\ \theta) = G'
\]

**где**

\[
G' = G \oplus \pi_G(\mathsf{plan}(\theta,\ \mathsf{freeze\_tree}(m,\Pi_0)))
\]

\[
\pi_G(\Delta) = \Delta_G
\qquad
\text{(проекция на } \mathbb{P},\ \omega,\ E_{\mathsf{proj}},\ \kappa,\ \ldots\text{)}
\]

\[
\mathsf{validate}(G') = \mathsf{ok}
\qquad
\mathsf{scope}(\Delta) \in \{\mathsf{FileChange},\ \mathsf{ProjectFileCrud},\ \ldots\}
\]

\[
\theta \text{ содержит anchor (ADR-0063)} \Rightarrow \mathsf{resolve}(\theta.\mathsf{anchor},\ \varphi) \in N_{\mathsf{sem}} \cup N_{\mathsf{syn}}
\]

**Частые случаи:**

| Transform | \( \Delta_{\mathsf{fs}} \) | \( \Delta_G \) | \( G' \) |
|-----------|-------------------|-------------|--------|
| rename local | spans in one \( f \) | \( \emptyset \) | \( G' = G \) |
| rename symbol (solution) | multi-file | \( \emptyset \) | \( G' = G \) |
| move type → new file | edits + new contents | \( \omega \) update | \( G' \neq G \) |
| add `ProjectReference` | csproj text | \( E_{\mathsf{proj}} \) | \( G' \neq G \) |

**Preview / dryRun:** \( \mathsf{plan}(\theta, \varphi_r) \) без \( \mathsf{apply} \); \( G \) не меняется.

#### Корректность (тройки Хоара)

\[
\mathsf{Sat}(P,\ G,\ Q)
\qquad\text{«тройка } (P,G,Q) \text{ выполнена»}
\]

| Сорт | Смысл (v0) | Примеры |
|------|------------|---------|
| \( P \) | **precondition** — контекст, в котором transform допустим | \( \sigma = \mathsf{DesignTime} \); anchor resolved; \( \mathsf{validate}(G) \) |
| \( G \) | **state** — граф + contents @ revision (наш SSOT) | \( (\mathbb{P}, E_*, \omega, \kappa, \mathrm{contents}) \) |
| \( Q \) | **postcondition** — что обязано остаться истинным | WF1–WF8; \( \mathsf{typecheck}(G) \); \( \mathsf{obs}(G) \) (поведение) |

**Рефактор корректен**, если сохраняет тройку при замене \( G \) на \( G' \):

\[
\mathsf{Sat}(P,\ G,\ Q)
\;\Rightarrow\;
\mathsf{Refactor}(G,\ \theta) = G'
\;\Rightarrow\;
\mathsf{Sat}(P,\ G',\ Q)
\]

Эквивалентно (правило Хоара для морфизма):

\[
\{ P \}\; G \xrightarrow{\;\mathsf{Refactor}(\theta)\;} G' \;\{ Q \}
\qquad\text{корректно}\qquad
\{ P \}\; G \;\{ Q \} \Rightarrow \{ P \}\; G' \;\{ Q \}
\]

**Слои \( Q \)** (можно conjoin):

\[
Q = Q_{\mathsf{wf}} \land Q_{\mathsf{types}} \land Q_{\mathsf{obs}}
\]

| \( Q \) | Формулировка |
|--------|----------------|
| \( Q_{\mathsf{wf}} \) | \( \mathsf{validate}(G') \) — структура графа |
| \( Q_{\mathsf{types}} \) | семантика well-typed @ contents' |
| \( Q_{\mathsf{obs}} \) | \( \mathsf{obs}(G') = \mathsf{obs}(G) \) — **behavior preservation** (rename, extract, …) |
| \( Q_{\mathsf{tests}} \) | опционально: test report green (дорого; LUT / CI) |

#### Классы \( \Theta \) (не путать)

\[
\Theta = \Theta_{\mathsf{ref}} \uplus \Theta_{\mathsf{fix}} \uplus \Theta_{\mathsf{style}} \uplus \Theta_{\mathsf{cfg}}
\]

| Класс | Зачем | Hoare \( Q \) (default) | \( Q_{\mathsf{obs}} \) |
|-------|-------|--------------------------|-------------------------|
| \( \Theta_{\mathsf{ref}} \) **Refactor** | структура / читаемость | \( Q_{\mathsf{wf}} \land Q_{\mathsf{types}} \land Q_{\mathsf{obs}} \) | **сохраняем** |
| \( \Theta_{\mathsf{fix}} \) **Fix** | закрыть diagnostic / bug | \( Q_{\mathsf{wf}} \land Q_{\mathsf{types}} \land Q_{\mathsf{diag}} \) | **может меняться** — это не refactor |
| \( \Theta_{\mathsf{style}} \) **CodeStyle** | формат / naming / style rules | см. §2.11 | обычно сохраняем; не обязано |
| \( \Theta_{\mathsf{cfg}} \) **Config** | `.editorconfig`, analyzer props, rule sets | \( Q_{\mathsf{wf}} \); меняется \( \psi \), не обязательно sources | n/a |

**Fix** — отдельная операция: \( \theta.\mathsf{diagnosticId} \), цель \( Q_{\mathsf{diag}} \) («этот diagnostic исчез»), **не** ослабленный refactor.

**Config** (EditorConfig и пр.) — операции над **policy-носителями** графа (\( \psi \), файлы правил в \( \omega \)), не над семантикой программы; compile **не гарантируют**.

| ID | Формулировка |
|----|----------------|
| **RF1** | \( \mathsf{plan}(\theta, \varphi) \) **детерминирован** при фиксированном \( \varphi \) |
| **RF2** | \( \mathsf{apply}(\Delta) \) только после явного commit; live \( M \) не трогаем до apply (OD3) |
| **RF3** | \( \mathsf{validate}(G') \) обязателен; WF1–WF8 после \( \oplus \) |
| **RF4** | \( \Delta_G = \emptyset \) — норма; promotion \( \mathsf{scope} \) по \( \Delta \), не по имени transform |
| **RF5** | Semantic work на \( \varphi \), не на dirty live (v0 default); sniper scope из \( \theta.\mathsf{anchor} \) (§7.4) |
| **RF6** | **Hoare (Refactor):** для \( \theta \in \Theta_{\mathsf{ref}} \): \( \mathsf{Sat}(P,G,Q) \Rightarrow \mathsf{Sat}(P,G',Q) \); preview **отклоняет** \( \Delta \), если \( Q \) не установим |
| **RF7** | **Hoare (Fix):** \( Q = Q_{\mathsf{wf}} \land Q_{\mathsf{types}} \land Q_{\mathsf{diag}} \); \( Q_{\mathsf{obs}} \) **не** требуется |
| **RF8** | **Hoare (Style):** proven path — \( Q_{\mathsf{types}} \) обязателен в preview; vendor/text без check — не auto-apply |

### 2.11 Code Style & EditorConfig в графе

**Ничего не мешает** держать style в том же \( G \), что и compile/refactor:

| Носитель | Что |
|----------|-----|
| \( \omega(f) \) | `.editorconfig`, `GlobalAnalyzerConfig`, style rule files |
| \( \psi(\pi) \), \( \psi(\mathsf{session}) \) | resolved effective style (merge иерархии каталогов) |
| \( \kappa_\pi(\mathsf{CodeStyle}) \) | capability: format, organize usings, naming fix |
| \( E_{\mathsf{gov}} \) | catalog / session → project style policy |

**Операция** — тот же каркас §2.10:

\[
\mathsf{StyleApply}(G, \theta) = G'
\qquad
\mathsf{plan}_{\mathsf{style}}(\theta, \varphi) \to \Delta
\]

**Три пути** (не путать «Roslyn formatter» с гарантией):

| Path | Механизм | Гарантии |
|------|----------|----------|
| **text** | whitespace / regex / line-based | только \( Q_{\mathsf{style}} \); **может** сломать parse/types |
| **vendor** | Roslyn/VS formatter, code fixes, «Apply» в IDE | **не гарантирует** \( Q_{\mathsf{types}} \) — синтаксический/эвристический; известные прод-фейлы: `file-scoped namespace`, `scoped using` ↔ block `using`, лишние `}` / EOF errors |
| **proven** | наш semantic path + **обязательный** \( Q_{\mathsf{types}} \) check в preview (Hoare) | \( Q_{\mathsf{wf}} \land Q_{\mathsf{types}} \land Q_{\mathsf{style}} \); иначе **reject** \( \Delta \) |

\[
Q_{\mathsf{style}} : \mathsf{rules}(G') \models \mathsf{EffectiveStyle}(G, \psi)
\]

**Vendor path — port, не SSOT:** делегируем Roslyn/VS как `styleDriver`, но **не** доверяем apply без \( \mathsf{typecheck}(G') \) @ \( \varphi \). «Semantic» в их маркетинге ≠ доказуемый semantic path в нашей модели.

**EditorConfig CRUD** (\( \Theta_{\mathsf{cfg}} \)): меняет правила и \( \psi \), не обязан трогать program sources; после apply — optional \( \mathsf{StyleApply} \) (on-demand).

| ID | Формулировка |
|----|----------------|
| **ST1** | Effective style = merge по иерархии путей (как EditorConfig), результат в \( \psi \) или derived view — **не** отдельный конфиг в обход \( G \) |
| **ST2** | Default format-on-save в IDE = \( \mathsf{FileChange} \) + refine; **explicit** format solution = \( \mathsf{job}(\mathsf{CodeStyle}, \ldots) \) |
| **ST3** | **Proven** path — default для commit apply; **vendor** / text — preview-only или с warn, пока \( Q_{\mathsf{types}} \) не прошёл checker |
| **ST4** | \( \mathsf{CodeStyle} \) \( E_{\mathsf{req}} \) → \( \mathsf{CompilerServices} \) на proven path |
| **ST5** | После \( \mathsf{plan}_{\mathsf{style}} \): \( \mathsf{typecheck}(G') \) @ \( \varphi \) **обязателен** перед apply; vendor output без check — **запрещён** для auto-apply (v0) |

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
| **FileChange** | содержимое файла | edit, save, watcher | dirty-bit на пути \( f \) (не обязательно менять \( G \)) | **не evict** \( M \); делегировать **sub-file** invalidation в \( \Sigma_\pi \) (§5.2a) |
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
            └── File ── Changes   (dirty; sub-file refine в Σ — §5.2a)
```

#### 5.2a Sub-file refinement (semantic substrate)

**FileChange** — **верхняя граница** session-scope, не нижняя. Внутри materialized \( \mathsf{CompilerServices}(\pi) \in M \) живёт **семантический субстрат**:

\[
\Sigma_\pi = (N_{\mathsf{syn}},\ N_{\mathsf{sem}},\ E_{\mathsf{dep}},\ \mathcal{D})
\]

| Компонент | Смысл |
|-----------|--------|
| \( N_{\mathsf{syn}} \) | syntax-узлы (деревья по файлам, green/red nodes) |
| \( N_{\mathsf{sem}} \) | semantic handles: symbols, types, binding entries |
| \( E_{\mathsf{dep}} \subseteq (N_{\mathsf{syn}} \cup N_{\mathsf{sem}})^2 \) | «зависит от» (parse → bind → type) |
| \( \mathcal{D} \) | кэш диагностик, keyed по узлам / spans |

**Две оси** события \( \delta \):

\[
\mathsf{scope}(\delta) \in \{\mathsf{FileChange}, \ldots\},\qquad
\mathsf{refine}(\delta) \subseteq N_{\mathsf{syn}} \cup N_{\mathsf{sem}}
\]

Оркестратор сессии знает **только** \( \mathsf{scope} \); \( \mathsf{refine} \) — зона ответственности порта \( \mathsf{CompilerServices} \) (Roslyn/FCS incremental).

**Уровни refinement** (от мелкого к крупному внутри FileChange):

| Level | Событие | \( \mathsf{refine}(\delta) \) |
|-------|---------|---------------------------|
| **TextEdit** | вставка/удаление в \( f \) @ span | затронутые syntax nodes + их \( E_{\mathsf{dep}} \)-потомки |
| **SyntaxStale** | reparsed subtree | \( N' \subseteq N_{\mathsf{syn}}(f) \) |
| **SemanticStale** | смена public surface, unresolved → resolved | \( \mathrm{closure}_{E_{\mathsf{dep}}}(N') \cap N_{\mathsf{sem}} \); может **выйти за файл** внутри \( \pi \) |
| **FileStale** | fallback / policy | весь \( N_{\mathsf{syn}}(f) \cup N_{\mathsf{sem}}(f) \) — грубый потолок, не evict \( M \) |

\[
\mathsf{TextEdit} \prec \mathsf{SyntaxStale} \prec \mathsf{SemanticStale} \prec \mathsf{FileStale}
\]

**Связь с Anchors** ([ADR-0063](https://github.com/AI-Guiders/guiders-platform/blob/main/docs/adr/GUIDERS-ADR-0063-anchors-federation-reincarnation.md)): `TextRange` → span / syntax node; `CodeSymbol` → \( n \in N_{\mathsf{sem}} \); invalidation и navigation на **одних** идентификаторах.

| ID | Формулировка |
|----|----------------|
| **I6** | \( \mathsf{scope}(\delta)=\mathsf{FileChange} \Rightarrow \Delta \cap M = \emptyset \); инвалидация только \( \mathsf{refine}(\delta) \subseteq \Sigma_\pi \) |
| **I7** | Cross-file spread **внутри** \( \pi \) (semantic deps A→B) — refinement, **не** promotion до \( \mathsf{ProjectFileCrud} \) |
| **I8** | Promotion scope вверх только при CRUD (add/remove file, rename path, …), не при semantic closure |

```text
Session orchestrator          CompilerServices port (inside M)
────────────────────          ────────────────────────────────
FileChange(f)  ────────────→  TextEdit → SyntaxStale → SemanticStale
  dirty(f)                      Σ_π refine only; M stays
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

\( U_\pi \) — **compilation units**; гранулярность policy — от **semantic node** (method, type member, …) до файла/модуля; \( E_\pi \subseteq U_\pi \times U_\pi \) — зависимости emit; \( \mathsf{emit} : U \to \mathsf{Artifact} \).

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

#### Sniper-scoped emit (semantic node → minimal \( U' \))

**Цель:** emit только того, что **реально** затронуто semantic delta — как sub-file refinement (§5.2a), но на compile lane. Операционный аналог: CDP **sniper/peel** — правка и пересчёт по **якорю**, не по всему буферу/сборке.

Пусть edit порождает \( \mathsf{refine}(\delta) = \Delta_{\mathsf{sem}} \subseteq N_{\mathsf{sem}} \) (напр. условие в `if` → узел выражения + enclosing symbol).

\[
\Delta_U = \{ u \in U_\pi : u \cap \mathrm{closure}_{E_{\mathsf{dep}}}(\Delta_{\mathsf{sem}}) \neq \emptyset \}
\]

\[
U' = \Delta_U \cup \mathrm{descendants}_{E_\pi}(\Delta_U)
\]

**Пример:** смена условия в `if` без смены типов / public API → \( U' \) может свестись к **одному** method-body unit (или \( U' = \emptyset \), если IL не меняется — comment, unreachable tweak).

| ID | Формулировка |
|----|----------------|
| **SN1** | Default incremental path: \( U' \) из \( \mathsf{refine}(\delta) \), **не** из \( \Delta_{\mathsf{files}} \) целиком, когда live \( \Sigma_\pi \) доступен |
| **SN2** | \( U' = \emptyset \) ⇒ **no emit** (skip compile); не fallback на full \( \pi \) без policy |
| **SN3** | Promotion file-wide / project-wide — только fallback (`FileStale`, `buildDriver=MsBuildInterop`) или broken public surface |
| **SN4** | \( \mathsf{compile}(u, \ldots) \) вызывается **только** для \( u \in U' \); остальное — reuse \( \mathcal{H} \) |
| **SN5** | `CodeSymbol` anchor (ADR-0063) — допустимый **ключ** \( u \) и sniper-target для emit scope |
| **SN6** | **Не** EnC / Hot Reload: runtime patch, debug-only, кривой blast radius — **отвергнуто** как SSOT; sniper emit = детерминированный IL @ \( \varphi_r \) в \( \mathcal{H} \), не «магия дебаггера» |

**Связь осей:** DesignTime \( \mathsf{refine} \) и CompileTime \( U' \) — **один** closure на \( E_{\mathsf{dep}} \), разные порты (semantic refresh vs IL emit).

```text
FileChange (live)     →  refine Δ_sem in Σ_π (syntax/semantic nodes)
                        →  FileStale(f) only as fallback
freeze r → r'         →  Δ_files (coarse) OR Δ_sem (sniper path)
Build@φ_r'            →  U' = closure(Δ_sem) on Γ_π   [SN1–SN4]
                      →  U' = ∅ → skip; else compile U' → IL → reuse H
LUT                   →  TestRun on new IL subset

Sniper example: edit if-condition → Δ_sem = {expr, enclosingMethod}
                  → U' = {methodBody_u} or ∅ — not whole assembly
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

### 9.8 Capability vs project edges — **решено**

**Вариант local + \( E_{\mathsf{proj}} \)** (§2.3):

- \( E_{\mathsf{req}}, E_{\mathsf{inv}}, E_{\mathsf{feed}} \) — **только внутри** \( \mathrm{subgraph}(\pi) \) (WF7).
- Кросс-project — \( E_{\mathsf{proj}} \subseteq \mathbb{P} \times \mathbb{P} \) (WF8): `ProjectReference`, slnx, restore order.
- \( E_{\mathsf{gov}} \) — policy overlay; может быть кросс-\( \pi \), не build dependency.
- Solution-wide analyzer / scan — `Scope=Solution` + **FTC** \( \mathsf{Solution} \) (§2.8b), **не** глобальное capability-ребро между \( \pi_1 \) и \( \pi_2 \).

### 9.10 Frozen Tree Composition — **решено**

Кросс-project работа (build closure, shared analyzer, CI slice) — через \( \mathsf{freeze\_tree} \) и \( \varphi_r(T) = \bigoplus_{\pi \in T} \varphi_r(\pi) \), не через capability-рёбра между \( \mathrm{subgraph}(\pi) \).

Отвергнуто: глобальные \( E_* \subseteq V \times V \) на capability-слое (ломает blast radius §5.2, дублирует \( E_{\mathsf{proj}} \)).

### 9.9 Идентичность project

v0: \( \mathrm{id}(\pi) = \mathsf{fullpath}(\pi.\mathsf{path}) \). Rename project на диске = новый узел. Позже: stable UUID в метаданных.

---

## 10. Соответствие коду (v0)

| Математика | F# (`Modeling.Ide.Session`) |
|------------|-----------------------------|
| \( \mathbb{P} \) | `ProjectNode` list |
| \( \kappa_\pi \) | `ProjectNode.Capabilities` |
| \( V \) | `GraphNodeId` = `ProjectNode` \| `CapabilityNode` |
| \( E_{\mathsf{proj}} \) | **ещё нет** (ports slnx/csproj) |
| \( E_{\mathsf{req}} \) | `SessionEdge` with `Kind = Requires` (v0: validate WF7 pending) |
| \( \omega \) | `SolutionGraph.FileOwnership` |
| \( \mathcal{S} \) | `SolutionSession` |
| \( \rho_0 \) | `SessionPolicy` (v0 face; → merge в graph) |
| \( \psi \), \( E_{\mathsf{gov}} \) | **ещё нет** |
| WF1–WF8 | `GraphValidation.validate` (WF7–WF8 Phase 1b) |
| \( M, \mu \) | **ещё нет** → `Execution.Ide.Session` Phase 2 |
| \( \Sigma_\pi \), `refine` | **ещё нет** → port `CompilerServices` / semantic substrate (Phase 2) |
| \( \varphi_r \), `freeze` / `freeze_tree` | **ещё нет** → `FrozenSnapshot`, `FrozenTreeComposition`, `FreezeMode` (Phase 2) |
| \( \pi_{\mathsf{ws}} \) | **ещё нет** → port `WorkspaceView` / materialize facade (Phase 2) |
| \( \mathsf{Refactor} \), \( \oplus \) | **ещё нет** → `RefactorPlan`, `GraphPatch`, `FileSystemPatch` (Phase 2) |
| RF1–RF8, ST1–ST4 | **ещё нет** → orchestrator + ports + `HoareChecker` |

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
6f. ~~Code Style & EditorConfig in G~~ — §2.11, ST1–ST4.  
7. Код: `InvalidationScope`, `SemanticRefinement`, `SniperEmitScope`, `MaterializedState`, `FrozenSnapshot`, `FrozenTreeComposition`, `FreezeMode`, `CompileGraph`, `ArtifactCache`, `SnapshotJob`, `E_proj`.  
8. Порт slnx: \( G \) + \( E_{\mathsf{proj}} \) из парсера + \( \kappa \) из `CapabilityCatalog`.  
9. `GraphValidation`: WF7 (local capability edges), WF8 (project DAG).  
10. Доказуемые свойства (опционально): «\(\mathsf{FileChange}\) не evict \( M \)»; «build @ \( \varphi_r \) не invalidate при edit @ \( r' > r \)».

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
