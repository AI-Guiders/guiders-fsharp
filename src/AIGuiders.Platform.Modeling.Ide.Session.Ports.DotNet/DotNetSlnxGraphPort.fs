namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet

open AIGuiders.Platform.Modeling.Ide.Session

module DotNetSlnxGraphPort =
    open System.IO
    open DotNetWorkspace.Core
    open ProjectFileReader

    let private toProjectKind (entry: DotNetProjectEntry) =
        match entry.Kind with
        | DotNetProjectKind.CSharp -> DotNet { Language = CSharp }
        | DotNetProjectKind.FSharp -> DotNet { Language = FSharp }
        | DotNetProjectKind.Unknown -> failwith $"Unsupported managed project '{entry.AbsolutePath}'."

    let private buildProjectNodes (entries: DotNetProjectEntry list) =
        entries
        |> List.map (fun entry ->
            let id = ProjectId.create entry.AbsolutePath

            ProjectNode.create
                id
                (toProjectKind entry)
                entry.AbsolutePath
                (ProjectCapabilityCatalog.forKind (toProjectKind entry)))

    let private buildProjectEdges (entries: DotNetProjectEntry list) =
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

    let private buildFileOwnership (entries: DotNetProjectEntry list) =
        entries
        |> List.collect (fun entry ->
            let owner = ProjectId.create entry.AbsolutePath

            readSourceFiles entry.AbsolutePath
            |> List.map (fun source -> source, owner))
        |> List.fold (fun acc (source, owner) -> Map.add source owner acc) Map.empty

    /// <summary>Parse slnx/sln/csproj/fsproj anchor into federation <c>SolutionGraph</c>.</summary>
    let load (anchorPath: string) : SolutionGraph =
        let parsed = DotNetWorkspace.Load anchorPath
        let entries = parsed.Projects |> Seq.toList

        let projects = buildProjectNodes entries
        let projectEdges = buildProjectEdges entries
        let ownership = buildFileOwnership entries

        SolutionGraph.create parsed.SolutionPath projects ownership [] projectEdges

    let loadSession (anchorPath: string) : SolutionSession =
        let graph = load anchorPath

        SolutionSession.create graph.AnchorPath graph
        |> SolutionSession.withPhase DesignTime
