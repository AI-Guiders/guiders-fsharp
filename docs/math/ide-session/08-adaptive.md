# Adaptive topology resolution

| | |
|---|---|
| **Package** | [IDE Session axioms](README.md) |
| **Hub** | [ide-session-axioms-v0.md](../ide-session-axioms-v0.md) |

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