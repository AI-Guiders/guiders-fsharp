namespace AIGuiders.Platform.Modeling.CodeCenter.Tests

open Xunit
open AIGuiders.Platform.Modeling.CodeCenter
open AIGuiders.Platform.Modeling.Core.Identity

module ReplayConformanceTests =

    [<Fact>]
    let ``V8b Extract undo via tryUndo restores pre-edit graph`` () =
        let session0 = ConformanceFixtures.createDemoSession()
        let tab =
            DocumentGraph.findNodeByName (ConformanceFixtures.rebuild session0.Text) "x"
            |> Option.defaultWith (fun () -> failwith "tab missing")
        let beforeText = session0.Text

        match session0.ApplyStructural(Extract(tab.Id, "pulled")) with
        | Error e -> Assert.Fail e
        | Ok(session1, entry) ->
            Assert.True(not (beforeText = session1.Text))
            Assert.Equal(InverseQuality.Partial, entry.InverseQuality)

            match session1.TryUndo() with
            | Error e -> Assert.Fail e
            | Ok session2 -> Assert.Equal(beforeText, session2.Text)

    [<Fact>]
    let ``V8c replayToRevision matches sequential apply`` () =
        let session0 = ConformanceFixtures.createDemoSession()
        let dash =
            DocumentGraph.findNodeByName (ConformanceFixtures.rebuild session0.Text) "demo"
            |> Option.defaultWith (fun () -> failwith "dash missing")

        let session1 =
            match session0.ApplyStructural(RenameMember(dash.Id, "dash1")) with
            | Ok(s, _) -> s
            | Error e -> Assert.Fail e; session0

        let tab =
            DocumentGraph.findNodeByName (ConformanceFixtures.rebuild session1.Text) "x"
            |> Option.defaultWith (fun () -> failwith "tab missing")

        let session2 =
            match session1.ApplyStructural(RenameMember(tab.Id, "tab1")) with
            | Ok(s, _) -> s
            | Error e -> Assert.Fail e; session1

        match session2.ReplayToRevision 1 with
        | Error e -> Assert.Fail e
        | Ok replayed -> Assert.Equal(session1.Text, replayed.Text)

    [<Fact>]
    let ``V8c replan replay path works for RenameMember without delta storage simulation`` () =
        let session0 = ConformanceFixtures.createDemoSession()
        let node = session0.TryResolve({ Offset = 5; TierHint = None }).Value

        match session0.ApplyStructural(RenameMember(node.NodeId.Value, "dashRenamed")) with
        | Ok(session1, entry) ->
            Assert.True(entry.Delta.IsSome)
            Assert.True(entry.PhiRef.IsSome)

            match session1.TryUndo() with
            | Ok undone -> Assert.Equal(ConformanceFixtures.sampleText, undone.Text)
            | Error e -> Assert.Fail e
        | Error e -> Assert.Fail e

    [<Fact>]
    let ``MoveMember compact inverse is Partial not replay result`` () =
        let session0 = ConformanceFixtures.createDemoSession()
        let tab =
            DocumentGraph.findNodeByName (ConformanceFixtures.rebuild session0.Text) "x"
            |> Option.defaultWith (fun () -> failwith "tab missing")

        let dash =
            DocumentGraph.findNodeByName (ConformanceFixtures.rebuild session0.Text) "demo"
            |> Option.defaultWith (fun () -> failwith "dash missing")

        match session0.ApplyStructural(MoveMember(tab.Id, dash.Id, 0)) with
        | Ok(_, entry) -> Assert.Equal(InverseQuality.Partial, entry.InverseQuality)
        | Error e -> Assert.Fail e
