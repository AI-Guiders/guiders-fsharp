# Refactor G→G', Hoare, Code Style

| | |
|---|---|
| **Package** | [IDE Session axioms](README.md) |
| **Hub** | [ide-session-axioms-v0.md](../ide-session-axioms-v0.md) |

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

**Читаемый SSOT (GDL):** тот же каталог операций и gates — без \( \theta \), \( Q_* \) — в [`docs/gdl/ide-session.catalog.gdl`](../../gdl/ide-session.catalog.gdl). Harness и агенты читают GDL; математика здесь остаётся нормативной ссылкой (RF6–RF8).

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
