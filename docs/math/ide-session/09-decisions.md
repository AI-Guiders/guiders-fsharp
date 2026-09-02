# Resolved decisions and open questions

| | |
|---|---|
| **Package** | [IDE Session axioms](README.md) |
| **Hub** | [ide-session-axioms-v0.md](../ide-session-axioms-v0.md) |

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

### 9.11 Revision ledger & Timeline — **направление зафиксировано**

§2.12: \( \Lambda \) (Δ-stream) + Git overlay \( \gamma \) @ apply; replay \( G_0 \to G_n \); Timeline = projection для UI/agent. CDP/habitat — ingress, не второй SSOT.

### 9.12 Planet / Gdl как \( \tau \) — **решено**

**Без special case:** \( \tau \in \{\mathsf{DotNet},\mathsf{Node},\mathsf{Gdl},\mathsf{Planet}\} \) — разный \( \mathcal{K}_\tau \) и port, **тот же** оркестратор (§1.5 T1–T3).

- Declare-time GDL project = \( \pi \) с \( \tau=\mathsf{Gdl} \), не второй IDE и не параллельный SSOT.
- Planet bundle = \( \pi \) с \( \tau=\mathsf{Planet} \); язык — метка на \( \pi \), не новый сорт графа.
- Mixed federation: \( E_{\mathsf{proj}} \) связывает DotNet ↔ Gdl ↔ Planet в одном \( G \).

Отвергнуто: отдельная «GDL session», обход invalidation/freeze/ledger для declare-time, `if (gdl)` в ядре оркестратора.

### 9.13 Conformance golden sessions — **решено**

### 9.14 Federation reframe of CDP — **направление зафиксировано**

[GUIDERS-FSHARP-ADR-0005](../../adr/GUIDERS-FSHARP-ADR-0005-federation-reframe-cdp-features.md): Modeling → Execution → CDP; feature map; Correspondence = next pilot.
