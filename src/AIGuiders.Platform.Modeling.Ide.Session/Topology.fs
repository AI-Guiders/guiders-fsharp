namespace AIGuiders.Platform.Modeling.Ide.Session

/// <summary>How a capability is materialized by the orchestrator.</summary>
type ExecutionTopology =
    | InProcess
    | OutOfProcess
    | SubprocessTool
    | Adaptive

module ExecutionTopology =
    let toWire =
        function
        | InProcess -> "in-process"
        | OutOfProcess -> "out-of-process"
        | SubprocessTool -> "subprocess-tool"
        | Adaptive -> "adaptive"

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

/// <summary>α.buildDriver — math §7.3 (BS2): driver is a capability attribute, not a graph-schema change. None = catalog/policy default (tactical: MsBuildInterop).</summary>
type BuildDriver =
    | MsBuildInterop
    | DotNetCli
    | GuidersBuild
    | Npm

module BuildDriver =
    let toWire =
        function
        | MsBuildInterop -> "msbuild-interop"
        | DotNetCli -> "dotnet-cli"
        | GuidersBuild -> "guiders-build"
        | Npm -> "npm"

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
      AdaptiveRules: AdaptiveRule list
      BuildDriver: BuildDriver option }

module CapabilityAttributes =
    let defaults topology phase =
        { Topology = topology
          Phase = phase
          Warmth = Cold
          Cost = Interactive
          Scope = Project
          AdaptiveRules = []
          BuildDriver = None }
