namespace AIGuiders.Platform.Modeling.Ide.Session

open System

/// <summary>\( E_{\mathsf{proj}} \subseteq \mathbb{P} \times \mathbb{P} \) — legacy bridge; prefer RelationType.ProjectRef in G.</summary>
[<Obsolete("Use Relation with RelationType.ProjectRef in SolutionGraph.Relations.", false)>]
type ProjectEdge =
    { From: ProjectId
      To: ProjectId }

module ProjectEdge =
    let create fromId toId = { From = fromId; To = toId }
