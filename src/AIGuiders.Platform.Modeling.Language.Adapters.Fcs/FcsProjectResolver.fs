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

    /// <summary>
    /// Walk up from the file's directory to the nearest .fsproj (O(depth), not O(solution)).
    /// Checks that the found .fsproj references the file (via Compile Include match).
    /// Standard F# tooling pattern (Ionide / FsAutoComplete).
    /// </summary>
    let private tryWalkUpToFsproj (filePath: string) : string option =
        let full = normalizePath filePath
        let fileName = Path.GetFileName full
        let startDir =
            let dir = Path.GetDirectoryName full
            if String.IsNullOrWhiteSpace dir then full else dir

        let rec walk (dir: string) : string option =
            if String.IsNullOrWhiteSpace dir || not (Directory.Exists dir) then
                None
            else
                let fsprojs =
                    try Directory.GetFiles(dir, "*.fsproj")
                    with _ -> [||]

                match fsprojs with
                | [| single |] -> Some single
                | multiple when multiple.Length > 1 ->
                    // Multiple fsprojs — prefer one that references the file
                    let fileNameNoExt = Path.GetFileNameWithoutExtension full
                    let matching =
                        multiple
                        |> Array.tryFind (fun fsproj ->
                            let projName = Path.GetFileNameWithoutExtension fsproj
                            projName.Equals(fileNameNoExt, StringComparison.OrdinalIgnoreCase))
                    matching
                | _ ->
                    let parent = Directory.GetParent dir
                    if isNull parent then None else walk parent.FullName

        walk startDir

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

    /// Resolve owning fsproj: walk-up first (O(depth)), graph fallback only if walk-up fails.
    let resolveFsproj (filePath: string) (solutionOrProjectPath: string) =
        if String.IsNullOrWhiteSpace filePath then
            None
        elif
            String.IsNullOrWhiteSpace solutionOrProjectPath
            && not (hasDirectoryComponent filePath)
        then
            None
        else
            // Fast path: walk up from file to nearest .fsproj
            match tryWalkUpToFsproj filePath with
            | Some fsproj -> Some fsproj
            | None ->
                // Slow path: graph-based resolution (large solutions, unusual layouts)
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
