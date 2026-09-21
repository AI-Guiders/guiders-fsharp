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
    let ``extra end block fires ExtraEndBlock rule`` () =
        let graph = DashSpecConceptGraphBuilder.buildFromText "end dashboard\n"
        let diagnostics = DashSpecRuleEngine.evaluateBlockBalance graph
        let expected = DashSpecRuleRegistry.code DashSpecRuleKind.ExtraEndBlock

        Assert.Contains(diagnostics, fun diagnostic -> diagnostic.Code = expected)

    [<Fact>]
    let ``unbalanced end block fires MismatchedEndKeyword rule`` () =
        let graph = DashSpecConceptGraphBuilder.buildFromText "@dashboard demo\nend tab\nend dashboard\n"
        let diagnostics = DashSpecRuleEngine.evaluateBlockBalance graph
        let expected = DashSpecRuleRegistry.code DashSpecRuleKind.MismatchedEndKeyword

        Assert.Contains(diagnostics, fun diagnostic -> diagnostic.Code = expected)

    [<Fact>]
    let ``rule registry assigns sequential DS codes`` () =
        Assert.Equal("DS001", DashSpecRuleRegistry.code DashSpecRuleKind.ExtraEndBlock)
        Assert.Equal("DS007", DashSpecRuleRegistry.code DashSpecRuleKind.RoundTripOutlineChanged)

    [<Fact>]
    let ``as title is stored as concept node attribute`` () =
        let graph = DashSpecConceptGraphBuilder.buildFromText sample
        let tab =
            graph.Nodes
            |> Seq.pick (fun pair ->
                match pair.Value.Kind with
                | DashSpecConceptKind.Block(DashSpecBlockKeyword.Tab, Some "x") -> Some pair.Value
                | _ -> None)

        Assert.Equal(Some "T", tab.Title)
        Assert.Equal("tab x", tab.Label)

    [<Fact>]
    let ``tab with as and end tab passes block balance`` () =
        let text =
            "@dashboard demo\n    tab overview as \"Overview\"\n        cards\n            peak\n        end cards\n    end tab\nend dashboard\n"

        let errors =
            DashSpecRuleEngine.evaluate (DashSpecConceptGraphBuilder.buildFromText text)
            |> List.filter (fun diagnostic -> diagnostic.Severity = "error")

        Assert.Empty(errors)
