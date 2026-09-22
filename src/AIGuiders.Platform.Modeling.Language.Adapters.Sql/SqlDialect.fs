namespace AIGuiders.Platform.Modeling.Language.Adapters.Sql

/// <summary>sql.script flavour wired from language.sql.* plugin ids.</summary>
type SqlDialect =
    | Generic
    | Postgres
    | Mssql
    | Sqlite

module SqlDialect =
    let lawCode suffix = $"sql.script/{suffix}"
