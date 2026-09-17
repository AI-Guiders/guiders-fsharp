module AIGuiders.Platform.Modeling.Documentation.Correspondence.Tests.AdrFactsParserTests

open System
open System.IO
open Xunit
open AIGuiders.Platform.Modeling.Documentation.Correspondence

let private sample0007 =
    """# GUIDERS-FSHARP-ADR-0007: Open build SSOT

```text
facts:
  golden:
    - GS-OB1: FCS diagnostics subset
    - GS-OB2: WorkspaceView materializes
    - GS-OB3: SdkAssets fallback
  hoare:
    - OB-H1: { graph revision = r } materialize(π) { Sat(Q_compile) }
    - OB-H2: { Sat(Q_compile) } emit(workspace, target) { artifacts compatible }
  wf:
    - WF-OB1: SSOT for design-time project truth is FTC graph
    - WF-OB2: MSBuild is optional port
end facts
prose:
```

## Context
"""

[<Fact>]
let ``containsFactsBlock detects fenced facts`` () =
    Assert.True(AdrFactsParser.containsFactsBlock sample0007)

[<Fact>]
let ``tryParse extracts nested golden hoare wf from FSHARP-0007 shape`` () =
    match AdrFactsParser.tryParse "docs/adr/GUIDERS-FSHARP-ADR-0007-open-build.md" sample0007 with
    | None -> failwith "expected facts block"
    | Some facts ->
        Assert.Equal("GUIDERS-FSHARP-ADR-0007", facts.AdrId.Value)
        Assert.Equal<string list>([ "GS-OB1"; "GS-OB2"; "GS-OB3" ], facts.GoldenIds |> Array.toList)
        Assert.Equal(2, facts.HoareObligations.Length)
        Assert.Equal("OB-H1", facts.HoareObligations.[0].Id)
        Assert.Equal<string list>([ "WF-OB1"; "WF-OB2" ], facts.WellFormednessIds |> Array.toList)

[<Fact>]
let ``tryParse reads golden from inline golden: GS1, GS2`` () =
    let md =
        """
facts:
golden: GS1, GS2
hoare:
- H1: { P } G { Q }
end facts
"""

    match AdrFactsParser.tryParse "x.md" md with
    | None -> failwith "expected facts"
    | Some facts -> Assert.Equal<string list>([ "GS1"; "GS2" ], facts.GoldenIds |> Array.toList)

[<Fact>]
let ``tryParse round-trips real FSHARP-0007 adr file when present`` () =
    let repoRoot =
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")
        )

    let adrPath =
        Path.Combine(
            repoRoot,
            "docs",
            "adr",
            "GUIDERS-FSHARP-ADR-0007-open-build-ssot-ftc-correspondence.md"
        )

    if File.Exists adrPath then
        let md = File.ReadAllText adrPath
        match AdrFactsParser.tryParse adrPath md with
        | None -> failwith "expected facts in canonical ADR-0007"
        | Some facts ->
            Assert.Equal(3, facts.GoldenIds.Length)
            Assert.Equal(3, facts.HoareObligations.Length)
            Assert.Equal(2, facts.WellFormednessIds.Length)
