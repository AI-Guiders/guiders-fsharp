namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet

open System
open System.IO
open AIGuiders.Platform.Modeling.Ide.Session
open DotNetWorkspace.Core

/// ISolutionInfoProvider over msbuild sources (slnx/sln/csproj/fsproj).
type MsBuildSolutionProvider(anchorPath: string) =

    let parsed () = DotNetWorkspace.Load anchorPath

    let entries () = parsed () |> fun graph -> graph.Projects |> Seq.toList

    let fingerprint () =
        let graph = parsed ()

        let latest =
            graph.Projects
            |> Seq.map (fun p -> File.GetLastWriteTimeUtc p.AbsolutePath)
            |> fun stamps ->
                if Seq.isEmpty stamps then DateTime.MinValue
                else Seq.max stamps

        $"{graph.SolutionPath}|projects={Seq.length graph.Projects}|{latest:o}"

    interface ISolutionInfoProvider with
        member _.Name = "msbuild"

        member _.Fingerprint() = fingerprint ()

        member _.Entries() =
            entries ()
            |> DotNetSlnxGraphPort.buildProjectNodes

        member _.Relations() =
            entries ()
            |> DotNetSlnxGraphPort.buildProjectEdges

/// Provider self-registration (ADR-0210 stage 1) — explicit composition-root init.
module Registration =
    /// Catalog name of this provider.
    let name = "msbuild"

    /// Registers this provider in the SolutionProviderRegistry (idempotent overwrite).
    let init () =
        SolutionProviderRegistry.register
            name
            (fun anchor -> MsBuildSolutionProvider(anchor) :> ISolutionInfoProvider)
