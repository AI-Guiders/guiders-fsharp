namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.IO
open DotNetWorkspace.Core
open Ionide.ProjInfo
open Ionide.ProjInfo.FCS
open FSharp.Compiler.CodeAnalysis

/// <summary>MSBuild design-time F# project options (Ionide.ProjInfo port — §7.3 buildDriver family).</summary>
type FcsProjInfoProjectOptionsSource(?checker: FSharpChecker) =
    let checker = defaultArg checker (FSharpChecker.Create())

    let loadOptions (projectPath: string) =
        MsBuildLocatorOnce.EnsureRegistered()
        let full = Path.GetFullPath projectPath
        let projectDir = DirectoryInfo(Path.GetDirectoryName full)
        let toolsPath = Init.init projectDir None
        let loader = WorkspaceLoader.Create(toolsPath, [])

        match loader.LoadProjects([ full ]) |> Seq.tryHead with
        | None -> Error { Message = $"Ionide.ProjInfo could not load '{full}'." }
        | Some projectOptions ->
            match FCS.mapManyOptions [ projectOptions ] |> Seq.tryHead with
            | None -> Error { Message = $"Ionide.ProjInfo could not map FCS options for '{full}'." }
            | Some fcsOptions -> FcsProjectOptionsGuards.requireFrameworkReferences fcsOptions

    interface IFcsProjectOptionsSource with
        member _.TryLoad projectPath =
            try
                loadOptions projectPath
            with ex ->
                Error { Message = ex.Message }

        member _.Warm projectPath =
            try
                loadOptions projectPath |> ignore
            with _ ->
                ()

        member _.Invalidate _ = ()
