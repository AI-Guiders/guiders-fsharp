namespace AIGuiders.Platform.Modeling.Ide.Session

/// Reader-port feeding the SolutionGraph IR from any source
/// (msbuild slnx/sln/csproj, file graph, ...).
/// Contract shape sealed by operator: { Name, Fingerprint, Entries, Relations }.
type ISolutionInfoProvider =
    /// Human-readable provider id ("msbuild", "file-graph").
    abstract Name: string

    /// Stable identity of the consumed source (path + revision token).
    abstract Fingerprint: unit -> string

    /// Graph vertices — project entries.
    abstract Entries: unit -> ProjectNode list

    /// Graph edges — project reference / membership relations.
    abstract Relations: unit -> ProjectEdge list

module SolutionProviders =
    /// File → owner map assembled from provider entries.
    let fileOwnership (entries: ProjectNode list) : Map<string, ProjectId> =
        entries |> List.map (fun p -> p.AbsolutePath, p.Id) |> Map.ofList

    /// Assemble the SolutionGraph from provider output (no semantic session edges).
    let toGraph (anchorPath: string) (provider: ISolutionInfoProvider) : SolutionGraph =
        let entries = provider.Entries ()

        SolutionGraph.create
            anchorPath
            entries
            (fileOwnership entries)
            []
            (provider.Relations ())
