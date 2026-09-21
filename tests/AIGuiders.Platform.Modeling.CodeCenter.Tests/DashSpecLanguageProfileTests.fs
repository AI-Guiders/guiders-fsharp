namespace AIGuiders.Platform.Modeling.CodeCenter.Tests

open Xunit
open AIGuiders.Platform.Modeling.CodeCenter
open DashSpec.Modeling.CodeCenter
open DashSpec.Modeling.Parse.Syntax

module DashSpecLanguageProfileTests =

    let private sample =
        "@dashboard demo\n    tab x as \"T\"\n    card events_detail\n        data\n        end data\n    end card events_detail\nend dashboard\n"

    [<Fact>]
    let ``concept graph uses stable ast ids and contains edges`` () =
        let graph = DashSpecConceptGraphBuilder.buildFromText sample

        Assert.True(graph.Nodes.Count >= 4)
        Assert.Contains(graph.Nodes, fun pair -> pair.Key > 0u)
        Assert.True(graph.Edges.Length >= 3)

        let tab =
            graph.Nodes
            |> Seq.choose (fun pair ->
                match pair.Value.Kind with
                | DashSpecConceptKind.Block(DashSpecBlockKeyword.Tab, Some "x") -> Some pair.Value
                | _ -> None)
            |> Seq.head

        Assert.Equal(DashSpecProjectionRole.Diagram, tab.ProjectionRole)

    [<Fact>]
    let ``invariant laws pass on valid dashboard sample`` () =
        let graph = DashSpecConceptGraphBuilder.buildFromText sample
        let errors =
            DashSpecInvariantLaws.all graph
            |> List.filter (fun diagnostic -> diagnostic.Severity = "error")

        Assert.Empty(errors)

    [<Fact>]
    let ``serialize round trip preserves outline signature`` () =
        match DashSpecSerializeRules.roundTripOutline sample with
        | Ok _ -> ()
        | Error diagnostics -> Assert.Fail(System.String.Join("; ", diagnostics |> List.map (fun d -> d.Message)))

    [<Fact>]
    let ``federation node ids match ast ids`` () =
        let snapshot, graph, _ = DashSpecProfileRebuild.rebuild sample
        let tabConcept =
            graph.Nodes
            |> Seq.pick (fun pair ->
                match pair.Value.Kind with
                | DashSpecConceptKind.Block(DashSpecBlockKeyword.Tab, Some "x") -> Some pair.Value
                | _ -> None)

        let expected = DashSpecProfileRebuild.nodeIdFromAst tabConcept.AstId

        Assert.True(Map.containsKey expected snapshot.Nodes)
        Assert.Equal("tab x", snapshot.Nodes.[expected].Name)

    [<Fact>]
    let ``projection hints classify data block as form field`` () =
        let graph = DashSpecConceptGraphBuilder.buildFromText sample
        let formNodes = DashSpecProjectionHints.formFieldNodes graph

        Assert.Contains(formNodes, fun node ->
            match node.Kind with
            | DashSpecConceptKind.Block(DashSpecBlockKeyword.Data, _) -> true
            | _ -> false)

    [<Fact>]
    let ``extra end block produces DS002`` () =
        let graph = DashSpecConceptGraphBuilder.buildFromText "end dashboard\n"
        let diagnostics = DashSpecRuleEngine.evaluateBlockBalance graph

        Assert.Contains(diagnostics, fun diagnostic -> diagnostic.Code = "DS002")

    [<Fact>]
    let ``unbalanced end block produces DS003`` () =
        let graph = DashSpecConceptGraphBuilder.buildFromText "@dashboard demo\nend tab\nend dashboard\n"
        let diagnostics = DashSpecRuleEngine.evaluateBlockBalance graph

        Assert.Contains(diagnostics, fun diagnostic -> diagnostic.Code = "DS003")
