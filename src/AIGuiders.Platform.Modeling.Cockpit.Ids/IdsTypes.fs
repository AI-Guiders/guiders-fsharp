namespace AIGuiders.Platform.Modeling.Cockpit.Ids

/// <summary>One IDS palette hit — go/tool + score (CIDE ADR 0079).</summary>
[<CLIMutable>]
type IdsFeatureHit =
    { Go: string
      Score: int
      Tool: string }

module IdsFeatureHit =

    let make go score tool =
        { Go = go; Score = score; Tool = tool }

    let byScoreDesc (hits: IdsFeatureHit seq) =
        hits |> Seq.sortByDescending (fun h -> h.Score)