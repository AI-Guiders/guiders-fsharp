namespace AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

open AIGuiders.Platform.Modeling.Core.Identity

type CodeTarget =
    | Symbol of doc: DocumentRef * symbol: SymbolRef
    | TreeNode of doc: DocumentRef * nodeId: NodeId

type RelationSpec =
    | CodeEdit of target: CodeTarget
    | DocToCode of source: DocumentPlace * target: CodeTarget
    | Diag of source: DiagnosticRef
    | Address of source: AddressRef * targetHint: ArtifactRef option
    | Nav of source: NavSeed
    | Resource of source: ResourcePath * codeTail: CodeTarget option
