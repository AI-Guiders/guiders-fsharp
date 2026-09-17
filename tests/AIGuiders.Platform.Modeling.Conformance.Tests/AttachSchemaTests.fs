module AIGuiders.Platform.Modeling.Conformance.Tests.AttachSchemaTests

open Xunit
open AIGuiders.Platform.Modeling.CommandPlane
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Paths

[<Fact>]
let ``AttachSchema maps error verb to Diag steps`` () =
    let schema = AttachSchema.forVerb AttachVerb.Error
    Assert.Equal("Diag", schema.TargetCase)
    Assert.Equal("pick_diagnostic", (Assert.Single(schema.Steps)).Id)

[<Fact>]
let ``AttachSchema relationSpecCaseName roundtrips CodeEdit`` () =
    let spec =
        RelationSpec.CodeEdit(
            CodeTarget.Symbol(DocumentRef.File(LogicalPath.Create "Foo.fs"), { Container = []; Name = "Bar"; Arity = None })
        )

    Assert.Equal("CodeEdit", AttachSchema.relationSpecCaseName spec)
