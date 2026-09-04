module AIGuiders.Platform.Modeling.Notations.Argument.Tests.ArgumentShapeTests

open System.Collections.Generic
open Xunit
open AIGuiders.Platform.Modeling.Notations.Argument

let private configSlot =
    ArgumentSlot("config", ArgumentSlotKind.Value, "--config", null)

let private verboseSlot =
    ArgumentSlot("verbose", ArgumentSlotKind.Flag, "--verbose", "-v")

[<Fact>]
let ``ArgumentSlot: defaults to Value kind, carries options`` () =
    let bare = ArgumentSlot("x")
    Assert.Equal(ArgumentSlotKind.Value, bare.Kind)
    Assert.True (isNull bare.LongOption)
    Assert.Equal("x", bare.Name)

    Assert.Equal("--config", configSlot.LongOption)
    Assert.Equal("--verbose", verboseSlot.LongOption)
    Assert.Equal("-v", verboseSlot.ShortOption)
    Assert.Equal(ArgumentSlotKind.Flag, verboseSlot.Kind)

[<Fact>]
let ``Profile: carries reader id and slots`` () =
    let profile = ArgumentNotationProfile(ArgumentReaders.Cli, [| configSlot; verboseSlot |])
    Assert.Equal("cli", profile.ReaderId)
    Assert.Equal(2, profile.Slots.Count)
    Assert.Equal("config", profile.Slots.[0].Name)
    Assert.False profile.IsEmpty

[<Fact>]
let ``Profile: IsEmpty for blank reader and empty slots`` () =
    let blank = ArgumentNotationProfile(null, null)
    Assert.True blank.IsEmpty
    let emptySlots = ArgumentNotationProfile("cli", [||])
    Assert.False (emptySlots.IsEmpty)

[<Fact>]
let ``Merge: blank incoming keeps existing, non-empty overrides, partial merges`` () =
    let existing = ArgumentNotationProfile("cli", [| configSlot |])
    let blank = ArgumentNotationProfile(null, null)

    let kept = ArgumentNotationProfile.Merge(existing, blank)
    Assert.Equal("cli", kept.ReaderId)
    Assert.Equal(1, kept.Slots.Count)

    let overridden = ArgumentNotationProfile.Merge(existing, ArgumentNotationProfile("kv", [| verboseSlot |]))
    Assert.Equal("kv", overridden.ReaderId)
    Assert.Equal(1, overridden.Slots.Count)
    Assert.Equal("verbose", overridden.Slots.[0].Name)

    let partial = ArgumentNotationProfile.Merge(existing, ArgumentNotationProfile("positional", null))
    Assert.Equal("positional", partial.ReaderId)
    Assert.Equal(1, partial.Slots.Count)

[<Fact>]
let ``ArgumentReaders: wire literal values match C#`` () =
    Assert.Equal("kv", ArgumentReaders.Kv)
    Assert.Equal("cli", ArgumentReaders.Cli)
    Assert.Equal("positional", ArgumentReaders.Positional)
    Assert.Equal("delimited", ArgumentReaders.Delimited)
    Assert.Equal("colon", ArgumentReaders.Colon)
    Assert.Equal("raw", ArgumentReaders.Raw)

[<Fact>]
let ``NormalizedArguments: FromRaw and FromSlots`` () =
    let raw = NormalizedArguments.FromRaw("a=b", ArgumentReaders.Kv)
    Assert.Equal("a=b", raw.Raw)
    Assert.Equal("kv", raw.ReaderId)
    Assert.True (isNull raw.Slots)

    let slots = Dictionary<string, string>()
    slots.["config"] <- "release"
    let fromSlots = NormalizedArguments.FromSlots(slots, ArgumentReaders.Cli)
    Assert.Equal("release", fromSlots.Slots.["config"])
    Assert.Equal("cli", fromSlots.ReaderId)
    Assert.True (isNull fromSlots.Raw)