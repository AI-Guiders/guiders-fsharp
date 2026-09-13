namespace AIGuiders.Platform.Modeling.IdeSession.GateCatalog

/// <summary>
/// IR for ide-session *.catalog.gdl gates table (SAT-004 / GUIDERS-FSHARP-ADR-0004).
/// </summary>
[<CLIMutable>]
type IdeSessionGateRow =
    { GateId: string
      RejectWhen: string
      Code: string }

[<CLIMutable>]
type IdeSessionGateCatalog =
    { SourcePath: string
      Gates: IdeSessionGateRow array }
