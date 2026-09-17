namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet

open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.Paths
open DotNetWorkspace.Core

/// Pre-loaded project topology row — Execution reads csproj/fsproj from disk.
type DotNetProjectTopologyRow =
    { Entry: DotNetProjectEntry
      ProjectReferences: string list
      SourceFiles: string list }

type DotNetSolutionTopology =
    { SolutionPath: string
      Fingerprint: string
      Rows: DotNetProjectTopologyRow list }

module DotNetSlnxGraphPort =
    let toProjectKind (entry: DotNetProjectEntry) =
        match entry.Kind with
        | DotNetWorkspace.Core.DotNetProjectKind.CSharp -> DotNet { Language = DotNetLanguage.CSharp }
        | DotNetWorkspace.Core.DotNetProjectKind.FSharp -> DotNet { Language = DotNetLanguage.FSharp }
        | DotNetWorkspace.Core.DotNetProjectKind.Unknown -> failwith $"Unsupported managed project '{entry.AbsolutePath}'."

    let buildProjectNodes (entries: DotNetProjectEntry list) =
        entries
        |> List.map (fun entry ->
            let id = ProjectId.create entry.AbsolutePath

            ProjectNode.create
                id
                (toProjectKind entry)
                entry.AbsolutePath
                (ProjectCapabilityCatalog.forKind (toProjectKind entry)))

    let buildProjectRefRelations (rows: DotNetProjectTopologyRow list) =
        let byPath =
            rows
            |> List.map (fun row -> row.Entry.AbsolutePath, ProjectId.create row.Entry.AbsolutePath)
            |> Map.ofList

        rows
        |> List.collect (fun row ->
            row.ProjectReferences
            |> List.choose (fun refPath ->
                match Map.tryFind refPath byPath with
                | None -> None
                | Some toId ->
                    Some(RelationGraph.projectRef (ProjectId.create row.Entry.AbsolutePath) toId)))

    let buildDocumentOwnership (rows: DotNetProjectTopologyRow list) =
        rows
        |> List.collect (fun row ->
            let owner = ProjectId.create row.Entry.AbsolutePath

            row.SourceFiles |> List.map (fun source -> source, owner))
        |> List.fold (fun acc (source, owner) -> Map.add source owner acc) Map.empty

    /// <summary>Pure federation topology graph from pre-loaded rows (ω lives on runtime registry).</summary>
    let buildGraph (topology: DotNetSolutionTopology) : SolutionGraph =
        let entries = topology.Rows |> List.map (fun row -> row.Entry)
        let projects = buildProjectNodes entries
        let relations = buildProjectRefRelations topology.Rows

        SolutionGraph.create (LogicalPath.Create topology.SolutionPath) projects relations

    let buildSession (topology: DotNetSolutionTopology) : SolutionSession =
        let graph = buildGraph topology

        SolutionSession.create graph.Anchor graph
        |> SolutionSession.withPhase DesignTime

    let buildRuntime
        (topology: DotNetSolutionTopology)
        (pathContents: seq<string * string>)
        : SessionRuntime =
        let session = buildSession topology
        let ownership = buildDocumentOwnership topology.Rows
        SessionOrchestrator.create session pathContents ownership
