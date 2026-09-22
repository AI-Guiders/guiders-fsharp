namespace AIGuiders.Platform.Modeling.Language

open System
open System.IO
open System.Threading
open System.Threading.Tasks

/// <summary>Optional project/solution hint for backend resolution (GUIDERS-ADR-0061).</summary>
[<CLIMutable>]
type ProjectHint =
    { SolutionOrProjectPath: string
      SessionDefaultLanguageId: string }

/// <summary>LRC rename verb request envelope.</summary>
[<CLIMutable>]
type RenameSymbolRequest =
    { Request: LanguageRequest
      NewName: string
      Apply: bool }

/// <summary>Well-known federation language ids for Language Resolver Center.</summary>
module LanguageIds =
    [<Literal>]
    let Csharp = "csharp"

    [<Literal>]
    let Fsharp = "fsharp"

    [<Literal>]
    let Typescript = "typescript"

    [<Literal>]
    let Gdl = "gdl"

    [<Literal>]
    let PowerShell = "powershell"

    [<Literal>]
    let Python = "python"

    [<Literal>]
    let Delphi = "delphi"

    [<Literal>]
    let Dashspec = "dashspec"

    [<Literal>]
    let Sql = "sql"

    [<Literal>]
    let SqlPostgres = "sql.postgres"

    [<Literal>]
    let SqlMssql = "sql.mssql"

    [<Literal>]
    let SqlSqlite = "sql.sqlite"

/// <summary>DashSpec planet file roots (ADR-0017) — SSOT for LRC + Code Center activation.</summary>
module DashSpecPathRules =
    let extensions =
        [| ".dash"
           ".dashspec"
           ".dashdiagram"
           ".dashlayout"
           ".dashpalette"
           ".dashpresentation"
           ".dashtransform"
           ".dashcatalog"
           ".dashtooltip"
           ".dashinclude" |]

    let private extensionSet = extensions |> Set.ofArray

    let isDashSpecPath (path: string) =
        if String.IsNullOrWhiteSpace path then
            false
        else
            extensionSet.Contains(Path.GetExtension(path).ToLowerInvariant())

/// <summary>Optional Code Center plugin activation catalog for LRC resolve (GUIDERS-FSHARP-ADR-0009).</summary>
type ILanguageActivationCatalog =
    abstract ResolveLanguageId: path: string -> string

/// <summary>Extension-based language id resolution per GUIDERS-ADR-0061 §3.</summary>
module LanguagePathRules =
    let resolveLanguageId (path: string) : string option =
        if String.IsNullOrWhiteSpace path then
            None
        else
            let fileName = Path.GetFileName path
            let ext = Path.GetExtension path

            if String.Equals(ext, ".sln", StringComparison.OrdinalIgnoreCase)
               || String.Equals(ext, ".slnx", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Csharp
            elif String.Equals(ext, ".csproj", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Csharp
            elif String.Equals(ext, ".fsproj", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Fsharp
            elif String.Equals(ext, ".gdlproj", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Gdl
            elif String.Equals(fileName, "tsconfig.json", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Typescript
            elif String.Equals(fileName, "pyproject.toml", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Python
            elif String.Equals(ext, ".fsx", StringComparison.OrdinalIgnoreCase)
                 || String.Equals(ext, ".fs", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Fsharp
            elif String.Equals(ext, ".cs", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Csharp
            elif String.Equals(ext, ".ts", StringComparison.OrdinalIgnoreCase)
                 || String.Equals(ext, ".tsx", StringComparison.OrdinalIgnoreCase)
                 || String.Equals(ext, ".js", StringComparison.OrdinalIgnoreCase)
                 || String.Equals(ext, ".jsx", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Typescript
            elif String.Equals(ext, ".ps1", StringComparison.OrdinalIgnoreCase)
                 || String.Equals(ext, ".psm1", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.PowerShell
            elif String.Equals(ext, ".py", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Python
            elif String.Equals(ext, ".pas", StringComparison.OrdinalIgnoreCase)
                 || String.Equals(ext, ".dpr", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Delphi
            elif String.Equals(ext, ".gdl", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Gdl
            elif String.Equals(ext, ".sql", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Sql
            elif DashSpecPathRules.isDashSpecPath path then
                Some LanguageIds.Dashspec
            else
                None

/// <summary>
/// Federation language backend seam (GUIDERS-FSHARP-ADR-0003 §4.6).
/// Modeling adapters implement this; Execution.Language hosts resolver + drive.
/// </summary>
type ILanguageBackend =
    abstract LanguageId: string
    abstract CanHandle: path: string * hint: ProjectHint -> bool
    abstract GetDiagnosticsAsync: LanguageRequest * CancellationToken -> Task<DiagnosticsResult>
    abstract GetDocumentSymbolsAsync: LanguageRequest * CancellationToken -> Task<DocumentSymbolsResult>
    abstract GoToDefinitionAsync: LanguageRequest * CancellationToken -> Task<LanguageNavigation>
    abstract FindUsagesAsync: LanguageRequest * CancellationToken -> Task<FindUsagesResult>
    abstract GetCompletionsAsync: LanguageRequest * CancellationToken -> Task<CompletionsResult>
    abstract GetSymbolAtPositionAsync: LanguageRequest * CancellationToken -> Task<SymbolAtPositionResult>
    abstract RenameSymbolAsync: RenameSymbolRequest * CancellationToken -> Task<RenameSymbolResult>
