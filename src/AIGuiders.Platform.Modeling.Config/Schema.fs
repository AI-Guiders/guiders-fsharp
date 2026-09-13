namespace AIGuiders.Platform.Modeling.Config

/// <summary>
/// Pilot IR for *.config.gdl (GUIDERS-ADR-0064 v0): header, defaults, contracts.
/// </summary>
[<CLIMutable>]
type ConfigContractRow =
    { Id: string
      Requires: string
      Ensures: string
      Line: int }

[<CLIMutable>]
type ConfigDocument =
    { Name: string
      BasedOnAdr: string option
      Defaults: System.Collections.Generic.IDictionary<string, string>
      Contracts: ConfigContractRow array }
