namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.Files

open System
open System.IO
open System.Text.RegularExpressions
open AIGuiders.Platform.Modeling.Ide.Session

module FileGraph =
    /// Document extensions recognized as graph units.
    let extensions = [| ".md"; ".json"; ".toml"; ".yaml"; ".yml" |]

    let isDocument (path: string) =
        Array.contains (Path.GetExtension(path).ToLowerInvariant()) extensions

    /// Inline markdown links: [text](target).
    let mdLink = Regex(@"\]\(([^)\s]+)\)", RegexOptions.Compiled)

    /// Quoted string literals ending with a document extension.
    let pathLiteral =
        Regex(
            "[\"']([^\"']+\\.(?:md|json|toml|yaml|yml))[\"']",
            RegexOptions.Compiled
            ||| RegexOptions.IgnoreCase
        )

    let documents (root: string) =
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        |> Seq.filter isDocument
        |> Seq.map Path.GetFullPath
        |> Seq.toList

    let kindOf (path: string) =
        Doc { Extension = Path.GetExtension(path).ToLowerInvariant() }

    let private externalRef (raw: string) =
        raw.StartsWith("http://")
        || raw.StartsWith("https://")
        || raw.StartsWith("mailto:")
        || raw.StartsWith("#")

    /// Targets referenced by a document, resolved against its directory,
    /// filtered to the known set of graph documents.
    let referenced (fromPath: string) (candidates: Set<string>) =
        let dir = Path.GetDirectoryName fromPath
        let text = File.ReadAllText fromPath

        let matches =
            if Path.GetExtension(fromPath).Equals(".md", StringComparison.OrdinalIgnoreCase) then
                mdLink.Matches(text)
            else
                pathLiteral.Matches(text)

        matches
        |> Seq.cast<Match>
        |> Seq.map (fun m -> m.Groups.[1].Value)
        |> Seq.filter (not << externalRef)
        |> Seq.map (fun raw ->
            Path.GetFullPath(Path.Combine(dir, raw.Replace('/', Path.DirectorySeparatorChar))))
        |> Seq.filter candidates.Contains
        |> Seq.distinct
        |> Seq.toList

    let fingerprint (root: string) =
        let docs = documents root

        let latest =
            match docs |> List.map File.GetLastWriteTimeUtc with
            | [] -> DateTime.MinValue
            | stamps -> List.max stamps

        $"{root}|docs={List.length docs}|{latest:o}"

    let entries (root: string) =
        documents root
        |> List.map (fun path ->
            ProjectNode.create (ProjectId.create path) (kindOf path) path [])

    let relations (root: string) =
        let docs = documents root
        let candidates = Set.ofList docs
        let byPath = docs |> List.map (fun d -> d, ProjectId.create d) |> Map.ofList

        docs
        |> List.collect (fun d ->
            referenced d candidates
            |> List.choose (fun target ->
                Map.tryFind target byPath
                |> Option.map (fun toId -> ProjectEdge.create (ProjectId.create d) toId)))

/// ISolutionInfoProvider over document trees (md/json/toml/yaml):
/// units = documents, edges = intra-tree references.
type FileGraphProvider(rootPath: string) =

    interface ISolutionInfoProvider with
        member _.Name = "file-graph"

        member _.Fingerprint() = FileGraph.fingerprint rootPath

        member _.Entries() = FileGraph.entries rootPath

        member _.Relations() = FileGraph.relations rootPath

/// Provider self-registration (ADR-0210 stage 1) — explicit composition-root init.
module Registration =
    /// Catalog name of this provider.
    let name = "file-graph"

    /// Registers this provider in the SolutionProviderRegistry (idempotent overwrite).
    let init () =
        SolutionProviderRegistry.register
            name
            (fun anchor -> FileGraphProvider(anchor) :> ISolutionInfoProvider)
