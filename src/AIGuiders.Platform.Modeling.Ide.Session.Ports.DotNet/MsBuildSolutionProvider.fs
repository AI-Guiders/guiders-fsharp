namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet

open AIGuiders.Platform.Modeling.Ide.Session

/// ISolutionInfoProvider over pre-loaded msbuild topology (IO @ Execution sources).
type MsBuildSolutionProvider(topology: DotNetSolutionTopology) =

    interface ISolutionInfoProvider with
        member _.Name = "msbuild"

        member _.Fingerprint() = topology.Fingerprint

        member _.Entries() =
            topology.Rows
            |> List.map (fun row -> row.Entry)
            |> DotNetSlnxGraphPort.buildProjectNodes

        member _.Relations() = DotNetSlnxGraphPort.buildProjectRefRelations topology.Rows

module MsBuildSolutionProvider =
    let create (topology: DotNetSolutionTopology) = MsBuildSolutionProvider topology

/// Provider catalog name (composition-root init lives @ Execution).
module Registration =
    let name = "msbuild"
