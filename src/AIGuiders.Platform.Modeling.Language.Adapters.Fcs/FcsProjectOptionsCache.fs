namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Text.Json
open FSharp.Compiler.CodeAnalysis
open Ionide.ProjInfo
open Ionide.ProjInfo.FCS
open Microsoft.Build.Locator

[<CLIMutable>]
type FcsProjectOptionsWire =
    { ProjectFile: string
      OtherOptions: string[]
      SourceFiles: string[] }

module private MsBuildBootstrap =
    let gate = obj ()
    let mutable registered = false

    let ensure () =
        lock gate (fun () ->
            if not registered then
                try
                    MSBuildLocator.RegisterDefaults() |> ignore
                with :? InvalidOperationException ->
                    ()

                registered <- true)

module FcsProjectOptionsCache =
    let private checker = FSharpChecker.Create()

    let private cache =
        ConcurrentDictionary<string, FSharpProjectOptions>(StringComparer.OrdinalIgnoreCase)

    let private loadProjectOptionsInProcess (projectPath: string) =
        MsBuildBootstrap.ensure ()
        let projectDir = DirectoryInfo(Path.GetDirectoryName projectPath)
        let toolsPath = Init.init projectDir None
        let loader = WorkspaceLoader.Create(toolsPath, [])

        match loader.LoadProjects([ projectPath ]) |> Seq.tryHead with
        | None -> failwith $"Ionide.ProjInfo could not load '{projectPath}'."
        | Some projectOptions ->
            match FCS.mapManyOptions [ projectOptions ] |> Seq.tryHead with
            | None -> failwith $"Ionide.ProjInfo.FCS could not map '{projectPath}'."
            | Some fcsOptions -> fcsOptions

    let private resolveProbeHome () =
        let baseDir = AppContext.BaseDirectory

        let candidates =
            [| Path.Combine(baseDir, "tools", "FcsProjInfoProbe")
               Path.Combine(baseDir, "FcsProjInfoProbe")
               Path.Combine(baseDir, "..", "..", "..", "guiders-fsharp", "tools", "FcsProjInfoProbe", "bin", "Release", "net10.0")
               Path.Combine(baseDir, "..", "..", "..", "guiders-fsharp", "tools", "FcsProjInfoProbe", "bin", "Debug", "net10.0")
               Path.Combine(baseDir, "..", "..", "..", "..", "guiders-fsharp", "tools", "FcsProjInfoProbe", "bin", "Release", "net10.0")
               Path.Combine(baseDir, "..", "..", "..", "..", "guiders-fsharp", "tools", "FcsProjInfoProbe", "bin", "Debug", "net10.0")
               Path.Combine(baseDir, "..", "..", "..", "..", "..", "guiders-fsharp", "tools", "FcsProjInfoProbe", "bin", "Release", "net10.0")
               Path.Combine(baseDir, "..", "..", "..", "..", "..", "guiders-fsharp", "tools", "FcsProjInfoProbe", "bin", "Debug", "net10.0") |]

        candidates
        |> Array.choose (fun dir ->
            let full = Path.GetFullPath dir
            let dll = Path.Combine(full, "FcsProjInfoProbe.dll")

            if File.Exists dll then
                Some full
            else
                None)
        |> Array.tryHead

    let private loadProjectOptionsViaProbe (projectPath: string) =
        match resolveProbeHome () with
        | None -> failwith "FcsProjInfoProbe home directory not found."
        | Some probeHome ->
            let dll = Path.Combine(probeHome, "FcsProjInfoProbe.dll")

            let psi =
                ProcessStartInfo(
                    FileName = "dotnet",
                    Arguments = $"exec \"{dll}\" \"{projectPath}\"",
                    WorkingDirectory = probeHome,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                )

            use proc = Process.Start(psi)

            if isNull proc then
                failwith "Failed to start FcsProjInfoProbe."

            let stdout = proc.StandardOutput.ReadToEnd()
            let stderr = proc.StandardError.ReadToEnd()
            proc.WaitForExit()

            if proc.ExitCode <> 0 then
                failwith $"FcsProjInfoProbe failed ({proc.ExitCode}): {stderr}"

            let wire = JsonSerializer.Deserialize<FcsProjectOptionsWire>(stdout)

            if isNull wire then
                failwith "FcsProjInfoProbe returned empty payload."

            let baseOptions =
                checker.GetProjectOptionsFromCommandLineArgs(wire.ProjectFile, wire.OtherOptions)

            { baseOptions with
                SourceFiles = wire.SourceFiles }

    let private loadProjectOptions (projectPath: string) =
        try
            loadProjectOptionsInProcess projectPath
        with _ ->
            loadProjectOptionsViaProbe projectPath

    let tryGet (fsprojPath: string) =
        if String.IsNullOrWhiteSpace fsprojPath || not (File.Exists fsprojPath) then
            None
        else
            let key = Path.GetFullPath fsprojPath

            try
                Some(cache.GetOrAdd(key, loadProjectOptions))
            with _ ->
                None
