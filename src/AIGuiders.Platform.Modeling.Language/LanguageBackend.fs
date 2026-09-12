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

/// <summary>Extension-based language id resolution per GUIDERS-ADR-0061 §3.</summary>
module LanguagePathRules =
    let resolveLanguageId (path: string) : string option =
        if String.IsNullOrWhiteSpace path then
            None
        else
            let fileName = Path.GetFileName path
            let ext = Path.GetExtension path

            if ext.Equals(".sln", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".slnx", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Csharp
            elif ext.Equals(".csproj", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Csharp
            elif ext.Equals(".fsproj", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Fsharp
            elif ext.Equals(".gdlproj", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Gdl
            elif fileName.Equals("tsconfig.json", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Typescript
            elif fileName.Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Python
            elif ext.Equals(".fsx", StringComparison.OrdinalIgnoreCase)
                 || ext.Equals(".fs", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Fsharp
            elif ext.Equals(".cs", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Csharp
            elif ext.Equals(".ts", StringComparison.OrdinalIgnoreCase)
                 || ext.Equals(".tsx", StringComparison.OrdinalIgnoreCase)
                 || ext.Equals(".js", StringComparison.OrdinalIgnoreCase)
                 || ext.Equals(".jsx", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Typescript
            elif ext.Equals(".ps1", StringComparison.OrdinalIgnoreCase)
                 || ext.Equals(".psm1", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.PowerShell
            elif ext.Equals(".py", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Python
            elif ext.Equals(".pas", StringComparison.OrdinalIgnoreCase)
                 || ext.Equals(".dpr", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Delphi
            elif ext.Equals(".gdl", StringComparison.OrdinalIgnoreCase) then
                Some LanguageIds.Gdl
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
