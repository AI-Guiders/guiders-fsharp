namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs.Probe

open System
open System.IO
open System.Text.Json
open Ionide.ProjInfo
open Ionide.ProjInfo.FCS
open Microsoft.Build.Locator

[<CLIMutable>]
type FcsProjectOptionsWire =
    { ProjectFile: string
      OtherOptions: string[]
      SourceFiles: string[] }

module Program =
    let loadOptions (projectPath: string) =
        try
            MSBuildLocator.RegisterDefaults() |> ignore
        with :? InvalidOperationException ->
            ()

        let projectDir = DirectoryInfo(Path.GetDirectoryName projectPath)
        let toolsPath = Init.init projectDir None
        let loader = WorkspaceLoader.Create(toolsPath, [])

        match loader.LoadProjects([ projectPath ]) |> Seq.tryHead with
        | None -> failwith $"Could not load '{projectPath}'."
        | Some projectOptions ->
            match FCS.mapManyOptions [ projectOptions ] |> Seq.tryHead with
            | None -> failwith $"Could not map '{projectPath}'."
            | Some fcsOptions ->
                { ProjectFile = projectPath
                  OtherOptions = fcsOptions.OtherOptions
                  SourceFiles = fcsOptions.SourceFiles }

    [<EntryPoint>]
    let main args =
        if args.Length = 0 then
            eprintfn "usage: FcsProjInfoProbe <path.fsproj>"
            2
        else
            try
                let wire = loadOptions (Path.GetFullPath args[0])
                let json = JsonSerializer.Serialize(wire)
                Console.Out.WriteLine(json)
                0
            with ex ->
                eprintfn "%s" ex.Message
                1
