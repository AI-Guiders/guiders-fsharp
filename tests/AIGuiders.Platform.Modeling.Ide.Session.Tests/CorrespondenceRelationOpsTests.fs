namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Documentation.Correspondence
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open Xunit

type CorrespondenceRelationOpsTests() =

    [<Fact>]
    member _.``IngestDocToCodeWitnesses merges materialized relations into graph``() =
        let pid = ProjectId.create @"D:\repo\App.csproj"
        let graph, ownership = SessionTestFixtures.createGraph "repo" [] (Map [ "src/Foo.cs", pid ]) []
        let runtime = SessionTestFixtures.createRuntime graph ownership [ "src/Foo.cs", "class Bar {}" ] Unloaded

        let witness =
            { DocPath = "docs/adr/0063.md"
              DocTitle = "ADR-0063"
              Provenance = Provenance.Bracket
              Kind = CorrespondenceRelationKind.Normates
              File = "src/Foo.cs"
              LineStart = None
              LineEnd = None
              MemberKey = Some "Bar"
              Wire = "[F:src/Foo.cs; M:Bar]"
              DocLineHint = None
              Excerpt = None }

        let updated, materialized, skipped =
            CorrespondenceRelationOps.ingestDocToCodeWitnesses [| witness |] runtime

        Assert.Equal(1, materialized)
        Assert.Equal(0, skipped)
        Assert.Equal(1, updated.Session.Graph.Relations.Length)
