# Δ-stream, Git pin, Code History Timeline

| | |
|---|---|
| **Package** | [IDE Session axioms](README.md) |
| **Hub** | [ide-session-axioms-v0.md](../ide-session-axioms-v0.md) |

### 2.12 Revision ledger (Δ-stream) & Git subgraph

**Agent memory** — не чат, а **журнал сессии** + replay. Habitat (CDP buffer, sniper/peel) — **источник событий**, не параллельный SSOT.

\[
\mathcal{S} = (G,\ \sigma,\ \rho_0,\ \Lambda)
\qquad
\Lambda = [\varepsilon_1, \varepsilon_2, \ldots, \varepsilon_n]
\]

**Запись журнала** \( \varepsilon_i \) (append-only):

\[
\varepsilon_i = (r_i,\ \mathsf{scope}_i,\ \theta_i,\ \Delta_i,\ \mathsf{anchor}_i,\ \gamma_i)
\]

| Поле | Смысл |
|------|--------|
| \( r_i \) | ревизия сессии (монотонна) |
| \( \mathsf{scope}_i \) | FileChange … SolutionProjectCrud (§5.2) |
| \( \theta_i \) | класс \( \Theta \) + spec (ref/fix/style/cfg/…) |
| \( \Delta_i \) | \( (\Delta_{\mathsf{fs}}, \Delta_G) \) или summary hash |
| \( \mathsf{anchor}_i \) | optional AnchorIntent (ADR-0063) |
| \( \gamma_i \) | **Git pin** @ apply boundary (§2.12b) |

**Replay:**

\[
G_0 \xrightarrow{\varepsilon_1} G_1 \xrightarrow{\varepsilon_2} \cdots \xrightarrow{\varepsilon_n} G_n
\qquad
\mathsf{replay}(G_0,\ \Lambda[1..k]) = G_k
\]

\[
\mathsf{apply}(G_{i-1},\ \Delta_i) = G_i
\]

Live edit (CDP) без commit в журнал — **ephemeral** `FileChange` + \( \Sigma_\pi \) refine; **commit** (user/agent apply, save batch) — **append** \( \varepsilon_i \).

#### 2.12b Git subgraph (overlay, не SSOT)

Git — **port**; в сессии — **overlay** на границах apply:

\[
\Gamma_{\mathsf{git}} = (\mathsf{commit},\ \mathsf{branch},\ \mathsf{dirty},\ \mathsf{remote?})
\qquad
\gamma_i = \mathsf{pin}(\Gamma_{\mathsf{git}})
\]

v0: минимум \( \mathsf{commit} \) hash (HEAD @ apply). Цель: **склейка** IDE timeline ↔ git history — у оператора и агента **полная картина**, не только replay графа.

\[
(\Lambda,\ \Gamma_{\mathsf{git}}) \Rightarrow \mathsf{Timeline}
\]

**Code History Timeline** — projection журнала для UI: scrub \( k \in [0,n] \), показать \( G_k \), diff \( G_{k-1} \to G_k \), pin \( \gamma_k \). Presentation / debug / «как мы сюда пришли» — **убийца на демо**, не side feature.

| ID | Формулировка |
|----|----------------|
| **LD1** | \( \Lambda \) **append-only**; удаление только compact/policy (archive), не silent mutate |
| **LD2** | \( r_i \) строго монотонны в пределах сессии |
| **LD3** | \( \mathsf{replay}(G_0, \Lambda) \) детерминирован при хранимых \( \Delta_i \) (или re-\( \mathsf{plan}(\theta_i, \varphi_{r_i}) \) @ stored φ) |
| **LD4** | CDP/habitat events **ingress** в \( \Lambda \) через orchestrator — не второй журнал |
| **LD5** | \( \gamma_i \) записывается на каждый **apply**; optional на preview-only |
| **LD6** | Git **не** заменяет \( G \); \( \Gamma_{\mathsf{git}} \) — correlation layer, `git commit` — отдельное событие (может batch несколько \( \varepsilon_i \)) |
