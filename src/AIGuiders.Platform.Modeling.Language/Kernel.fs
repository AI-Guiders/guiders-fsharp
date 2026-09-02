namespace AIGuiders.Platform.Modeling.Language

open System

[<CLIMutable>]
type SourceSpan =
    { Path: string
      Line: int
      Column: int
      EndLine: int
      EndColumn: int }

type Severity =
    | Error
    | Warning
    | Info
    | Hint

[<CLIMutable>]
type LanguageDiagnostic =
    { Id: string
      Severity: Severity
      Message: string
      Span: SourceSpan
      Tags: string[]
      Language: string }

[<CLIMutable>]
type LanguageSymbol =
    { Name: string
      Kind: string
      Span: SourceSpan
      Container: string
      Children: LanguageSymbol[] }

[<CLIMutable>]
type LanguageNavigation =
    { Definition: SourceSpan
      Declarations: SourceSpan[] }

[<CLIMutable>]
type LanguageReference =
    { Span: SourceSpan
      Target: SourceSpan
      Kind: string }

[<CLIMutable>]
type LanguageCompletion =
    { Label: string
      Kind: string
      Detail: string
      InsertText: string }

[<CLIMutable>]
type DiagnosticsResult = { Diagnostics: LanguageDiagnostic[] }

[<CLIMutable>]
type DocumentSymbolsResult = { Root: LanguageSymbol }

[<CLIMutable>]
type CompletionsResult = { Items: LanguageCompletion[] }

[<CLIMutable>]
type FindUsagesResult = { References: LanguageReference[] }

[<CLIMutable>]
type LanguageRequest =
    { FilePath: string
      Line: int
      Column: int
      SourceText: string
      SolutionOrProjectPath: string }
