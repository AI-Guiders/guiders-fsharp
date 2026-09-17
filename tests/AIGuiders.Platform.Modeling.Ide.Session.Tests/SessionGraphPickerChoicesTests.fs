namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

type SessionGraphPickerChoicesTests() =

    [<Fact>]
    member _.``SemanticSymbolPickerChoices collects graph semantic nodes``() =
        let pid = ProjectId.create @"D:\repo\App.fsproj"
        let graph, ownership = SessionTestFixtures.createGraph "repo" [] (Map [ "src/Foo.fs", pid ]) [] 

        let doc = DocumentRef.File(LogicalPath.Create "src/Foo.fs")
        let sym = { Container = [ "Module" ]; Name = "Bar"; Arity = None }

        let relation =
            { From = GraphNodeRef.Semantic(doc, sym)
              Type = RelationType.Uses
              To = GraphNodeRef.Semantic(doc, { Container = []; Name = "Baz"; Arity = None })
              Scope = SemanticSubstrate pid
              Attributes = RelationAttributes.empty }

        let session = SolutionSession.create graph.Anchor graph
        let runtime = SessionTestFixtures.createRuntime graph ownership [ "src/Foo.fs", "let x = 1" ] Unloaded

        let runtimeWithRelation =
            { runtime with
                Session =
                    { runtime.Session with
                        Graph = { graph with Relations = relation :: graph.Relations } } }

        let choices = SessionGraphPickerChoices.semanticSymbolPickerChoices runtimeWithRelation

        Assert.Equal(2, choices.Length)
        Assert.Contains(choices, fun c -> c.Id = "Module.Bar")
        Assert.Contains(choices, fun c -> c.Id = "Baz")

    [<Fact>]
    member _.``RelationSpecKindChoices lists attach manual kinds``() =
        let kinds = SessionGraphPickerChoices.relationSpecKindChoices |> List.map (fun c -> c.Id)
        Assert.Equal(6, kinds.Length)
        Assert.Contains("CodeEdit", kinds)
        Assert.Contains("Resource", kinds)
