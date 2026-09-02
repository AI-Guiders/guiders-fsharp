namespace AIGuiders.Platform.Modeling.Ide.Session

/// <summary>\( E_{\mathsf{proj}} \subseteq \mathbb{P} \times \mathbb{P} \) — project references, slnx membership order.</summary>
[<Struct; StructuralEquality; StructuralComparison>]
type ProjectEdge =
    { From: ProjectId
      To: ProjectId }

module ProjectEdge =
    let create fromId toId = { From = fromId; To = toId }
