namespace AIGuiders.Platform.Modeling.Documentation.Correspondence

open System
open System.IO
open System.Text.RegularExpressions

/// <summary>
/// Golden session test evidence discovery (GUIDERS-FSHARP-ADR-0006 / 0007).
/// SSOT for workspace scan; Sat observers consume via thin C# bridge.
/// </summary>
[<CLIMutable>]
type GoldenEvidenceFound = { Path: string; Hint: string }

type GoldenEvidenceResult =
    | Found of GoldenEvidenceFound
    | Missing

[<RequireQualifiedAccess>]
module GoldenEvidence =
    let private evidencePatterns = [| "*.fs"; "*.cs" |]

    let private goldenSessionAttributeRegex =
        Regex(
            @"\[GoldenSession\s*\(\s*""(?<id>[^""]+)""\s*\)\]",
            RegexOptions.Compiled ||| RegexOptions.IgnoreCase
        )

    let private goldenColonRegex =
        Regex(@"\bgolden:\s*(?<id>[A-Za-z0-9_-]+)", RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

    let private testMarkerRegex =
        Regex(
            @"\[(Fact|Theory|Test|TestMethod)\b|member\s|TestMethod\s|GoldenSession\b|golden:",
            RegexOptions.Compiled ||| RegexOptions.IgnoreCase
        )

    let private isExcludedPath (path: string) =
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
        || path.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)

    let private looksLikeTestFile (path: string) =
        path.Contains("Tests", StringComparison.OrdinalIgnoreCase)
        || path.Contains(".Test.", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("Tests.fs", StringComparison.OrdinalIgnoreCase)

    let private tryMatchGoldenSessionAttribute (content: string) (goldenId: string) =
        goldenSessionAttributeRegex.Matches content
        |> Seq.cast<Match>
        |> Seq.tryFind (fun m ->
            String.Equals(m.Groups["id"].Value, goldenId, StringComparison.OrdinalIgnoreCase))
        |> Option.map (fun _ -> "GoldenSession attribute")

    let private tryMatchGoldenColon (content: string) (goldenId: string) =
        match
            goldenColonRegex.Matches content
            |> Seq.cast<Match>
            |> Seq.tryFind (fun m ->
                String.Equals(m.Groups["id"].Value, goldenId, StringComparison.OrdinalIgnoreCase))
        with
        | Some _ -> Some "golden: marker"
        | None ->
            if content.Contains($"golden:{goldenId}", StringComparison.OrdinalIgnoreCase) then
                Some "golden: inline"
            elif content.Contains($"golden: {goldenId}", StringComparison.OrdinalIgnoreCase) then
                Some "golden: inline"
            else
                None

    let private tryMatchTestContext (content: string) (goldenId: string) =
        if not (content.Contains(goldenId, StringComparison.OrdinalIgnoreCase)) then
            None
        elif testMarkerRegex.IsMatch content then
            Some "test marker with golden id"
        else
            None

    let private tryMatchTestName (path: string) (goldenId: string) =
        match Path.GetFileNameWithoutExtension path with
        | null -> None
        | fileName when fileName.Contains(goldenId, StringComparison.OrdinalIgnoreCase) ->
            Some "test file name"
        | _ -> None

    let private tryDetectEvidence (path: string) (content: string) (goldenId: string) =
        [ tryMatchGoldenSessionAttribute content goldenId |> Option.map (fun hint -> hint, 3)
          tryMatchGoldenColon content goldenId |> Option.map (fun hint -> hint, 3)
          tryMatchTestName path goldenId |> Option.map (fun hint -> hint, 2)
          tryMatchTestContext content goldenId |> Option.map (fun hint -> hint, 1) ]
        |> List.choose id
        |> List.sortByDescending snd
        |> List.tryHead
        |> Option.map (fun (hint, strength) -> strength, { Path = path; Hint = hint })

    let private enumerateEvidenceFiles (workspaceRoot: string) =
        seq {
            if Directory.Exists workspaceRoot then
                for pattern in evidencePatterns do
                    for file in Directory.EnumerateFiles(workspaceRoot, pattern, SearchOption.AllDirectories) do
                        if not (isExcludedPath file) && looksLikeTestFile file then
                            yield file
        }

    /// <summary>Locate golden session evidence in workspace test files.</summary>
    let locate (workspaceRoot: string) (goldenId: string) : GoldenEvidenceResult =
        if String.IsNullOrWhiteSpace workspaceRoot || String.IsNullOrWhiteSpace goldenId then
            Missing
        else
            let root = Path.GetFullPath workspaceRoot

            enumerateEvidenceFiles root
            |> Seq.choose (fun file ->
                let content = File.ReadAllText file
                tryDetectEvidence file content goldenId)
            |> Seq.sortByDescending fst
            |> Seq.tryHead
            |> Option.map snd
            |> function
                | Some found -> Found found
                | None -> Missing

    /// <summary>True when <paramref name="goldenId"/> has discoverable test evidence.</summary>
    let exists (workspaceRoot: string) (goldenId: string) : bool =
        match locate workspaceRoot goldenId with
        | Found _ -> true
        | Missing -> false
