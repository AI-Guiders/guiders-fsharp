module AIGuiders.Platform.Modeling.Notations.Bracket.Tests.BracketRelationWireTests

open System.Collections.Generic
open Xunit
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Notations.Bracket
open AIGuiders.Platform.Modeling.Paths

let private kvWire kind file memberName =
    { ProfileId = BracketProfiles.CdpSquareKeyValue.Id
      Raw = $"[Kind:{kind}; File:{file}; Member:{memberName}]"
      Axes =
        [ BracketAxis("Kind", ':', kind)
          BracketAxis("File", ':', file)
          BracketAxis("Member", ':', memberName) ]
        :> IReadOnlyList<_> }

[<Fact>]
let ``Kind CodeEdit parses to RelationSpec CodeEdit`` () =
    let wire = kvWire "CodeEdit" "src/Foo.fs" "Bar"

    match BracketRelationWire.tryParseRelationSpec wire with
    | None -> Assert.Fail "expected CodeEdit spec"
    | Some (RelationSpec.CodeEdit (CodeTarget.Symbol(DocumentRef.File path, sym))) ->
        Assert.Equal("src/Foo.fs", path.Value)
        Assert.Equal("Bar", sym.Name)
    | Some _ -> Assert.Fail "unexpected spec case"

[<Fact>]
let ``Wire without Kind returns none`` () =
    let wire =
        { ProfileId = BracketProfiles.CdpSquareKeyValue.Id
          Raw = "[File:src/Foo.fs; Member:Bar]"
          Axes = [ BracketAxis("File", ':', "src/Foo.fs") ] :> IReadOnlyList<_> }

    Assert.True(BracketRelationWire.tryParseRelationSpec wire |> Option.isNone)

[<Fact>]
let ``Kind CodeEdit Element parses xml wire encoding`` () =
    let wire =
        { ProfileId = BracketProfiles.CdpSquareKeyValue.Id
          Raw = "[Kind:CodeEdit; File:doc.xml; Element:Root/Item]"
          Axes =
            [ BracketAxis("Kind", ':', "CodeEdit")
              BracketAxis("File", ':', "doc.xml")
              BracketAxis("Element", ':', "Root/Item") ]
            :> IReadOnlyList<_> }

    match BracketRelationWire.tryParseRelationSpec wire with
    | None -> Assert.Fail "expected CodeEdit spec"
    | Some (RelationSpec.CodeEdit (CodeTarget.Symbol(DocumentRef.File path, sym))) ->
        Assert.Equal("doc.xml", path.Value)
        Assert.Equal("Root/Item", sym.Name)
        Assert.Equal(XmlWireEncoding.Marker, sym.Container.[0])
    | Some _ -> Assert.Fail "unexpected spec case"

[<Fact>]
let ``Kind Nav parses NavSeed`` () =
    let wire =
        { ProfileId = BracketProfiles.CdpSquareKeyValue.Id
          Raw = "[Kind:Nav; File:README.md; Line:10; Command:open]"
          Axes =
            [ BracketAxis("Kind", ':', "Nav")
              BracketAxis("File", ':', "README.md")
              BracketAxis("Line", ':', "10")
              BracketAxis("Command", ':', "open") ]
            :> IReadOnlyList<_> }

    match BracketRelationWire.tryParseRelationSpec wire with
    | Some (RelationSpec.Nav seed) ->
        Assert.Equal("README.md", seed.Path.Value)
        Assert.Equal(Some 10, seed.Line)
        Assert.Equal(Some "open", seed.Command)
    | _ -> Assert.Fail "expected Nav spec"

[<Fact>]
let ``Kind CodeEdit line and scope encode wire hints`` () =
    let wire =
        { ProfileId = BracketProfiles.CdpSquareKeyValue.Id
          Raw = "[Kind:CodeEdit; File:a.cs; Member:Foo; Line:10; Scope:for; ScopeIndex:2]"
          Axes =
            [ BracketAxis("Kind", ':', "CodeEdit")
              BracketAxis("File", ':', "a.cs")
              BracketAxis("Member", ':', "Foo")
              BracketAxis("Line", ':', "10")
              BracketAxis("Scope", ':', "for")
              BracketAxis("ScopeIndex", ':', "2") ]
            :> IReadOnlyList<_> }

    match BracketRelationWire.tryParseRelationSpec wire with
    | Some (RelationSpec.CodeEdit (CodeTarget.Symbol(_, sym))) ->
        Assert.Equal("Foo", sym.Name)
        Assert.Contains("@line:10", sym.Container)
        Assert.Contains("@scope:for:2", sym.Container)
    | _ -> Assert.Fail "expected CodeEdit spec"
