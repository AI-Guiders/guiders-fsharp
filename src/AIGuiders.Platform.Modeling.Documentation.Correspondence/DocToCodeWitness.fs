namespace AIGuiders.Platform.Modeling.Documentation.Correspondence

/// <summary>CRS doc→code witness at federation boundary (plan §2.5; replaces legacy ReverseAnchor wire).</summary>
type DocToCodeWitness =
    { DocPath: string
      DocTitle: string
      Provenance: string
      Kind: CorrespondenceRelationKind
      File: string
      LineStart: int option
      LineEnd: int option
      MemberKey: string option
      Wire: string
      DocLineHint: int option
      Excerpt: string option }

type CorrespondenceResult =
    { WorkspaceRoot: string
      FileRel: string option
      FeatureLine: string option
      FeatureDocs: string array
      AdrLine: string
      ForwardDocs: ForwardDoc array
      DocToCodeWitnesses: DocToCodeWitness array
      ActiveLayers: string array
      TomlPath: string }
