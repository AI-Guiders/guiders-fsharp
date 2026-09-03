namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.IO
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet
open DotNetWorkspace.Core

module FcsProjectResolver =
    let private hasDirectoryComponent (filePath: string) =
        not (String.IsNullOrWhiteSpace filePath)
        && not (String.IsNullOrWhiteSpace(Path.GetDirectoryName(filePath)))

    let private normalizePath path = Path.GetFullPath path

    let private tryOwnerProjectPath (graph: SolutionGraph) (filePath: string) =
        let full = normalizePath filePath

        let ownerId =
            match Map.tryFind full graph.FileOwnership with
            | Some id -> Some id
            | None ->
                graph.FileOwnership
                |> Map.tryPick (fun ownedPath owner ->
                    if String.Equals(normalizePath ownedPath, full, StringComparison.OrdinalIgnoreCase) then
                        Some owner
                    else
                        None)

        ownerId
        |> Option.bind (fun id -> SolutionGraph.tryFindProject id graph)
        |> Option.map (fun project -> project.AbsolutePath)

    let private tryResolveFromGraph (filePath: string) (anchorPath: string) =
        if not (File.Exists anchorPath) then
            None
        else
            try
                let graph = DotNetSlnxGraphPort.load anchorPath
                tryOwnerProjectPath graph filePath
            with _ ->
                None

    /// Resolve owning fsproj via federation ω (FileOwnership) when anchor is known; port fallback otherwise.
    let resolveFsproj (filePath: string) (solutionOrProjectPath: string) =
        if String.IsNullOrWhiteSpace filePath then
            None
        elif
            String.IsNullOrWhiteSpace solutionOrProjectPath
            && not (hasDirectoryComponent filePath)
        then
            None
        else
            match
                if String.IsNullOrWhiteSpace solutionOrProjectPath then
                    None
                else
                    tryResolveFromGraph filePath solutionOrProjectPath
            with
            | Some fsproj -> Some fsproj
            | None ->
                let hint =
                    if String.IsNullOrWhiteSpace solutionOrProjectPath then
                        null
                    else
                        solutionOrProjectPath

                match DotNetWorkspace.TryResolveOwningProject(filePath, hint, DotNetProjectKind.FSharp) with
                | null -> None
                | entry -> Some entry.AbsolutePath
