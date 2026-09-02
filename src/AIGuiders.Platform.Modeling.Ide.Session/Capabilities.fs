namespace AIGuiders.Platform.Modeling.Ide.Session

[<StructuralEquality; StructuralComparison>]
type CapabilityKind =
    | CompilerServices
    | StaticAnalysis
    | Build
    | TestDiscovery
    | TestRun
    | LspBridge

type CapabilityNode =
    { Kind: CapabilityKind
      Attributes: CapabilityAttributes }

module CapabilityKind =
    let id =
        function
        | CompilerServices -> "compiler-services"
        | StaticAnalysis -> "static-analysis"
        | Build -> "build"
        | TestDiscovery -> "test-discovery"
        | TestRun -> "test-run"
        | LspBridge -> "lsp-bridge"

module CapabilityCatalog =
    let compilerServices () =
        { Kind = CompilerServices
          Attributes =
            CapabilityAttributes.defaults InProcess DesignTime
            |> fun a -> { a with Warmth = Warm } }

    let staticAnalysisAdaptive () =
        { Kind = StaticAnalysis
          Attributes =
            { Topology = Adaptive
              Phase = DesignTime
              Warmth = Cold
              Cost = Heavy
              Scope = Solution
              AdaptiveRules =
                [ WhenAlreadyWarm InProcess
                  WhenFullSolutionScan OutOfProcess ] } }

    let buildTool () =
        { Kind = Build
          Attributes = CapabilityAttributes.defaults SubprocessTool CompileTime }

    let testDiscovery () =
        { Kind = TestDiscovery
          Attributes = CapabilityAttributes.defaults OutOfProcess TestTime }

    let lspBridge () =
        { Kind = LspBridge
          Attributes = CapabilityAttributes.defaults OutOfProcess DesignTime }

    let defaultDotNet () =
        [ compilerServices ()
          buildTool () ]

    let defaultNode () =
        [ compilerServices ()
          buildTool () ]

    let defaultGdl () =
        [ compilerServices () ]

    let defaultPlanet () =
        [ compilerServices () ]

/// <summary>Address in the unified session graph (project or capability under project).</summary>
[<StructuralEquality; StructuralComparison>]
type GraphNodeId =
    | ProjectNode of ProjectId
    | CapabilityNode of ProjectId * CapabilityKind

module GraphNodeId =
    let project (ProjectId _ as pid) = ProjectNode pid

    let capability pid kind = CapabilityNode(pid, kind)

    let key =
        function
        | ProjectNode(ProjectId id) -> $"p:{id}"
        | CapabilityNode(ProjectId pid, kind) -> $"c:{pid}:{CapabilityKind.id kind}"
