# Frozen snapshot, FTC, workspace projection

| | |
|---|---|
| **Package** | [IDE Session axioms](README.md) |
| **Hub** | [ide-session-axioms-v0.md](../ide-session-axioms-v0.md) |

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
