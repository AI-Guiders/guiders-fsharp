module AIGuiders.Platform.Modeling.Configurations.Tests.ContractPredicatesTests

open System
open System.Collections.Generic
open System.IO
open Xunit
open AIGuiders.Platform.Modeling.Configurations
open AIGuiders.Platform.Modeling.Gdl.Parse.Config

let private emptyDocument () =
    { Name = "test"
      BasedOnAdr = None
      Defaults = Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
      Sources = Array.empty
      Contracts = Array.empty
      Facts = Array.empty }

let private writeToml (workspace: string) (personalRoot: string) =
    let content =
        $"""
[knowledge]
primary = "personal"

[knowledge.roots]
personal = "{personalRoot.Replace("\\", "/")}"
"""

    File.WriteAllText(Path.Combine(workspace, "agent-notes-mcp.toml"), content)

let private writeManifest (personalRoot: string) (l0Ids: string[]) =
    let metaDir = Path.Combine(personalRoot, "knowledge", "META")
    Directory.CreateDirectory metaDir |> ignore

    let body =
        l0Ids
        |> Array.map (fun id -> $"    \"{id}\"")
        |> String.concat ",\n"

    File.WriteAllText(
        Path.Combine(metaDir, "memory-architecture-v1.json"),
        $"{{\n  \"l0\": [\n{body}\n  ]\n}}\n"
    )

let private writeHotNotes (personalRoot: string) (sectionIds: string[]) =
    let blocks =
        sectionIds
        |> Array.map (fun id ->
            $"<!-- section:{id} -->\nbody {id}\n<!-- /section:{id} -->")

    let memoryArch =
        """<!-- section:memory-architecture-v1 -->
l0_manifest: knowledge/META/memory-architecture-v1.json
<!-- /section:memory-architecture-v1 -->"""

    let content = String.concat "\n\n" (Array.append blocks [| memoryArch |])
    File.WriteAllText(Path.Combine(personalRoot, "agent-notes.md"), content)

let private pilotDocument () =
    let sample =
        """
config cdp-newcomer

sources table
  | id           | kind              | path                                      | slice            |
  | personal-hot | markdown_sections | {personal}/agent-notes.md                 | above_public_cut |
  | l0-manifest  | json              | {personal}/knowledge/META/memory-architecture-v1.json | key:l0 |
"""

    let parse = ConfigParser.parseText sample

    match parse.Document with
    | Some doc -> doc
    | None -> failwith "pilot config parse failed"

let private buildWire (workspace: string) =
    let tomlPath = Path.Combine(workspace, "agent-notes-mcp.toml")

    match KnowledgeWire.tryBuildPersonalRootWire tomlPath (File.ReadLines tomlPath) with
    | Some wire -> wire
    | None -> failwith "personal root wire build failed"

let private readHotNotes (personalRoot: string) =
    File.ReadAllText(Path.Combine(personalRoot, "agent-notes.md"))

let private readManifest (personalRoot: string) =
    let path = Path.Combine(personalRoot, "knowledge", "META", "memory-architecture-v1.json")

    if File.Exists path then Some(File.ReadAllText path) else None

let private evaluate workspace personalRoot document =
    ContractPredicates.evaluateHotL0SectionsPresent
        (buildWire workspace)
        (readHotNotes personalRoot)
        (readManifest personalRoot)
        document

[<Fact>]
let ``hot_l0_sections_present passes when manifest l0 ids exist in agent-notes`` () =
    let workspace = Path.Combine(Path.GetTempPath(), "config-sat-" + Guid.NewGuid().ToString("N"))
    let personalRoot = Path.Combine(workspace, "personal")
    Directory.CreateDirectory personalRoot |> ignore
    writeToml workspace personalRoot
    writeManifest personalRoot [| "alpha-l0"; "beta-l0" |]
    writeHotNotes personalRoot [| "alpha-l0"; "beta-l0" |]

    let result = evaluate workspace personalRoot (emptyDocument ())

    Assert.True(result.Satisfied)
    Assert.Contains("hot_l0_sections_present", result.Note)

    try
        Directory.Delete(workspace, true)
    with _ ->
        ()

[<Fact>]
let ``hot_l0_sections_present fails when l0 section missing`` () =
    let workspace = Path.Combine(Path.GetTempPath(), "config-sat-" + Guid.NewGuid().ToString("N"))
    let personalRoot = Path.Combine(workspace, "personal")
    Directory.CreateDirectory personalRoot |> ignore
    writeToml workspace personalRoot
    writeManifest personalRoot [| "alpha-l0"; "beta-l0" |]
    writeHotNotes personalRoot [| "alpha-l0" |]

    let result = evaluate workspace personalRoot (emptyDocument ())

    Assert.False(result.Satisfied)
    Assert.Equal("config-l0-sections-missing", result.Diagnostic.Code)

    try
        Directory.Delete(workspace, true)
    with _ ->
        ()

[<Fact>]
let ``hot_l0_sections_present uses pilot sources table paths`` () =
    let workspace = Path.Combine(Path.GetTempPath(), "config-sat-" + Guid.NewGuid().ToString("N"))
    let personalRoot = Path.Combine(workspace, "personal")
    Directory.CreateDirectory personalRoot |> ignore
    writeToml workspace personalRoot
    writeManifest personalRoot [| "alpha-l0" |]
    writeHotNotes personalRoot [| "alpha-l0" |]

    let result = evaluate workspace personalRoot (pilotDocument ())

    Assert.True(result.Satisfied)

    try
        Directory.Delete(workspace, true)
    with _ ->
        ()

[<Fact>]
let ``hot_l0_sections_present ignores sections below public-cut`` () =
    let workspace = Path.Combine(Path.GetTempPath(), "config-sat-" + Guid.NewGuid().ToString("N"))
    let personalRoot = Path.Combine(workspace, "personal")
    Directory.CreateDirectory personalRoot |> ignore
    writeToml workspace personalRoot
    writeManifest personalRoot [| "alpha-l0" |]

    let notes =
        """<!-- section:alpha-l0 -->
above cut
<!-- /section:alpha-l0 -->
<!-- public-cut -->
<!-- section:shadow-l0 -->
below cut
<!-- /section:shadow-l0 -->
<!-- section:memory-architecture-v1 -->
l0_manifest: knowledge/META/memory-architecture-v1.json
<!-- /section:memory-architecture-v1 -->"""

    File.WriteAllText(Path.Combine(personalRoot, "agent-notes.md"), notes)
    writeManifest personalRoot [| "alpha-l0" |]

    let result = evaluate workspace personalRoot (pilotDocument ())

    Assert.True(result.Satisfied)

    try
        Directory.Delete(workspace, true)
    with _ ->
        ()
