namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.Workspace

open System
open System.IO
open System.Text.RegularExpressions
open AIGuiders.Platform.Modeling.Ide.Session

/// Snapshot row for pure workspace graph build (Execution reads disk).
type WorkspaceDocumentSnapshot =
    { Path: string
      Content: string
      LastWriteUtc: DateTime }

module WorkspaceLinks =
    /// Document extensions whose content is scanned for links.
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

    let private externalRef (raw: string) =
        raw.StartsWith("http://")
        || raw.StartsWith("https://")
        || raw.StartsWith("mailto:")
        || raw.StartsWith("#")

    /// Navigation links from pre-loaded document contents (physical layer enrichment).
    let linksFromSnapshots (documents: WorkspaceDocumentSnapshot list) : WorkspaceLink list =
        let known = documents |> List.map (fun d -> d.Path) |> Set.ofList

        documents
        |> List.collect (fun doc ->
            let dir = Path.GetDirectoryName doc.Path

            let matches =
                if Path.GetExtension(doc.Path).Equals(".md", StringComparison.OrdinalIgnoreCase) then
                    mdLink.Matches(doc.Content)
                else
                    pathLiteral.Matches(doc.Content)

            matches
            |> Seq.cast<Match>
            |> Seq.map (fun m -> m.Groups.[1].Value)
            |> Seq.filter (not << externalRef)
            |> Seq.map (fun raw ->
                Path.GetFullPath(Path.Combine(dir, raw.Replace('/', Path.DirectorySeparatorChar))))
            |> Seq.filter known.Contains
            |> Seq.distinct
            |> Seq.map (fun target -> { FromPath = doc.Path; ToPath = target })
            |> Seq.toList)

/// Physical layer port: pure graph from document snapshots (no File IO in Modeling).
module WorkspaceGraphPort =
    let build (root: string) (documents: WorkspaceDocumentSnapshot list) : WorkspaceGraph =
        let paths = documents |> List.map (fun d -> d.Path)
        let links = WorkspaceLinks.linksFromSnapshots documents
        WorkspaceGraph.create root paths links

    let fingerprint (root: string) (documents: WorkspaceDocumentSnapshot list) : string =
        let latest =
            match documents |> List.map (fun d -> d.LastWriteUtc) with
            | [] -> DateTime.MinValue
            | stamps -> List.max stamps

        $"{root}|docs={List.length documents}|{latest:o}"
