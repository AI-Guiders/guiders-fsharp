namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.IO
open DotNetWorkspace.Core

module FcsProjectResolver =
    let private hasDirectoryComponent (filePath: string) =
        not (String.IsNullOrWhiteSpace filePath)
        && not (String.IsNullOrWhiteSpace(Path.GetDirectoryName(filePath)))

    let resolveFsproj (filePath: string) (solutionOrProjectPath: string) =
        if String.IsNullOrWhiteSpace filePath then
            None
        elif
            String.IsNullOrWhiteSpace solutionOrProjectPath
            && not (hasDirectoryComponent filePath)
        then
            None
        else
            let hint =
                if String.IsNullOrWhiteSpace solutionOrProjectPath then
                    null
                else
                    solutionOrProjectPath

            match DotNetWorkspace.TryResolveOwningProject(filePath, hint, DotNetProjectKind.FSharp) with
            | null -> None
            | entry -> Some entry.AbsolutePath
