# Invalidation, scopes, semantic substrate (Σ_π)

| | |
|---|---|
| **Package** | [IDE Session axioms](README.md) |
| **Hub** | [ide-session-axioms-v0.md](../ide-session-axioms-v0.md) |

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

#### 5.2b File-as-graph (unification)

**Файл — не «внешний blob», а подграф**, сериализованный в текст:

\[
f \in \mathrm{paths}(\pi) \quad\Rightarrow\quad
\Sigma_\pi \supseteq (N_{\mathsf{syn}}(f),\ N_{\mathsf{sem}}(f),\ E_{\mathsf{dep}}|_f)
\]

| View | Носитель |
|------|----------|
| Текст @ path | сериализация \( N_{\mathsf{syn}}(f) \) (+ spans как атрибуты) |
| Buffer / editor | live projection на тот же подграф |
| \( \omega(f) = \pi \) | ownership на уровне solution graph \( G \) |

**Refactor / Move / Rename** — один морфизм \( G \to G' \) (§2.10):

\[
\mathsf{plan}(\theta,\ \varphi) \to \Delta = (\Delta_{\mathsf{fs}},\ \Delta_G)
\qquad
G' = G \oplus \Delta_G
\]

| Transform | \( \Delta_G \) | \( \mathsf{scope}(\Delta) \) |
|-----------|----------------|------------------------------|
| rename (symbol/local) | \( \emptyset \) | `FileChange` |
| move type → new file | \( \omega \) update | `ProjectFileCrud` |
| move/rename path | \( \omega \) transfer | `ProjectFileCrud` |

Проверяемость **по необходимости**: `validate(G')` всегда; Hoare \( Q_{\mathsf{obs}} \) / \( Q_{\mathsf{types}} \) — для класса \( \Theta_{\mathsf{ref}} \) (RF6), не для каждого vendor apply.

Код: `RefactorPlan`, `SessionPatch.scope`, `SessionPatch.apply` — единый pipeline preview → apply → invalidate.

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