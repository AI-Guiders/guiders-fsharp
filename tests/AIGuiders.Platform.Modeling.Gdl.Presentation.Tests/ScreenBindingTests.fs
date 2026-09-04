module AIGuiders.Platform.Modeling.Gdl.Presentation.Tests.ScreenBindingTests

open System.Collections.Generic
open Xunit
open AIGuiders.Platform.Modeling.Gdl.Presentation

[<Fact>]
let ``PhysicalScreenSelector factories: primary index ultrawide`` () =
    let p = PhysicalScreenSelector.primary
    Assert.Equal(PhysicalScreenSelectorKind.Primary, p.Kind)
    Assert.True p.ScreenIndex.IsNone

    let i = PhysicalScreenSelector.byIndex 2
    Assert.Equal(PhysicalScreenSelectorKind.Index, i.Kind)
    Assert.Equal(Some 2, i.ScreenIndex)

    let u = PhysicalScreenSelector.ultrawide 0.0 0.25 1.0 0.5
    Assert.Equal(PhysicalScreenSelectorKind.UltrawideRegion, u.Kind)
    Assert.Equal(Some 0.0, u.RegionLeft)
    Assert.Equal(Some 0.25, u.RegionTop)
    Assert.Equal(Some 1.0, u.RegionWidth)
    Assert.Equal(Some 0.5, u.RegionHeight)

[<Fact>]
let ``DisplayBindingProfile: binds logical hosts to screens`` () =
    let bindings =
        [ { HostIndex = 0; Screen = PhysicalScreenSelector.primary }
          { HostIndex = 1; Screen = PhysicalScreenSelector.byIndex 1 } ]
        :> IReadOnlyList<DisplayHostBinding>
    let profile = { ProfileId = "op-cabin"; Bindings = bindings }
    Assert.Equal("op-cabin", profile.ProfileId)
    Assert.Equal(2, profile.Bindings.Count)
    Assert.Equal(PhysicalScreenSelectorKind.Primary, profile.Bindings.[0].Screen.Kind)
    Assert.Equal(1, profile.Bindings.[1].HostIndex)

[<Fact>]
let ``Enums: selector and topology wire values`` () =
    Assert.Equal(0, int PhysicalScreenSelectorKind.Primary)
    Assert.Equal(3, int PhysicalScreenSelectorKind.UltrawideRegion)
    Assert.Equal(0, int TopologyArrangement.SingleSurfaceCompositional)
    Assert.Equal(2, int TopologyArrangement.MultiHost)
    Assert.Equal(0, int ZoneComposeKind.Split)
    Assert.Equal(1, int ZoneComposeKind.OneOf)
    Assert.Equal(3, int AttentionDisplayRole.Mfd)
    Assert.Equal(4, int AttentionDisplayRole.PmOneOf)

[<Fact>]
let ``PresentationTopology: HostCount from hosts list`` () =
    let host =
        { HostIndex = 0
          HostId = "host-0"
          Role = AttentionDisplayRole.Pfd
          Compose = ZoneComposeKind.Split
          ChannelStack = [ "pfd" ] |> List.toSeq |> Seq.toList
          ActiveChannel = "pfd" }
    let topology =
        { Arrangement = TopologyArrangement.MultiHost
          Hosts = [ host ]
          SourceWire = "pfd+mfd" }
    Assert.Equal(1, topology.HostCount)