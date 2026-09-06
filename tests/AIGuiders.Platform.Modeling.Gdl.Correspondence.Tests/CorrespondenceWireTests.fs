module AIGuiders.Platform.Modeling.Gdl.Correspondence.Tests.CorrespondenceWireTests

open Xunit
open AIGuiders.Platform.Modeling.Gdl.Correspondence

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

/// C# parity: L2 is emitted on build but the parser keeps lineEnd = lineStart
/// (same TryParseBracket semantics as the transitional Execution implementation).
[<Fact>]
let ``parse: round-trip line span (L2 collapses to L, C# parity)`` () =
    let wire = BracketWire.build "docs/adr/x.md" (Some 10) (Some 20) None
    match BracketWire.tryParseBracket wire with
    | Some(f, Some 10, Some 10, None) -> Assert.Equal("docs/adr/x.md", f)
    | _ -> failwith "expected parsed line span"

/// Member wins on build — no L keys in the wire, so parse yields no lines.
[<Fact>]
let ``parse: round-trip member (key without M: prefix, C# parity)`` () =
    let wire = BracketWire.build "docs/adr/x.md" (Some 10) (Some 20) (Some "Foo")
    match BracketWire.tryParseBracket wire with
    | Some(f, None, None, Some "Foo") -> Assert.Equal("docs/adr/x.md", f)
    | _ -> failwith "expected parsed member"

[<Fact>]
let ``parse: tolerates whitespace separators and normalizes path`` () =
    match BracketWire.tryParseBracket @"[F:docs\adr\x.md L:3 M:Bar]" with
    | Some(f, Some 3, Some 3, Some "Bar") -> Assert.Equal("docs/adr/x.md", f)
    | _ -> failwith "expected parsed mixed separators"

[<Fact>]
let ``parse: empty and fileless brackets yield None`` () =
    Assert.Equal(None, BracketWire.tryParseBracket "")
    Assert.Equal(None, BracketWire.tryParseBracket "   ")
    Assert.Equal(None, BracketWire.tryParseBracket "[M:Foo]")

[<Fact>]
let ``model: kind and provenance constants keep wire vocabulary`` () =
    Assert.Equal("correspondence/v0", Schema.V0)
    Assert.Equal("normates", Kind.Normates)
    Assert.Equal("verified_by", Kind.VerifiedBy)
    Assert.Equal("bracket", Provenance.Bracket)