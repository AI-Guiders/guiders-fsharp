namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet

open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.Paths

module DotNetSlnxGraphPort =
    open System.IO
    open DotNetWorkspace.Core
    open ProjectFileReader

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

    let buildProjectEdges (entries: DotNetProjectEntry list) =
        let byPath =
            entries
            |> List.map (fun e -> e.AbsolutePath, ProjectId.create e.AbsolutePath)
            |> Map.ofList

        entries
        |> List.collect (fun entry ->
            readProjectReferences entry.AbsolutePath
            |> List.choose (fun refPath ->
                match Map.tryFind refPath byPath with
                | None -> None
                | Some toId ->
                    Some
                        { From = ProjectId.create entry.AbsolutePath
                          To = toId }))

    let buildDocumentOwnership (entries: DotNetProjectEntry list) =
        entries
        |> List.collect (fun entry ->
            let owner = ProjectId.create entry.AbsolutePath

            readSourceFiles entry.AbsolutePath
            |> List.map (fun source -> source, owner))
        |> List.fold (fun acc (source, owner) -> Map.add source owner acc) Map.empty

    /// <summary>Parse slnx/sln/csproj/fsproj anchor into federation topology graph (ω lives on runtime registry).</summary>
    let load (anchorPath: string) : SolutionGraph =
        let parsed = DotNetWorkspace.Load anchorPath
        let entries = parsed.Projects |> Seq.toList

        let projects = buildProjectNodes entries
        let projectEdges = buildProjectEdges entries

        SolutionGraph.create (LogicalPath.Create parsed.SolutionPath) projects projectEdges []

    let loadDocumentOwnership (anchorPath: string) : Map<string, ProjectId> =
        let parsed = DotNetWorkspace.Load anchorPath
        buildDocumentOwnership (parsed.Projects |> Seq.toList)

    let loadSession (anchorPath: string) : SolutionSession =
        let graph = load anchorPath

        SolutionSession.create graph.Anchor graph
        |> SolutionSession.withPhase DesignTime

    let loadRuntime (anchorPath: string) (sourceOverrides: Map<string, string>) : SessionRuntime =
        let session = loadSession anchorPath
        let ownership = loadDocumentOwnership anchorPath

        let pathContents =
            ownership
            |> Map.toSeq
            |> Seq.map (fun (path, _) ->
                let text =
                    match Map.tryFind path sourceOverrides with
                    | Some t -> t
                    | None when File.Exists path -> File.ReadAllText path
                    | _ -> ""

                path, text)

        SessionOrchestrator.create session pathContents ownership
