namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.Workspace

open System
open System.IO
open System.Text.RegularExpressions
open AIGuiders.Platform.Modeling.Ide.Session

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

    let files (root: string) =
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        |> Seq.filter isDocument
        |> Seq.map Path.GetFullPath
        |> Seq.toList

    let private externalRef (raw: string) =
        raw.StartsWith("http://")
        || raw.StartsWith("https://")
        || raw.StartsWith("mailto:")
        || raw.StartsWith("#")

    /// Navigation links from one document to its siblings (physical layer enrichment).
    let links (root: string) : WorkspaceLink list =
        let docs = files root
        let known = Set.ofList docs

        docs
        |> List.collect (fun fromPath ->
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
            |> Seq.filter known.Contains
            |> Seq.distinct
            |> Seq.map (fun target -> { FromPath = fromPath; ToPath = target })
            |> Seq.toList)

/// Physical layer port: document tree + navigation links (no solution semantics).
module WorkspaceGraphPort =
    let fingerprint (root: string) =
        let docs = WorkspaceLinks.files root

        let latest =
            match docs |> List.map File.GetLastWriteTimeUtc with
            | [] -> DateTime.MinValue
            | stamps -> List.max stamps

        $"{root}|docs={List.length docs}|{latest:o}"

    let load (root: string) : WorkspaceGraph =
        WorkspaceGraph.create root (WorkspaceLinks.files root) (WorkspaceLinks.links root)
