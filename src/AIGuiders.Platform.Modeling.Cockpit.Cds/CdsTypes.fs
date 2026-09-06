namespace AIGuiders.Platform.Modeling.Cockpit.Cds

open System.Collections.Generic
open System.Text.Json

/// <summary>CDS input: normalize MFD/go attention before channel dispatch (ADR 0036/0097).
/// Nullability mirrors C# `string?` inputs.</summary>
[<CLIMutable>]
type AttentionRoutingInput =
    { MfdExplicit: string | null
      GoVerb: string | null
      SeatsMode: bool
      DefaultMfd: string | null }

/// <summary>CDS decision after attention routing.</summary>
[<CLIMutable>]
type AttentionRoutingDecision =
    { Mfd: string
      GoVerb: string
      DeskDetailNavForced: bool }

/// <summary>CDS decision: desk_detail / nav_detail resolution (ADR 0097).</summary>
[<CLIMutable>]
type DeskDetailDecision =
    { DeskDetail: string
      WantNav: bool }

/// <summary>CDS go-verb catalog entry: organ tool + default args (ADR 0036).</summary>
[<CLIMutable>]
type DeskGoMapEntry =
    { Tool: string
      Defaults: IReadOnlyDictionary<string, JsonElement> }
