module AIGuiders.Platform.Modeling.Conformance.Tests.AttachSchemaTests

open System
open System.IO
open System.Text.Json
open Xunit
open AIGuiders.Platform.Modeling.CommandPlane
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Paths

[<CLIMutable>]
type AttachSchemaSpecVector =
    { Id: string
      Verb: string
      TargetCase: string
      Steps: string list }

[<CLIMutable>]
type AttachSchemaSpecDocument =
    { Kind: string
      Version: int
      Source: string
      Vectors: AttachSchemaSpecVector list }

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

[<Fact>]
let ``AttachSchema catalog exposes all attach verbs`` () =
    Assert.Equal(6, AttachSchemaCatalog.allVerbs.Length)
    Assert.Equal(6, AttachSchemaCatalog.schemas.Length)

[<Fact>]
let ``AttachSchema conformance vectors match spec document`` () =
    let specPath =
        Path.Combine(
            __SOURCE_DIRECTORY__,
            "..",
            "..",
            "docs",
            "conformance",
            "commandplane",
            "attach-schema.spec.json"
        )
        |> Path.GetFullPath

    let json = File.ReadAllText specPath
    let doc = JsonSerializer.Deserialize<AttachSchemaSpecDocument>(json, JsonSerializerOptions(PropertyNameCaseInsensitive = true))
    Assert.Equal("commandplane.attach-schema", doc.Kind)

    for vector in doc.Vectors do
        let errors = AttachSchema.validateSpecVector vector.Verb vector.TargetCase vector.Steps

        if not errors.IsEmpty then
            let detail = String.concat "; " errors
            Assert.True(false, $"{vector.Id}: {detail}")
