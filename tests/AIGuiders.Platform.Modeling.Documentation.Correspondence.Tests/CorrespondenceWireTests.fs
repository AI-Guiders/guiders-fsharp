module AIGuiders.Platform.Modeling.Documentation.Correspondence.Tests.CorrespondenceWireTests

open Xunit
open AIGuiders.Platform.Modeling.Documentation.Correspondence

[<Fact>]
let ``buildCodeEdit emits Kind canon wire`` () =
    let wire = BracketWire.buildCodeEdit "src/Foo.cs" (Some "Bar") (Some 10)
    Assert.Equal("[Kind:CodeEdit; File:src/Foo.cs; Member:Bar; Line:10]", wire)

[<Fact>]
let ``build: member wins over line span`` () =
    let wire = BracketWire.build "docs/adr/x.md" (Some 10) (Some 20) (Some "Foo")
    Assert.Equal("[F:docs/adr/x.md; M:Foo]", wire)

[<Fact>]
let ``build: line span emits L and L2`` () =
    let wire = BracketWire.build "docs/adr/x.md" (Some 10) (Some 20) None
    Assert.Equal("[F:docs/adr/x.md; L:10; L2:20]", wire)

[<Fact>]
let ``build: single line omits L2 and normalizes path separators`` () =
    let wire = BracketWire.build @"docs\adr\x.md" (Some 10) (Some 10) None
    Assert.Equal("[F:docs/adr/x.md; L:10]", wire)

[<Fact>]
let ``build: no line when only file given`` () =
    let wire = BracketWire.build "docs/adr/x.md" None None None
    Assert.Equal("[F:docs/adr/x.md]", wire)

[<Fact>]
let ``model: kind and provenance constants keep wire vocabulary`` () =
    Assert.Equal("correspondence/v0", Schema.V0)
    Assert.Equal("normates", Kind.Normates)
    Assert.Equal("verified_by", Kind.VerifiedBy)
    Assert.Equal("bracket", Provenance.Bracket)
