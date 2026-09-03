namespace AIGuiders.Platform.Modeling.Ide.Session

open System
open System.IO

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

    let tryResolveProjectId (graph: SolutionGraph) (filePath: string) =
        let full = normalizePath filePath

        match Map.tryFind full graph.FileOwnership with
        | Some id -> Some id
        | None ->
            graph.FileOwnership
            |> Map.tryPick (fun ownedPath owner ->
                if String.Equals(normalizePath ownedPath, full, StringComparison.OrdinalIgnoreCase) then
                    Some owner
                else
                    None)

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
