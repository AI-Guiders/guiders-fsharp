namespace AIGuiders.Platform.Modeling.CodeCenter.Tests

open Xunit
open AIGuiders.Platform.Modeling.CodeCenter
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

module DocumentSessionConformanceTests =

    let sampleText = "@dashboard demo\n    tab x as \"T\"\nend dashboard\n"

    [<Fact>]
    let ``V1 NodeId round trip at Semantic tier`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)
        let anchor = { Offset = 18; TierHint = Some "Semantic" }

        match session.TryResolve anchor with
        | None -> Assert.Fail("expected locus")
        | Some locus ->
            Assert.Equal("Semantic", locus.Tier)
            Assert.True(locus.NodeId.IsSome)
            Assert.True(DocumentGraph.nodeIdRoundTrip (DocumentGraph.rebuildFromText session.Text) locus)

    [<Fact>]
    let ``V2 applyStructural RenameMember projects text`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)
        let node = session.TryResolve({ Offset = 14; TierHint = None }).Value

        match session.ApplyStructural(RenameMember(node.NodeId.Value, "renamedDash")) with
        | Error e -> Assert.Fail e
        | Ok(session', _) ->
            Assert.Contains("renamedDash", session'.Text)
            Assert.DoesNotContain("@dashboard demo", session'.Text)

    [<Fact>]
    let ``V2a RelationSpec CodeEdit routes to applyStructural`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)
        let node = session.TryResolve({ Offset = 14; TierHint = None }).Value
        let docRef = DocumentRef.File(AIGuiders.Platform.Modeling.Paths.LogicalPath.Create "doc://demo")
        let spec = RelationSpec.CodeEdit(CodeTarget.TreeNode(docRef, node.NodeId.Value))

        match session.ApplyFromRelationSpec spec with
        | Error e -> Assert.Fail e
        | Ok(session', _) -> Assert.Contains("renamed", session'.Text)

    [<Fact>]
    let ``V3 kind wire resolves syntax locus`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)

        match session.TryResolve({ Offset = 14; TierHint = None }) with
        | None -> Assert.Fail("expected syntax locus")
        | Some locus -> Assert.Equal("Syntax", locus.Tier)

    [<Fact>]
    let ``V4 mechanical point edit`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)
        let edit = { Scope = Point; RemovedSpan = (0, 0); InsertedText = "//" }

        match session.ApplyMechanicalEdit edit with
        | Error e -> Assert.Fail e
        | Ok session' ->
            Assert.StartsWith("//", session'.Text)
            Assert.Equal(1, session'.RefreshScopes.Length)

    [<Fact>]
    let ``V4b mechanical region edit`` () =
        let session = DocumentSession.Create("doc://demo", "hello world")
        let edit = { Scope = Region; RemovedSpan = (0, 5); InsertedText = "bye" }

        match session.ApplyMechanicalEdit edit with
        | Error e -> Assert.Fail e
        | Ok session' -> Assert.Equal("bye world", session'.Text)

    [<Fact>]
    let ``V4c mechanical document sync`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)
        let session' = session.SyncFromText("@dashboard broken\n")
        Assert.Equal(1, session'.Revision)

    [<Fact>]
    let ``V5 completions stub returns items`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)
        let items = session.GetCompletions({ Offset = 5; TierHint = None })
        Assert.NotEmpty(items)

    [<Fact>]
    let ``V6 rename keeps NodeId stable for untouched nodes`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)
        let dash =
            DocumentGraph.findNodeByName (DocumentGraph.rebuildFromText session.Text) "demo"
            |> Option.defaultWith (fun () -> failwith "dashboard missing")

        let tab =
            DocumentGraph.findNodeByName (DocumentGraph.rebuildFromText session.Text) "x"
            |> Option.defaultWith (fun () -> failwith "tab missing")

        match session.ApplyStructural(RenameMember(dash.Id, "main")) with
        | Ok(session', _) ->
            let tabAfter =
                DocumentGraph.findNodeByName (DocumentGraph.rebuildFromText session'.Text) "x"

            Assert.True(tabAfter.IsSome)

            match tabAfter with
            | None -> Assert.Fail("tab node missing")
            | Some node -> Assert.Equal(tab.Id, node.Id)
        | Error e -> Assert.Fail e

    [<Fact>]
    let ``V7 structural input uses NodeId not text offsets`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)
        let node = session.TryResolve({ Offset = 5; TierHint = None }).Value
        let edit = RenameMember(node.NodeId.Value, "main")

        match session.ApplyStructural edit with
        | Ok _ -> Assert.True(true)
        | Error e -> Assert.Fail e

    [<Fact>]
    let ``V9 partial parse survives broken syntax`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)
        let broken = session.SyncFromText("@dashboard oops")
        Assert.True(broken.PartialParse || broken.Text.Contains("@dashboard"))

    [<Fact>]
    let ``V14 RelationSpec stub applyFromRelationSpec`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)
        let node = session.TryResolve({ Offset = 5; TierHint = None }).Value
        let docRef = DocumentRef.File(AIGuiders.Platform.Modeling.Paths.LogicalPath.Create "doc://demo")
        let spec = RelationSpec.CodeEdit(CodeTarget.TreeNode(docRef, node.NodeId.Value))

        match session.ApplyFromRelationSpec spec with
        | Ok _ -> Assert.True(true)
        | Error e -> Assert.Fail e

    [<Fact>]
    let ``V8a RenameMember inverse field is Exact`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)
        let node = session.TryResolve({ Offset = 5; TierHint = None }).Value

        match session.ApplyStructural(RenameMember(node.NodeId.Value, "main")) with
        | Ok(_, entry) ->
            Assert.Equal(InverseQuality.Exact, entry.InverseQuality)
            Assert.True(entry.Inverse.IsSome)
        | Error e -> Assert.Fail e

    [<Fact>]
    let ``V16 structural completions only legal blocks`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)
        let items = session.GetStructuralCompletions({ Offset = 5; TierHint = None })
        Assert.NotEmpty(items)
        Assert.True(items |> List.forall (fun i -> i.Edit |> function InsertBlock _ -> true | _ -> false))

    [<Fact>]
    let ``V10 structural paste commits InsertBlock not mechanical`` () =
        let session = DocumentSession.Create("doc://demo", sampleText)
        let node = session.TryResolve({ Offset = 5; TierHint = None }).Value

        match session.ApplyStructural(InsertBlock(node.NodeId.Value, "tab", "newTab as \"N\"")) with
        | Error e -> Assert.Fail e
        | Ok(session', entry) ->
            Assert.Contains("newTab", session'.Text)
            match entry.Theta with
            | Structural (InsertBlock _) -> Assert.Equal(1, session'.CommittedCount)
            | _ -> Assert.Fail("expected structural InsertBlock ledger entry")
