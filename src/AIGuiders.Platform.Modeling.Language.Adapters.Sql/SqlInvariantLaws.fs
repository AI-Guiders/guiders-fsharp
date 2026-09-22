namespace AIGuiders.Platform.Modeling.Language.Adapters.Sql

open System
open System.Text.RegularExpressions
open AIGuiders.Platform.Modeling.Language

/// Dialect-specific invariant laws for <c>sql.script</c> (GUIDERS-ADR-0067).
module SqlInvariantLaws =
    let private topPrefix = Regex(@"\bSELECT\b[\s\S]*?\bTOP\b\s+\d+", RegexOptions.IgnoreCase ||| RegexOptions.CultureInvariant)
    let private trailingLimit = Regex(@"\bLIMIT\b\s+\d+\s*(;|\s)*$", RegexOptions.IgnoreCase ||| RegexOptions.CultureInvariant)
    let private dateAdd = Regex(@"\bDATEADD\s*\(", RegexOptions.IgnoreCase ||| RegexOptions.CultureInvariant)
    let private postgresInterval = Regex(@"::date\s*\+\s*INTERVAL\b", RegexOptions.IgnoreCase ||| RegexOptions.CultureInvariant)
    let private bracketIdentifier = Regex(@"\[[^\]]+\]", RegexOptions.CultureInvariant)

    let private diagnostic path languageId code severity message line column =
        { Id = code
          Severity = severity
          Message = message
          Span =
            { Path = path
              Line = max 1 line
              Column = max 1 column
              EndLine = max 1 line
              EndColumn = max 1 (column + 1) }
          Tags = [| "sql.script"; "invariant" |]
          Language = languageId }

    let private scan (path: string) (languageId: string) (code: string) severity message (pattern: Regex) (text: string) =
        if pattern.IsMatch text then
            [ diagnostic path languageId code severity message 1 1 ]
        else
            []

    let evaluate (dialect: SqlDialect) (path: string) (languageId: string) (text: string) =
        let body = if String.IsNullOrWhiteSpace text then "" else text

        match dialect with
        | SqlDialect.Postgres
        | SqlDialect.Sqlite ->
            [ yield! scan path languageId (SqlDialect.lawCode "no-top") Error "PostgreSQL/SQLite scripts must not use SELECT TOP." topPrefix body
              yield! scan path languageId (SqlDialect.lawCode "no-dateadd") Error "PostgreSQL/SQLite scripts must not use DATEADD()." dateAdd body ]

        | SqlDialect.Mssql ->
            [ yield! scan path languageId (SqlDialect.lawCode "no-trailing-limit") Error "T-SQL scripts must use SELECT TOP, not trailing LIMIT." trailingLimit body
              yield! scan path languageId (SqlDialect.lawCode "no-postgres-interval") Error "T-SQL scripts must not use PostgreSQL interval date syntax." postgresInterval body ]

        | SqlDialect.Generic ->
            [ yield!
                scan path languageId (SqlDialect.lawCode "dialect-top") Warning "TOP is T-SQL-specific; prefer LIMIT for portable SQL." topPrefix body
              yield!
                scan path languageId (SqlDialect.lawCode "dialect-limit") Warning "Trailing LIMIT is PostgreSQL/SQLite-specific." trailingLimit body
              yield!
                scan path languageId (SqlDialect.lawCode "dialect-dateadd") Warning "DATEADD() is T-SQL-specific." dateAdd body
              yield!
                scan path languageId (SqlDialect.lawCode "dialect-interval") Warning "PostgreSQL interval syntax is not portable." postgresInterval body
              yield!
                scan path languageId (SqlDialect.lawCode "dialect-brackets") Hint "Bracket identifiers are T-SQL-style; double-quote or unquoted names differ by engine." bracketIdentifier body ]
