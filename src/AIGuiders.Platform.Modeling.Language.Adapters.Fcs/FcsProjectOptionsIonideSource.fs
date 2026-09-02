namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System.IO
open Ionide.ProjInfo
open Ionide.ProjInfo.FCS
open Microsoft.Build.Locator

module private MsBuildBootstrap =
    let gate = obj ()
    let mutable registered = false

    let ensure () =
        lock gate (fun () ->
            if not registered then
                try
                    MSBuildLocator.RegisterDefaults() |> ignore
                with :? System.InvalidOperationException ->
                    ()

                registered <- true)

type IonideInProcessFcsProjectOptionsSource() =
    interface IFcsProjectOptionsSource with
        member _.TryLoad projectPath =
            try
                MsBuildBootstrap.ensure ()
                let projectDir = DirectoryInfo(Path.GetDirectoryName projectPath)
                let toolsPath = Init.init projectDir None
                let loader = WorkspaceLoader.Create(toolsPath, [])

                match loader.LoadProjects([ projectPath ]) |> Seq.tryHead with
                | None ->
                    Error
                        { Message = $"Ionide.ProjInfo could not load '{projectPath}'." }
                | Some projectOptions ->
                    match FCS.mapManyOptions [ projectOptions ] |> Seq.tryHead with
                    | None ->
                        Error
                            { Message = $"Ionide.ProjInfo.FCS could not map '{projectPath}'." }
                    | Some fcsOptions -> Ok fcsOptions
            with ex ->
                Error { Message = ex.Message }

        member _.Warm projectPath =
            (IonideInProcessFcsProjectOptionsSource() :> IFcsProjectOptionsSource).TryLoad projectPath
            |> ignore

        member _.Invalidate _ = ()
