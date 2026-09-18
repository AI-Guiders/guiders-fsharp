namespace AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Paths

type TextSpan = { StartLine: int; StartCol: int; EndLine: int; EndCol: int }

type LineRangeHint = { StartLine: int; EndLine: int option }

type SymbolRef =
    { Container: string list
      Name: string
      Arity: int option }

type AdrObligationId = AdrObligationId of id: string

type DocumentPlace =
    | Fragment of path: LogicalPath * anchor: string
    | Obligation of AdrObligationId
    | Package of packageId: string * memberName: string option

type DocumentRef =
    | DocId of DocId
    | File of LogicalPath

type Locus =
    | Syntax of doc: DocId * node: NodeId * span: TextSpan
    | Semantic of doc: DocId * symbol: SymbolRef * span: TextSpan

type NavSeed =
    { Path: LogicalPath
      Line: int option
      Column: int option
      Command: string option
      Go: string option
      Solution: LogicalPath option
      Member: string option }

type ResourcePath = ResourcePath of path: LogicalPath

type SurfaceVersion = SurfaceVersion of value: int64

type DocumentText = DocumentText of text: string

module DocumentText =
    let value (DocumentText t) = t

type DocumentMeta =
    { Path: LogicalPath
      Owner: ProjectId
      SurfaceVersion: SurfaceVersion }

type DocumentRegistry = Map<DocId, DocumentMeta>

type DiagnosticRecord =
    { Code: string
      Severity: string
      Message: string
      Doc: DocId
      Span: TextSpan
      Tags: string list
      Language: string
      SurfaceVersion: SurfaceVersion }

type DiagnosticIndex = Map<DiagnosticRef, DiagnosticRecord>

type ResolveCtx =
    { ActiveDoc: DocId
      Registry: DocumentRegistry
      Diagnostics: DiagnosticIndex
      Tier: ResolveTier }

type AddressRef =
    | Diagnostic of DiagnosticRef
    | Artifact of ArtifactRef
