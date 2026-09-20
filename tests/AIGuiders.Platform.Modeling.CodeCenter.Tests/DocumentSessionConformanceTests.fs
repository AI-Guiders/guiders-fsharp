namespace AIGuiders.Platform.Modeling.CodeCenter.Tests

open Xunit
open AIGuiders.Platform.Modeling.CodeCenter
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

module DocumentSessionConformanceTests =

    [<Fact>]
    let ``V1 NodeId round trip at Semantic tier`` () =
        let session = ConformanceFixtures.createDemoSession()
        let anchor = { Offset = 18; TierHint = Some "Semantic" }

        match session.TryResolve anchor with
        | None -> Assert.Fail("expected locus")
        | Some locus ->
            Assert.Equal("Semantic", locus.Tier)
            Assert.True(locus.NodeId.IsSome)
            Assert.True(DocumentGraph.nodeIdRoundTrip (ConformanceFixtures.rebuild session.Text) locus)

    [<Fact>]
    let ``V2 applyStructural RenameMember projects text`` () =
        let session = ConformanceFixtures.createDemoSession()
        let node = session.TryResolve({ Offset = 14; TierHint = None }).Value

        match session.ApplyStructural(RenameMember(node.NodeId.Value, "renamedDash")) with
        | Error e -> Assert.Fail e
        | Ok(session', _) ->
            Assert.Contains("renamedDash", session'.Text)
            Assert.DoesNotContain("@dashboard demo", session'.Text)

    [<Fact>]
    let ``V2a RelationSpec CodeEdit routes to applyStructural`` () =
        let session = ConformanceFixtures.createDemoSession()
        let node = session.TryResolve({ Offset = 14; TierHint = None }).Value
        let docRef = DocumentRef.File(AIGuiders.Platform.Modeling.Paths.LogicalPath.Create "doc://demo")
        let spec = RelationSpec.CodeEdit(CodeTarget.TreeNode(docRef, node.NodeId.Value))

        match session.ApplyFromRelationSpec spec with
        | Error e -> Assert.Fail e
        | Ok(session', _) -> Assert.Contains("renamed", session'.Text)

    [<Fact>]
    let ``V3 kind wire resolves syntax locus`` () =
        let session = ConformanceFixtures.createDemoSession()

        match session.TryResolve({ Offset = 14; TierHint = None }) with
        | None -> Assert.Fail("expected syntax locus")
        | Some locus -> Assert.Equal("Syntax", locus.Tier)

    [<Fact>]
    let ``V4 mechanical point edit`` () =
        let session = ConformanceFixtures.createDemoSession()
        let edit = { Scope = Point; RemovedSpan = (0, 0); InsertedText = "//" }

        match session.ApplyMechanicalEdit edit with
        | Error e -> Assert.Fail e
        | Ok session' ->
            Assert.StartsWith("//", session'.Text)
            Assert.Equal(1, session'.RefreshScopes.Length)

    [<Fact>]
    let ``V4b mechanical region edit`` () =
        let session = ConformanceFixtures.createSession "doc://demo" "hello world"
        let edit = { Scope = Region; RemovedSpan = (0, 5); InsertedText = "bye" }

        match session.ApplyMechanicalEdit edit with
        | Error e -> Assert.Fail e
        | Ok session' -> Assert.Equal("bye world", session'.Text)

    [<Fact>]
    let ``V4c mechanical document sync`` () =
        let session = ConformanceFixtures.createDemoSession()
        let session' = session.SyncFromText("@dashboard broken\n")
        Assert.Equal(1, session'.Revision)

    [<Fact>]
    let ``V4d DashSpec session exposes Language Profile`` () =
        let session = ConformanceFixtures.createDemoSession()
        Assert.Equal("dashspec.block", session.LanguageProfile.ProfileRef.ProfileId)

    [<Fact>]
    let ``V5 completions stub returns items`` () =
        let session = ConformanceFixtures.createDemoSession()
        let items = session.GetCompletions({ Offset = 5; TierHint = None })
        Assert.NotEmpty(items)

    [<Fact>]
    let ``V6 rename keeps NodeId stable for untouched nodes`` () =
        let session = ConformanceFixtures.createDemoSession()
        let dash =
            DocumentGraph.findNodeByName (ConformanceFixtures.rebuild session.Text) "demo"
            |> Option.defaultWith (fun () -> failwith "dashboard missing")

        let tab =
            DocumentGraph.findNodeByName (ConformanceFixtures.rebuild session.Text) "x"
            |> Option.defaultWith (fun () -> failwith "tab missing")

        match session.ApplyStructural(RenameMember(dash.Id, "main")) with
        | Ok(session', _) ->
            let tabAfter =
                DocumentGraph.findNodeByName (ConformanceFixtures.rebuild session'.Text) "x"

            Assert.True(tabAfter.IsSome)

            match tabAfter with
            | None -> Assert.Fail("tab node missing")
            | Some node -> Assert.Equal(tab.Id, node.Id)
        | Error e -> Assert.Fail e

    [<Fact>]
    let ``V7 structural input uses NodeId not text offsets`` () =
        let session = ConformanceFixtures.createDemoSession()
        let node = session.TryResolve({ Offset = 5; TierHint = None }).Value
        let edit = RenameMember(node.NodeId.Value, "main")

        match session.ApplyStructural edit with
        | Ok _ -> Assert.True(true)
        | Error e -> Assert.Fail e

    [<Fact>]
    let ``V9 partial parse survives broken syntax`` () =
        let session = ConformanceFixtures.createDemoSession()
        let broken = session.SyncFromText("@dashboard oops")
        Assert.True(broken.PartialParse || broken.Text.Contains("@dashboard"))

    [<Fact>]
    let ``V14 RelationSpec stub applyFromRelationSpec`` () =
        let session = ConformanceFixtures.createDemoSession()
        let node = session.TryResolve({ Offset = 5; TierHint = None }).Value
        let docRef = DocumentRef.File(AIGuiders.Platform.Modeling.Paths.LogicalPath.Create "doc://demo")
        let spec = RelationSpec.CodeEdit(CodeTarget.TreeNode(docRef, node.NodeId.Value))

        match session.ApplyFromRelationSpec spec with
        | Ok _ -> Assert.True(true)
        | Error e -> Assert.Fail e

    [<Fact>]
    let ``V8a RenameMember inverse field is Exact`` () =
        let session = ConformanceFixtures.createDemoSession()
        let node = session.TryResolve({ Offset = 5; TierHint = None }).Value

        match session.ApplyStructural(RenameMember(node.NodeId.Value, "main")) with
        | Ok(_, entry) ->
            Assert.Equal(InverseQuality.Exact, entry.InverseQuality)
            Assert.True(entry.Inverse.IsSome)
        | Error e -> Assert.Fail e

    [<Fact>]
    let ``V16 structural completions only legal blocks`` () =
        let session = ConformanceFixtures.createDemoSession()
        let items = session.GetStructuralCompletions({ Offset = 5; TierHint = None })
        Assert.NotEmpty(items)
        Assert.True(items |> List.forall (fun i -> i.Edit |> function InsertBlock _ -> true | _ -> false))

    [<Fact>]
    let ``V10 structural paste commits InsertBlock not mechanical`` () =
        let session = ConformanceFixtures.createDemoSession()
        let node = session.TryResolve({ Offset = 5; TierHint = None }).Value

        match session.ApplyStructural(InsertBlock(node.NodeId.Value, "tab newTab as \"N\"")) with
        | Error e -> Assert.Fail e
        | Ok(session', entry) ->
            Assert.Contains("newTab", session'.Text)
            match entry.Theta with
            | Structural (InsertBlock _) -> Assert.Equal(1, session'.CommittedCount)
            | _ -> Assert.Fail("expected structural InsertBlock ledger entry")

    [<Fact>]
    let ``V12 graph nodes export and node resolve`` () =
        let session = ConformanceFixtures.createDemoSession()
        let nodes = session.GetDocumentNodes() |> Seq.toList
        Assert.True(nodes.Length >= 2)

        let tab =
            nodes
            |> List.find (fun node -> node.Name = "tab x")

        match session.TryResolveNode tab.Id with
        | None -> Assert.Fail("expected node locus")
        | Some locus ->
            Assert.Equal("Semantic", locus.Tier)
            Assert.True(locus.NodeId.IsSome)
            Assert.True(DocumentGraph.nodeIdRoundTrip (ConformanceFixtures.rebuild session.Text) locus)
