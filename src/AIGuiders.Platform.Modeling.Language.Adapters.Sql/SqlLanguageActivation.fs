namespace AIGuiders.Platform.Modeling.Language.Adapters.Sql

open System
open AIGuiders.Platform.Modeling.Language

module SqlLanguageActivation =
    let isSqlPath (path: string) =
        not (String.IsNullOrWhiteSpace path)
        && String.Equals(System.IO.Path.GetExtension path, ".sql", StringComparison.OrdinalIgnoreCase)

    let matchesDialectHint (hint: ProjectHint) (dialectLanguageId: string) (aliases: string[]) =
        let session = hint.SessionDefaultLanguageId

        if String.IsNullOrWhiteSpace session then
            false
        elif session.Equals(dialectLanguageId, StringComparison.OrdinalIgnoreCase) then
            true
        else
            aliases |> Array.exists (fun alias -> session.Equals(alias, StringComparison.OrdinalIgnoreCase))

    let matchesGenericHint (hint: ProjectHint) =
        let session = hint.SessionDefaultLanguageId

        if String.IsNullOrWhiteSpace session
           || session.Equals(LanguageIds.Sql, StringComparison.OrdinalIgnoreCase)
           || session.Equals("generic", StringComparison.OrdinalIgnoreCase) then
            true
        elif session.StartsWith("sql.", StringComparison.OrdinalIgnoreCase) then
            false
        else
            not (
                session.Equals("postgres", StringComparison.OrdinalIgnoreCase)
                || session.Equals("tsql", StringComparison.OrdinalIgnoreCase)
                || session.Equals("mssql", StringComparison.OrdinalIgnoreCase)
                || session.Equals("sqlite", StringComparison.OrdinalIgnoreCase)
            )

    let canHandleGeneric path hint = isSqlPath path && matchesGenericHint hint

    let canHandlePostgres path hint =
        isSqlPath path && matchesDialectHint hint LanguageIds.SqlPostgres [| "postgres" |]

    let canHandleMssql path hint =
        isSqlPath path && matchesDialectHint hint LanguageIds.SqlMssql [| "mssql"; "tsql" |]

    let canHandleSqlite path hint =
        isSqlPath path && matchesDialectHint hint LanguageIds.SqlSqlite [| "sqlite" |]
