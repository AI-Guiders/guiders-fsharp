namespace AIGuiders.Platform.Modeling.Gdl.Agent

open System.Collections.Generic

type DetailTier =
    | Pulse = 0
    | Slim = 1
    | Full = 2
    | Wide = 3

/// <summary>Next-step hint row for the agent client.</summary>
type NextHint =
    { Kind: string
      CommandId: string option
      ToolName: string option
      Label: string option }

/// <summary>
/// Agent response envelope: pulse/slim/full payload + hints.
/// IntentOutcome (Modeling.Core) stays execution-side — neutral port carries wire fields only.
/// </summary>
type AgentResponseEnvelope =
    { Ok: bool
      Tier: DetailTier
      Pulse: string option
      Reason: string option
      Next: IReadOnlyList<NextHint> option }