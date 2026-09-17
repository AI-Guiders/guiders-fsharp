module AIGuiders.Platform.Modeling.LanguageIntelligence.Relations.Tests.RelationSpecTests

open Xunit
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

[<Fact>]
let ``ResolveTier exposes Syntax and Semantic only`` () =
    Assert.Equal(1, int ResolveTier.Syntax)
    Assert.Equal(2, int ResolveTier.Semantic)
    Assert.False(typeof<ResolveTier>.GetEnumNames() |> Array.contains "Text")

[<Fact>]
let ``Locus stores doc-scoped Syntax tier`` () =
    let doc = DocId.mint (NumericId.ofCounter 1L)
    let node = NodeId.mint (NumericId.ofCounter 2L)
    let span = { StartLine = 1; StartCol = 0; EndLine = 1; EndCol = 5 }

    match Locus.Syntax(doc, node, span) with
    | Locus.Syntax(d, n, s) ->
        Assert.Equal(doc, d)
        Assert.Equal(node, n)
        Assert.Equal(5, s.EndCol)
    | _ -> Assert.Fail "expected syntax locus"

[<Fact>]
let ``RelationSpec Diag carries DiagnosticRef not file line`` () =
    let ref = DiagnosticRef.mint (NumericId.ofCounter 9L)
    let spec = RelationSpec.Diag ref
    Assert.Equal(RelationSpec.Diag ref, spec)

[<Fact>]
let ``DocumentRef prefers DocId after registry snap`` () =
    let doc = DocId.mint (NumericId.ofCounter 3L)
    let spec =
        RelationSpec.CodeEdit(
            CodeTarget.Symbol(DocumentRef.DocId doc, { Container = [ "Ns" ]; Name = "Foo"; Arity = None })
        )

    match spec with
    | RelationSpec.CodeEdit (CodeTarget.Symbol(DocumentRef.DocId id, _)) -> Assert.Equal(doc, id)
    | _ -> Assert.Fail "expected code edit symbol target"

[<Fact>]
let ``NavSeed uses LogicalPath not bare string`` () =
    let seed =
        { Path = LogicalPath.Create "src/App.fs"
          Line = Some 10
          Column = None
          Command = Some "go"
          Go = None
          Solution = None }

    Assert.Equal("src/App.fs", seed.Path.Value)
