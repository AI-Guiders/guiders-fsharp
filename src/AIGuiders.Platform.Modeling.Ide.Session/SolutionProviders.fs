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

/// Plugin-style provider catalog (ADR-0210 stage 1): the core knows only
/// the contract + this registry; provider assemblies self-register.
module SolutionProviderRegistry =
    let private factories =
        System.Collections.Concurrent.ConcurrentDictionary<string, string -> ISolutionInfoProvider>()

    /// Register a provider factory keyed by provider name ("msbuild", "file-graph", ...).
    let register (name: string) (create: string -> ISolutionInfoProvider) =
        factories.[name] <- create

    /// Registered provider names, sorted (capability snapshot base).
    let names () : string list =
        factories.Keys |> Seq.sort |> Seq.toList

    /// Instantiate a provider by name for the given anchor.
    let create (name: string) (anchor: string) : ISolutionInfoProvider option =
        match factories.TryGetValue name with
        | true, make -> Some(make anchor)
        | _ -> None

    /// Instantiate every registered provider for the given anchor.
    let createAll (anchor: string) : ISolutionInfoProvider list =
        factories
        |> Seq.map (fun kv -> kv.Value anchor)
        |> Seq.toList

    /// What can this host feed the Solution Center? (Forge capabilities parity.)
    let capabilities (anchor: string) : (string * string) list =
        createAll anchor |> List.map (fun p -> p.Name, p.Fingerprint ())
