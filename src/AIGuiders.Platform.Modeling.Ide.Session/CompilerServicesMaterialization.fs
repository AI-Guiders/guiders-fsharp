namespace AIGuiders.Platform.Modeling.Ide.Session

open System
open System.IO
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

/// <summary>Result of <c>EnsureCompilerServices</c> — topology comes from capability attributes on the graph.</summary>
[<CLIMutable>]
type CompilerServicesMaterialization =
    { ProjectId: ProjectId
      CapabilityNode: GraphNodeId
      Topology: ExecutionTopology
      TopologyWire: string
      LanguageId: string
      Revision: int64
      WorkspaceView: WorkspaceView }

type CompilerServicesEnsureResult =
    | Ensured of CompilerServicesMaterialization * SessionRuntime
    | Failed of reason: string

module CompilerServicesMaterialization =
    let private normalizePath (path: string) =
        if String.IsNullOrWhiteSpace path then
            ""
        else
            Path.GetFullPath path

    let tryResolveProjectId (registry: DocumentRegistry) (filePath: string) =
        let full = normalizePath filePath

        registry
        |> Map.toList
        |> List.tryFind (fun (_, meta) ->
            String.Equals(normalizePath meta.Path.Value, full, StringComparison.OrdinalIgnoreCase))
        |> Option.map (fun (_, meta) -> meta.Owner)

    let languageIdForProject (project: ProjectNode) =
        match project.Kind with
        | DotNet { Language = CSharp } -> "csharp"
        | DotNet { Language = FSharp } -> "fsharp"
        | Node _ -> "typescript"
        | Gdl _ -> "gdl"
        | Planet { LanguageId = lid } -> lid

    let resolveTopology (attrs: CapabilityAttributes) =
        match attrs.Topology with
        | Adaptive ->
            attrs.AdaptiveRules
            |> List.tryPick (function
                | WhenAlreadyWarm topology -> Some topology
                | _ -> None)
            |> Option.defaultValue InProcess
        | topology -> topology

    let tryGetCompilerServices (project: ProjectNode) =
        project.Capabilities |> List.tryFind (fun c -> c.Kind = CompilerServices)
