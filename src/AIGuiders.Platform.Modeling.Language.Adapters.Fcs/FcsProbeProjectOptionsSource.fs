namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.Diagnostics
open System.IO
open System.Collections.Concurrent
open System.Text.Json
open FSharp.Compiler.CodeAnalysis

[<CLIMutable>]
type FcsProbeWire =
    { ProjectFile: string
      OtherOptions: string[]
      SourceFiles: string[] }

/// <summary>Out-of-process F# project options via FcsProjInfoProbe child process.</summary>
/// <remarks>
/// Ionide WorkspaceLoader silently fails inside hosts that already hold MSBuild assemblies
/// (RoslynMcp WorkspaceHost registers its own Microsoft.Build). The probe runs in a clean
/// process — no in-process conflict. Wire: {ProjectFile, OtherOptions, SourceFiles} round-trips
/// into FSharpProjectOptions. Probe dll resolution: AIGUIDERS_FCS_PROBE env, then walk-up from
/// AppContext.BaseDirectory (test/service bins), then walk-up from the fsproj directory (repo).
/// </remarks>
type FcsProbeProjectOptionsSource() =

    static let cache = ConcurrentDictionary<string, DateTime * FSharpProjectOptions>()

    static let rec walkUp (dir: string) (acc: string list) =
        if String.IsNullOrEmpty dir then acc
        else
            let probeDir = Path.Combine(dir, "tools", "FcsProjInfoProbe")
            let found =
                if Directory.Exists probeDir then
                    Directory.EnumerateFiles(probeDir, "FcsProjInfoProbe.dll", SearchOption.AllDirectories)
                    |> List.ofSeq
                else []

            match Directory.GetParent dir with
            | null -> acc @ found
            | parent -> walkUp parent.FullName (acc @ found)

    static let probeDllCandidates (fsprojPath: string) =
        seq {
            match Environment.GetEnvironmentVariable "AIGUIDERS_FCS_PROBE" with
            | null
            | "" -> ()
            | p -> yield p

            match
                try
                    Some(AppContext.BaseDirectory)
                with _ ->
                    None
            with
            | Some baseDir ->
                yield Path.Combine(baseDir, "FcsProjInfoProbe", "FcsProjInfoProbe.dll")
                yield Path.Combine(baseDir, "FcsProjInfoProbe.dll")
                yield! walkUp baseDir []
            | None -> ()

            yield! walkUp (Path.GetDirectoryName (Path.GetFullPath fsprojPath)) []
        }

    static let resolveProbeDll (fsprojPath: string) =
        probeDllCandidates fsprojPath
        |> Seq.tryFind (fun p -> String.IsNullOrWhiteSpace p |> not && File.Exists p)
        |> Option.defaultWith (fun () ->
            failwithf "FcsProjInfoProbe.dll not found for '%s' (set AIGUIDERS_FCS_PROBE)" fsprojPath)

    static let runProbe (fsprojPath: string) : Result<FcsProbeWire, string> =
        try
            let dll = resolveProbeDll fsprojPath
            let psi = ProcessStartInfo()
            psi.FileName <- "dotnet"
            psi.Arguments <- $"exec \"{dll}\" \"{fsprojPath}\""
            psi.RedirectStandardOutput <- true
            psi.RedirectStandardError <- true
            psi.UseShellExecute <- false
            psi.CreateNoWindow <- true

            use proc = Process.Start psi

            if isNull (box proc) then
                Error $"FcsProjInfoProbe process failed to start for '{fsprojPath}'."
            else
                let stdout = proc.StandardOutput.ReadToEnd()
                let stderr = proc.StandardError.ReadToEnd()

                if not (proc.WaitForExit(120000)) then
                    try
                        proc.Kill()
                    with _ ->
                        ()

                    Error $"FcsProjInfoProbe timed out for '{fsprojPath}'."
                elif proc.ExitCode <> 0 then
                    Error $"FcsProjInfoProbe failed for '{fsprojPath}': {stderr.Trim()}"
                else
                    let wire = JsonSerializer.Deserialize<FcsProbeWire>(stdout)

                    if isNull (box wire) then
                        Error $"FcsProjInfoProbe returned empty wire for '{fsprojPath}'."
                    else
                        Ok wire
        with ex ->
            Error ex.Message

    static let toFcsOptions (wire: FcsProbeWire) : Result<FSharpProjectOptions, FcsProjectOptionsLoadError> =
        let options =
            { ProjectId = None
              ProjectFileName = wire.ProjectFile
              SourceFiles = wire.SourceFiles
              OtherOptions = wire.OtherOptions
              ReferencedProjects = [||]
              IsIncompleteTypeCheckEnvironment = true
              UseScriptResolutionRules = false
              LoadTime = DateTime.UtcNow
              UnresolvedReferences = None
              OriginalLoadReferences = []
              Stamp = None }

        FcsProjectOptionsGuards.requireFrameworkReferences options

    interface IFcsProjectOptionsSource with
        member _.TryLoad(fsprojPath: string) : Result<FSharpProjectOptions, FcsProjectOptionsLoadError> =
            try
                let full = Path.GetFullPath fsprojPath
                let mtime = File.GetLastWriteTimeUtc full

                match cache.TryGetValue full with
                | true, (seen, options) when seen = mtime -> Ok options
                | _ ->
                    match runProbe full with
                    | Error message ->
                        cache.TryRemove(full, ref (Unchecked.defaultof<_>, Unchecked.defaultof<_>)) |> ignore
                        Error { Message = message }
                    | Ok wire ->
                        match toFcsOptions wire with
                        | Ok options ->
                            cache[full] <- (mtime, options)
                            Ok options
                        | Error e -> Error e
            with ex ->
                Error { Message = ex.Message }

        member this.Warm fsprojPath = (this :> IFcsProjectOptionsSource).TryLoad fsprojPath |> ignore

        member _.Invalidate fsprojPath =
            match fsprojPath with
            | Some path -> cache.TryRemove(Path.GetFullPath path, ref (Unchecked.defaultof<_>, Unchecked.defaultof<_>)) |> ignore
            | None -> cache.Clear()
