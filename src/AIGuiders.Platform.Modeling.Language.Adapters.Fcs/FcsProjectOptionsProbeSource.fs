namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open FSharp.Compiler.CodeAnalysis

type ProbeFcsProjectOptionsSource(?checker: FSharpChecker) =
    let checker = defaultArg checker (FSharpChecker.Create())

    let resolveProbeHome () =
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

    let loadViaProbe projectPath =
        match resolveProbeHome () with
        | None ->
            Error
                { Message = "FcsProjInfoProbe home directory not found." }
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
                Error { Message = "Failed to start FcsProjInfoProbe." }
            else
                let stdout = proc.StandardOutput.ReadToEnd()
                let stderr = proc.StandardError.ReadToEnd()
                proc.WaitForExit()

                if proc.ExitCode <> 0 then
                    Error
                        { Message = $"FcsProjInfoProbe failed ({proc.ExitCode}): {stderr}" }
                else
                    let wire = JsonSerializer.Deserialize<FcsProjectOptionsWire>(stdout)

                    if isNull (box wire) then
                        Error { Message = "FcsProjInfoProbe returned empty payload." }
                    else
                        Ok(FcsProjectOptionsWire.toFcsOptions checker wire)

    interface IFcsProjectOptionsSource with
        member _.TryLoad projectPath = loadViaProbe projectPath
        member _.Warm projectPath = loadViaProbe projectPath |> ignore
        member _.Invalidate _ = ()
