namespace AIGuiders.Platform.Modeling.Configurations

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
type ConfigSourceRow =
    { Id: string
      Kind: string
      Path: string
      Slice: string
      Line: int }

[<CLIMutable>]
type ConfigFactRow =
    { Contract: string
      VerifiedBy: string
      Line: int }

[<CLIMutable>]
type ConfigDocument =
    { Name: string
      BasedOnAdr: string option
      Defaults: System.Collections.Generic.IDictionary<string, string>
      Sources: ConfigSourceRow array
      Contracts: ConfigContractRow array
      Facts: ConfigFactRow array }
