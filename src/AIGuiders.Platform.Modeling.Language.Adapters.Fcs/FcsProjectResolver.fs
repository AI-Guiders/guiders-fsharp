namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.IO

module FcsProjectResolver =
    let private normalize (path: string) = Path.GetFullPath path

    let private fileBelongsToProject (filePath: string) (fsprojPath: string) =
        let full = normalize filePath
        let projDir = normalize (Path.GetDirectoryName fsprojPath)
        full.StartsWith(projDir + string Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)

    let rec private tryFindOwningFsproj (filePath: string) =
        let dir = Path.GetDirectoryName(filePath)

        if String.IsNullOrWhiteSpace dir then
            None
        else
            let hit =
                Directory.EnumerateFiles(dir, "*.fsproj")
                |> Seq.tryFind (fileBelongsToProject filePath)

            match hit with
            | Some proj -> Some(normalize proj)
            | None ->
                let parent = Path.GetDirectoryName dir

                if String.IsNullOrWhiteSpace parent || parent = dir then
                    None
                else
                    tryFindOwningFsproj (Path.Combine(parent, "_.fs"))

    let resolveFsproj (filePath: string) (solutionOrProjectPath: string) =
        if String.IsNullOrWhiteSpace filePath then
            None
        else
            let fromWalk = tryFindOwningFsproj filePath

            if String.IsNullOrWhiteSpace solutionOrProjectPath then
                fromWalk
            else
                let hint = normalize solutionOrProjectPath

                if hint.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase) then
                    Some hint
                elif
                    hint.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
                    || hint.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                then
                    fromWalk
                    |> Option.orElseWith (fun () ->
                        let root = Path.GetDirectoryName hint

                        if String.IsNullOrWhiteSpace root then
                            None
                        else
                            Directory.EnumerateFiles(root, "*.fsproj", SearchOption.AllDirectories)
                            |> Seq.tryFind (fileBelongsToProject filePath)
                            |> Option.map normalize)
                else
                    fromWalk
