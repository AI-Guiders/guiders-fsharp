module AIGuiders.Platform.Modeling.Gdl.Correspondence.Tests.GoldenEvidenceTests

open System
open System.IO
open Xunit
open AIGuiders.Platform.Modeling.Gdl.Correspondence

let private repoRoot =
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."))

[<Theory>]
[<InlineData("GS-OB1")>]
[<InlineData("GS-OB2")>]
[<InlineData("GS-OB3")>]
let ``locate finds ADR-0007 open-build golden stubs in guiders-fsharp`` (goldenId: string) =
    match GoldenEvidence.locate repoRoot goldenId with
    | Found found ->
        Assert.Contains("OpenBuildGoldenTests", found.Path)
        Assert.False(String.IsNullOrWhiteSpace found.Hint)
    | Missing -> failwith $"expected evidence for {goldenId} under {repoRoot}"

[<Fact>]
let ``locate returns Missing for unknown golden id`` () =
    match GoldenEvidence.locate repoRoot "GS-OB999" with
    | Missing -> ()
    | Found found -> failwith $"unexpected evidence at {found.Path}"

[<Fact>]
let ``exists is false for blank golden id`` () =
    Assert.False(GoldenEvidence.exists repoRoot "")
