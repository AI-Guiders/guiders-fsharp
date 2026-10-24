# AIGuiders.Platform.Modeling.Language.Adapters.Sql

SQL backends for the Language Resolver Center — parse diagnostics, statement-level outline and dialect invariant laws for .sql scripts, one backend per dialect.

## What you get

- Four ready `ILanguageBackend` implementations over one shared core:
  - `SqlLanguageBackend` — generic SQL (`sql`)
  - `SqlPostgresLanguageBackend` (`sql.postgres`), `SqlMssqlLanguageBackend` (`sql.mssql`), `SqlSqliteLanguageBackend` (`sql.sqlite`)
- `SqlLanguageActivation` — `CanHandle` rules: a `.sql` path plus session language-id hint matching (aliases `postgres`; `mssql`/`tsql`; `sqlite`; the generic backend accepts empty/`sql`/`generic` hints and yields to any dialect hint)
- Diagnostics per dialect, two layers:
  - parse errors from the bundled SQL parser, id `sql.parse`
  - dialect invariant laws from `SqlInvariantLaws` (ids like `sql.script/no-top`):
    - PostgreSQL / SQLite — errors on `SELECT TOP` and `DATEADD()`
    - MSSQL — errors on trailing `LIMIT` and PostgreSQL interval date syntax
    - Generic — portability warnings/hints for `TOP`, `LIMIT`, `DATEADD()`, interval syntax, bracket identifiers
- `GetDocumentSymbolsAsync` — one symbol per statement: kind = statement type (`select`, `insert`, …), name = `kind_index`
- `SqlDialect` — `Generic` / `Postgres` / `Mssql` / `Sqlite` DU, plus `SqlDialect.lawCode`; `SqlInvariantLaws.evaluate` is pure and usable standalone
- Semantic verbs (`GoToDefinitionAsync`, `FindUsagesAsync`, `GetCompletionsAsync`, `GetSymbolAtPositionAsync`, `RenameSymbolAsync`) are intentional stubs

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Language
open AIGuiders.Platform.Modeling.Language.Adapters.Sql

// Standalone law check — no backend host needed
let violations =
    SqlInvariantLaws.evaluate
        SqlDialect.Postgres "report.sql" LanguageIds.SqlPostgres
        "SELECT TOP 5 * FROM orders;"

// Or as a full backend verb (parse + law diagnostics)
let backend = SqlMssqlLanguageBackend() :> ILanguageBackend
let hint : ProjectHint =
    { SolutionOrProjectPath = ""; SessionDefaultLanguageId = "tsql" }
backend.CanHandle("report.sql", hint)   // true
```

## Problem

- SQL scripts move between engines, and dialect drift is silent: `SELECT TOP` runs on SQL Server but not on PostgreSQL; `DATEADD()` and `::date + INTERVAL` mark a script as written for one engine.
- Generic parsers catch syntax errors but not "valid T-SQL sitting in a Postgres repo" — and polyglot tooling still wants `.sql` files to answer the same verbs as code files.

## Solution

- A dialect-aware backend set on one parser: each backend claims `.sql` files via the session language hint, then layers engine-specific invariant laws on top of parse diagnostics — the laws are a small closed vocabulary with stable ids, so CI can gate on them.
- Dialects are wired from the `language.sql.*` profile flavours of [ADR-0067](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0067-language-profile-federation-model.md); the backends fill the SQL slots of the Language Resolver Center ([ADR-0061](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0061-language-resolver-center.md)).

## When not to use

- Not a SQL validator/linter general: the laws are a closed regex set (`TOP`, `LIMIT`, `DATEADD`, interval, bracket identifiers), one diagnostic per script per rule — no full grammar or dialect coverage beyond that.
- No semantic tier: navigation, usages, completions and rename return empty; the outline is statement-level (`select_1`, `insert_2`), not tables/columns.
- Dialect pick needs the session hint — a bare `.sql` path alone routes to the generic backend; if your tool cannot supply a `ProjectHint`, you only get portability warnings, not engine-specific errors.

## Learn more

- [ADR-0067 — Language Profile federation model](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0067-language-profile-federation-model.md)
- [ADR-0061 — Language Resolver Center](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0061-language-resolver-center.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
