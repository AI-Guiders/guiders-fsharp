namespace AIGuiders.Platform.Modeling.Ide.Session

/// <summary>How a capability is materialized by the orchestrator.</summary>
type ExecutionTopology =
    | InProcess
    | OutOfProcess
    | SubprocessTool
    | Adaptive

type WarmthHint =
    | Cold
    | Warm
    | Hot

type CostTier =
    | Interactive
    | Standard
    | Heavy

type CapabilityScope =
    | File
    | Project
    | Solution

/// <summary>v0 placeholder — rules land in a later slice (ADR-0062 §2.1).</summary>
type AdaptiveRule =
    | WhenProjectFileCountBelow of int * ExecutionTopology
    | WhenAlreadyWarm of ExecutionTopology
    | WhenFullSolutionScan of ExecutionTopology
    | WhenElapsedBudgetExceeds of System.TimeSpan * ExecutionTopology

type CapabilityAttributes =
    { Topology: ExecutionTopology
      Phase: LifecyclePhase
      Warmth: WarmthHint
      Cost: CostTier
      Scope: CapabilityScope
      AdaptiveRules: AdaptiveRule list }

module CapabilityAttributes =
    let defaults topology phase =
        { Topology = topology
          Phase = phase
          Warmth = Cold
          Cost = Interactive
          Scope = Project
          AdaptiveRules = [] }
