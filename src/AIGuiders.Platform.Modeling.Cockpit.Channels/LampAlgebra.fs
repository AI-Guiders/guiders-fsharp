namespace AIGuiders.Platform.Modeling.Cockpit.Channels

/// <summary>W/C/A annunciator lamp level (ADR 0021 / EICAS grammar).</summary>
type AnnunciatorLampLevel =
    | Ok
    | Advisory
    | Caution
    | Critical

/// <summary>One annunciator / Korry cell on a lamp strip (ADR 0063).</summary>
[<CLIMutable>]
type AnnunciatorLampItem =
    { Id: string
      Title: string
      Detail: string
      Level: AnnunciatorLampLevel
      LampShortLabel: string }

module AnnunciatorLamp =
    /// Lamp row algebra: level ordering W &lt; C &lt; A (EICAS strip priority).
    let severityRank (level: AnnunciatorLampLevel) =
        match level with
        | Ok -> 0
        | Advisory -> 1
        | Caution -> 2
        | Critical -> 3

    /// Highest severity across a strip (drives master lamp).
    let maxLevel (items: AnnunciatorLampItem seq) =
        if Seq.isEmpty items then Ok
        else
            items
            |> Seq.map (fun i -> i.Level)
            |> Seq.maxBy severityRank