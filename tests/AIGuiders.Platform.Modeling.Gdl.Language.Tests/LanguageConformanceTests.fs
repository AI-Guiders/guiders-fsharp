module AIGuiders.Platform.Modeling.Gdl.Language.Tests.LanguageConformanceTests

open Xunit
open AIGuiders.Platform.Modeling.Gdl.Language

[<Fact>]
let ``ResolveTier: wire values 0..2`` () =
    Assert.Equal(0, int ResolveTier.Text)
    Assert.Equal(1, int ResolveTier.Syntax)
    Assert.Equal(2, int ResolveTier.Semantic)

[<Fact>]
let ``Locus: ofRange defaults to Text tier without symbol`` () =
    let l = Locus.ofRange 10 20
    Assert.Equal(10, l.Start)
    Assert.Equal(20, l.End)
    Assert.Equal(ResolveTier.Text, l.Tier)
    Assert.True l.SymbolId.IsNone
    Assert.True l.FilePath.IsNone

[<Fact>]
let ``Locus: semantic carries symbol id`` () =
    let l = Locus.semantic 5 15 "A.B.C"
    Assert.Equal(ResolveTier.Semantic, l.Tier)
    Assert.Equal(Some "A.B.C", l.SymbolId)

[<Fact>]
let ``BufferEditOutcome: fromText carries text and selection`` () =
    let o = BufferEditOutcome.fromText "hello" 0 5
    Assert.Equal(Some "hello", o.Text)
    Assert.Equal(Some 0, o.SelectionStart)
    Assert.Equal(Some 5, o.SelectionEnd)
    Assert.True o.TextMode.IsNone
    Assert.True o.Edits.IsNone

[<Fact>]
let ``TextEdit: range and replacement`` () =
    let e = { Start = 3; End = 7; NewText = "abc" }
    Assert.Equal(3, e.Start)
    Assert.Equal(7, e.End)
    Assert.Equal("abc", e.NewText)

[<Fact>]
let ``SniperScope: empty has no axes; full carries all`` () =
    let empty = SniperScope.empty
    Assert.True empty.FromLine.IsNone
    Assert.True empty.TillLine.IsNone
    Assert.True empty.Wire.IsNone
    Assert.True empty.Pad.IsNone

    let full = { FromLine = Some 1; TillLine = Some 5; Wire = Some "w"; Pad = Some "2" }
    Assert.Equal(Some 1, full.FromLine)
    Assert.Equal(Some 5, full.TillLine)

[<Fact>]
let ``BracketAxisFamily: wire values incl Json = 5`` () =
    Assert.Equal(0, int BracketAxisFamily.None)
    Assert.Equal(1, int BracketAxisFamily.Csharp)
    Assert.Equal(2, int BracketAxisFamily.Xml)
    Assert.Equal(3, int BracketAxisFamily.Navigation)
    Assert.Equal(4, int BracketAxisFamily.Fsharp)
    Assert.Equal(5, int BracketAxisFamily.Json)

[<Fact>]
let ``BracketAnchorSpan: equality and nested anchors`` () =
    let inner = { BracketAnchorSpan.empty with MemberKey = Some "inner" }
    let outer = { BracketAnchorSpan.empty with File = Some "a.fs"; NestedAnchor = Some inner }

    Assert.Equal(Some "a.fs", outer.File)
    Assert.Equal(Some "inner", outer.NestedAnchor.Value.MemberKey)

    let same = { outer with File = Some "a.fs" }
    Assert.Equal(outer, same)

[<Fact>]
let ``AnchorWire: carries raw value`` () =
    let w = { Value = "[F:a.fs;M:Foo]" }
    Assert.Equal("[F:a.fs;M:Foo]", w.Value)