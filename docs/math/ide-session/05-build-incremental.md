# Build substrate, incremental compile, sniper emit

| | |
|---|---|
| **Package** | [IDE Session axioms](README.md) |
| **Hub** | [ide-session-axioms-v0.md](../ide-session-axioms-v0.md) |

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