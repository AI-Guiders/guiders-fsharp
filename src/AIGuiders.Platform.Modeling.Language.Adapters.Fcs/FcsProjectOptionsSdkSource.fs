namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.IO
open DotNetWorkspace.Core
open FSharp.Compiler.CodeAnalysis

type SdkAssetsFcsProjectOptionsSource(?checker: FSharpChecker, ?loader: ISdkProjectContextLoader) =
    let checker = defaultArg checker (FSharpChecker.Create())
    let loader = defaultArg loader DotNetWorkspace.ProjectContext

    let toFcsOptions (ctx: SdkProjectContext) =
        let otherOptions =
            [|
                for reference in ctx.ReferenceAssemblies do
                    yield "-r:" + reference

                for define in ctx.DefineConstants do
                    yield "--define:" + define

                yield "--target:library"
                yield "--noframework"
                yield "--simpleresolution"
                yield "--targetprofile:netcore"
            |]

        let sourceFiles =
            if ctx.SourceFiles.Count = 0 then
                Directory.EnumerateFiles(ctx.ProjectDirectory, "*.fs", SearchOption.TopDirectoryOnly)
                |> Seq.map Path.GetFullPath
                |> Seq.toArray
            else
                ctx.SourceFiles |> Array.ofSeq

        let baseOptions =
            checker.GetProjectOptionsFromCommandLineArgs(ctx.ProjectPath, otherOptions)

        { baseOptions with
            SourceFiles = sourceFiles }

    interface IFcsProjectOptionsSource with
        member _.TryLoad projectPath =
            try
                let ctx = loader.Load(projectPath, WorkspaceProjectWarm.FSharpWarmOptions)
                Ok(toFcsOptions ctx)
            with ex ->
                Error { Message = ex.Message }

        member _.Warm projectPath =
            loader.Warm(projectPath, WorkspaceProjectWarm.FSharpWarmOptions)

        member _.Invalidate fsprojPath =
            match fsprojPath with
            | None -> loader.Invalidate()
            | Some path -> loader.Invalidate(path)
