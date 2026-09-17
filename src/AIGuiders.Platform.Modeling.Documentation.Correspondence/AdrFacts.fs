namespace AIGuiders.Platform.Modeling.Documentation.Correspondence

open System
open System.Collections.Generic
open System.Text.RegularExpressions

/// <summary>
/// ADR machine-facing facts block (GUIDERS-FSHARP-ADR-0006 / 0007).
/// SSOT for parse IR; Execution Sat observers consume via thin C# bridge.
/// </summary>
type HoareObligation =
    { Id: string
      Precondition: string
      Transform: string
      Postcondition: string
      RawExpression: string }

type AdrVerifiedByRow = { Subject: string; Evidence: string }

type AdrFactsBlock =
    { SourcePath: string
      AdrId: string option
      GoldenIds: string array
      HoareObligations: HoareObligation array
      WellFormednessIds: string array
      VerifiedBy: AdrVerifiedByRow array }

[<RequireQualifiedAccess>]
module AdrFactsParser =
    type private FactsSection =
        | Outside
        | Golden
        | Hoare
        | WellFormedness
        | Table

    let private factsFenceRegex =
        Regex(
            @"```text\s*\r?\nfacts:\s*\r?\n(?<body>.*?)\r?\nend facts\s*\r?\n```",
            RegexOptions.Compiled ||| RegexOptions.IgnoreCase ||| RegexOptions.Singleline
        )

    let private inlineFactsRegex =
        Regex(
            @"\bfacts:\s*\r?\n(?<body>.*?)\r?\nend facts\b",
            RegexOptions.Compiled ||| RegexOptions.IgnoreCase ||| RegexOptions.Singleline
        )

    let private hoareLineRegex =
        Regex(
            @"^\s*-\s*(?<id>[A-Za-z0-9_-]+)\s*:\s*\{\s*(?<pre>.*?)\s*\}\s*(?<transform>.*?)\s*\{\s*(?<post>.*?)\s*\}\s*$",
            RegexOptions.Compiled
        )

    let private goldenInlineRegex =
        Regex(@"\bgolden:\s*(?<ids>[A-Za-z0-9_,\s*-]+)", RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

    let private goldenListItemRegex =
        Regex(@"^\s*-\s*(?<id>[A-Za-z0-9_-]+)\s*:", RegexOptions.Compiled)

    let private wfListItemRegex =
        Regex(@"^\s*-\s*(?<id>[A-Za-z0-9_-]+)\s*:", RegexOptions.Compiled)

    let private verifiedByRowRegex =
        Regex(@"^\s*\|\s*(?<subject>[^|]+?)\s*\|\s*(?<evidence>[^|]+?)\s*\|\s*$", RegexOptions.Compiled)

    let private adrIdRegex =
        Regex(@"GUIDERS-[A-Z0-9]+-ADR-\d{4}", RegexOptions.IgnoreCase)

    let private adrHeadingRegex =
        Regex(@"^#\s+(?<id>GUIDERS-[A-Z0-9]+-ADR-\d{4})\b", RegexOptions.IgnoreCase)

    let containsFactsBlock (markdown: string) =
        not (String.IsNullOrWhiteSpace markdown)
        && markdown.Contains("facts:", StringComparison.OrdinalIgnoreCase)
        && markdown.Contains("end facts", StringComparison.OrdinalIgnoreCase)

    let private tryExtractFactsBody (markdown: string) =
        let fenceMatch = factsFenceRegex.Match markdown

        if fenceMatch.Success then
            Some fenceMatch.Groups["body"].Value
        else
            let inlineMatch = inlineFactsRegex.Match markdown

            if inlineMatch.Success then
                Some inlineMatch.Groups["body"].Value
            else
                None

    let private addGoldenId (goldenIds: HashSet<string>) (id: string) =
        if not (String.IsNullOrWhiteSpace id) then
            goldenIds.Add id |> ignore

    let private addGoldenInline (line: string) (goldenIds: HashSet<string>) =
        let inlineMatch = goldenInlineRegex.Match line

        if inlineMatch.Success then
            for token in
                inlineMatch.Groups["ids"].Value.Split([| ','; ' ' |], StringSplitOptions.RemoveEmptyEntries) do
                let id = token.Trim()

                if id <> "*" && id <> "..*" then
                    addGoldenId goldenIds id

    let private addGoldenFromLine (line: string) (goldenIds: HashSet<string>) =
        let listMatch = goldenListItemRegex.Match line

        if listMatch.Success then
            addGoldenId goldenIds listMatch.Groups["id"].Value
        else
            addGoldenInline line goldenIds

    let private addHoareFromLine (line: string) (hoare: ResizeArray<HoareObligation>) =
        let m = hoareLineRegex.Match line

        if m.Success then
            hoare.Add(
                { Id = m.Groups["id"].Value
                  Precondition = m.Groups["pre"].Value.Trim()
                  Transform = m.Groups["transform"].Value.Trim()
                  Postcondition = m.Groups["post"].Value.Trim()
                  RawExpression = line.Trim() }
            )

    let private addWfFromLine (line: string) (wfIds: HashSet<string>) =
        let m = wfListItemRegex.Match line

        if m.Success then
            wfIds.Add m.Groups["id"].Value |> ignore

    let private addVerifiedByRow (line: string) (verifiedBy: ResizeArray<AdrVerifiedByRow>) (goldenIds: HashSet<string>) =
        if
            line.Contains("---", StringComparison.Ordinal)
            || line.Contains("contract", StringComparison.OrdinalIgnoreCase)
            || line.Contains("verified_by", StringComparison.OrdinalIgnoreCase)
        then
            ()
        else
            let m = verifiedByRowRegex.Match line

            if m.Success then
                let subject = m.Groups["subject"].Value.Trim()
                let evidence = m.Groups["evidence"].Value.Trim()
                verifiedBy.Add({ Subject = subject; Evidence = evidence }) |> ignore
                addGoldenInline evidence goldenIds

    let private tryEnterSection (line: string) (goldenIds: HashSet<string>) (section: FactsSection ref) =
        let trimmed = line.Trim()

        if
            trimmed.Equals("golden:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("golden:", StringComparison.OrdinalIgnoreCase)
        then
            section.Value <- Golden
            addGoldenInline line goldenIds
            true
        elif trimmed.Equals("hoare:", StringComparison.OrdinalIgnoreCase) then
            section.Value <- Hoare
            true
        elif trimmed.Equals("wf:", StringComparison.OrdinalIgnoreCase) then
            section.Value <- WellFormedness
            true
        elif trimmed.StartsWith("facts table", StringComparison.OrdinalIgnoreCase) then
            section.Value <- Table
            true
        else
            false

    let private tryExtractAdrId (sourcePath: string) (markdown: string) =
        let fileName =
            System.IO.Path.GetFileNameWithoutExtension sourcePath
            |> Option.ofObj
            |> Option.defaultValue ""

        if not (String.IsNullOrWhiteSpace fileName) then
            let fileMatch = adrIdRegex.Match fileName

            if fileMatch.Success then
                Some(fileMatch.Value.ToUpperInvariant())
            else
                let headingMatch = adrHeadingRegex.Match markdown

                if headingMatch.Success then
                    Some(headingMatch.Groups["id"].Value.ToUpperInvariant())
                else
                    None
        else
            let headingMatch = adrHeadingRegex.Match markdown

            if headingMatch.Success then
                Some(headingMatch.Groups["id"].Value.ToUpperInvariant())
            else
                None

    /// <summary>Parse ADR markdown facts block. Returns None when no facts fence/body found.</summary>
    let tryParse (sourcePath: string) (markdown: string | null) : AdrFactsBlock option =
        if String.IsNullOrWhiteSpace sourcePath then
            invalidArg "sourcePath" "Source path is required."

        if isNull markdown then
            nullArg "markdown"

        match tryExtractFactsBody markdown with
        | None -> None
        | Some body ->
            let goldenIds = HashSet<string>(StringComparer.OrdinalIgnoreCase)
            let hoare = ResizeArray<HoareObligation>()
            let wfIds = HashSet<string>(StringComparer.OrdinalIgnoreCase)
            let verifiedBy = ResizeArray<AdrVerifiedByRow>()
            let section = ref FactsSection.Outside

            for rawLine in body.Split('\n') do
                let line = rawLine.TrimEnd('\r')

                if not (String.IsNullOrWhiteSpace line) then
                    if not (tryEnterSection line goldenIds section) then
                        match section.Value with
                        | FactsSection.Golden -> addGoldenFromLine line goldenIds
                        | FactsSection.Hoare -> addHoareFromLine line hoare
                        | FactsSection.WellFormedness -> addWfFromLine line wfIds
                        | FactsSection.Table -> addVerifiedByRow line verifiedBy goldenIds
                        | FactsSection.Outside -> ()

            Some
                { SourcePath = sourcePath
                  AdrId = tryExtractAdrId sourcePath markdown
                  GoldenIds = goldenIds |> Seq.toArray
                  HoareObligations = hoare.ToArray()
                  WellFormednessIds = wfIds |> Seq.toArray
                  VerifiedBy = verifiedBy.ToArray() }
