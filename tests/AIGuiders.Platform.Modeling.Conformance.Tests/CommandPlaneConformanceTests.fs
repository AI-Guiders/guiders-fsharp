module AIGuiders.Platform.Modeling.Conformance.Tests.CommandPlaneTests

open Xunit
open AIGuiders.Platform.Modeling.CommandPlane

[<Fact>]
let ``PrefixArm: ready / arm-constructor / no-match states`` () =
    let ready = PrefixArmMatch.ready "2026-09-06" "06.09"
    Assert.Equal(PrefixArmDisposition.Ready, ready.Disposition)
    Assert.Equal("2026-09-06", ready.Wire)

    let arm = PrefixArmMatch.armConstructor "range" "06.." [ ("day", "06") ]
    Assert.Equal(PrefixArmDisposition.ArmConstructor, arm.Disposition)
    Assert.Equal("range", arm.RootConstructorId)
    Assert.Equal(1, arm.Segments.Length)

    Assert.Equal(PrefixArmDisposition.NoMatch, PrefixArmMatch.noMatch.Disposition)

[<Fact>]
let ``Locale: field order from pattern (parity with C# ParseFieldOrder)`` () =
    // Parity: C# ParseFieldOrder yields one entry per y/M/d letter, doubled letters
    // consume the pair (so "yyyy" = 2 Year entries). Same behavior here.
    let ru = LocaleInputProfile.fromPattern "dd.MM.yyyy"
    Assert.Equal<LocaleDateField list>
        ([ LocaleDateField.Day; LocaleDateField.Month
           LocaleDateField.Year; LocaleDateField.Year ], ru.FieldOrder)

    let us = LocaleInputProfile.fromPattern "M/d/yyyy"
    Assert.Equal<LocaleDateField list>
        ([ LocaleDateField.Month; LocaleDateField.Day
           LocaleDateField.Year; LocaleDateField.Year ], us.FieldOrder)

    let fallback = LocaleInputProfile.fromPattern "nonsense"
    Assert.Equal(3, fallback.FieldOrder.Length)

[<Fact>]
let ``Locale: separators collect letter-less chars + defaults`` () =
    let ru = LocaleInputProfile.fromPattern "dd.MM.yyyy"
    Assert.Contains('.', ru.Separators)
    Assert.Contains('/', ru.Separators)
    Assert.Contains('-', ru.Separators)

[<Fact>]
let ``Locale: completeness algebra`` () =
    Assert.Equal(LocaleDateCompleteness.CompleteDate, LocaleDateAlgebra.completeness 3 true true true)
    Assert.Equal(LocaleDateCompleteness.MonthYear, LocaleDateAlgebra.completeness 2 false true true)
    Assert.Equal(LocaleDateCompleteness.Partial, LocaleDateAlgebra.completeness 1 true false false)
    Assert.Equal(LocaleDateCompleteness.Empty, LocaleDateAlgebra.completeness 0 false false false)

[<Fact>]
let ``Constructors: registry register + tryGet`` () =
    let leaf =
        ConstructorRegistry.Leaf
            { Id = "day"
              Label = "Day"
              Segments = [ { SegmentId = "day"; Label = "Day"; WireMinWidth = 2; DisplayMinWidth = 1 } ]
              WirePattern = "{day}"
              DisplayPattern = "{day}" }

    let registry = ConstructorRegistry.empty |> ConstructorRegistry.register leaf

    let found = ConstructorRegistry.tryGet "day" registry
    Assert.True found.IsSome
    Assert.Equal("day", found.Value.Id)

    let missing = ConstructorRegistry.tryGet "month" registry
    Assert.True missing.IsNone

[<Fact>]
let ``Constructors: composite shape round-trip`` () =
    let composite: CompositeConstructorDef =
        { Id = "date-range"
          Label = "Date range"
          Slots =
            [ { SlotId = "from"; ConstructorId = "day"; SeparatorBefore = null }
              { SlotId = "to"; ConstructorId = "day"; SeparatorBefore = " .. " } ]
          WirePattern = "{from}..{to}" }
    Assert.Equal(2, composite.Slots.Length)
    Assert.Equal("from", composite.Slots[0].SlotId)