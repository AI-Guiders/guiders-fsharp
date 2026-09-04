module AIGuiders.Platform.Modeling.Notations.Bracket.Tests.BracketShapeTests

open System.Collections.Generic
open Xunit
open AIGuiders.Platform.Modeling.Notations.Bracket

[<Fact>]
let ``CdpSquareKeyValue profile: terminals separator kv-sign nested keys`` () =
    let p = BracketProfiles.CdpSquareKeyValue
    Assert.Equal("bracket.cdp-square-kv", p.Id)
    Assert.Equal("[", p.StartTerminal)
    Assert.Equal("]", p.EndTerminal)
    Assert.Equal(';', p.ListSeparator)
    Assert.Equal(':', p.KvSign)
    Assert.Equal(BracketAxisShape.KeyValue, p.AxisShape)
    Assert.True p.StripOuterTerminals
    Assert.True p.RespectBracketDepthOnListSplit
    Assert.Equal(1, p.NestedAxisKeys.Count)
    Assert.Equal("Anchor", p.NestedAxisKeys.[0])

[<Fact>]
let ``AngleOpaque profile: opaque shape, no nested keys`` () =
    let p = BracketProfiles.AngleOpaque
    Assert.Equal("bracket.angle-opaque", p.Id)
    Assert.Equal("<", p.StartTerminal)
    Assert.Equal(">", p.EndTerminal)
    Assert.Equal(BracketAxisShape.Opaque, p.AxisShape)
    Assert.True (isNull p.NestedAxisKeys)

[<Fact>]
let ``DocSymbol profile: kv shape without nested`` () =
    let p = BracketProfiles.DocSymbol
    Assert.Equal("bracket.doc-symbol", p.Id)
    Assert.Equal(BracketAxisShape.KeyValue, p.AxisShape)
    Assert.True (isNull p.NestedAxisKeys)

[<Fact>]
let ``BracketAxis: defaults to opaque wire class, no nested`` () =
    let axis = BracketAxis("F", ':', "a.fs")
    Assert.Equal("F", axis.Key)
    Assert.Equal("a.fs", axis.Value)
    Assert.Equal(BracketAxisValueClasses.Opaque, axis.ValueWireClass)
    Assert.True (isNull (box axis.Nested))

    let nestedWire =
        { ProfileId = "p"; Axes = [||] :> IReadOnlyList<BracketAxis>; Raw = "inner" }
    let nested = BracketAxis("Anchor", ':', "inner", BracketAxisValueClasses.NestedBracket, nestedWire)
    Assert.Equal(BracketAxisValueClasses.NestedBracket, nested.ValueWireClass)
    Assert.Equal("inner", nested.Nested.Raw)

[<Fact>]
let ``CdpCode value plan: axis classes per CDP vocabulary`` () =
    let plan = BracketAxisValuePlans.CdpCode
    Assert.Equal(BracketAxisValueClasses.CommandPath, plan.ByAxisKey.["F"])
    Assert.Equal(BracketAxisValueClasses.LineRange, plan.ByAxisKey.["L"])
    Assert.Equal(BracketAxisValueClasses.Kv, plan.ByAxisKey.["S"])
    Assert.Equal(BracketAxisValueClasses.NestedBracket, plan.ByAxisKey.["Anchor"])
    Assert.Equal(':', plan.DefaultValueKvSign)

[<Fact>]
let ``ForgeFrgCompound plan: FRG maps to command path`` () =
    let plan = BracketAxisValuePlans.ForgeFrgCompound
    Assert.Equal(BracketAxisValueClasses.CommandPath, plan.ByAxisKey.["FRG"])
    Assert.Equal(1, plan.ByAxisKey.Count)

[<Fact>]
let ``Value classes: constants match CDP vocabulary`` () =
    Assert.Equal("opaque", BracketAxisValueClasses.Opaque)
    Assert.Equal("command.path", BracketAxisValueClasses.CommandPath)
    Assert.Equal("notation.kv", BracketAxisValueClasses.Kv)
    Assert.Equal("line.range", BracketAxisValueClasses.LineRange)
    Assert.Equal("bracket.nested", BracketAxisValueClasses.NestedBracket)