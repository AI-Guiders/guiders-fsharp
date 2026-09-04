module AIGuiders.Platform.Modeling.Gdl.Melody.Tests.ConformanceTests

open System.Collections.Generic
open Xunit
open AIGuiders.Platform.Modeling.Gdl.Melody

[<Fact>]
let ``FromSlug: PureByNote default, empty steps, help passthrough`` () =
    let d = MelodyDescriptor.fromSlug "cdp_files_delete" "fd" (Some "delete file")
    Assert.Equal("cdp_files_delete", d.CommandId)
    Assert.Equal("fd", d.Slug)
    Assert.Equal(MelodyLineProfile.PureByNote, d.Profile)
    Assert.Empty d.Steps
    Assert.Equal(Some "delete file", d.Help)

[<Fact>]
let ``ToLine: descriptor fields project onto play line`` () =
    let steps =
        [ { Articulation = MelodyArticulation.ByChord; Wire = "Ctrl+R"; ReaderId = None }
          { Articulation = MelodyArticulation.ByNote; Wire = "b"; ReaderId = Some "slotReader" } ]
        :> IReadOnlyList<MelodyStep>
    let d =
        { MelodyDescriptor.fromSlug "id" "slug" None with
            Profile = MelodyLineProfile.Mixed
            Steps = steps }
    let line = MelodyDescriptor.toLine d
    Assert.Equal("slug", line.Slug)
    Assert.Equal(MelodyLineProfile.Mixed, line.Profile)
    Assert.Equal(2, line.Steps.Count)
    Assert.Equal(Some "slotReader", line.Steps.[1].ReaderId)
    Assert.True line.Help.IsNone

[<Fact>]
let ``Enums: articulation and line profile wire values`` () =
    Assert.Equal(0, int MelodyArticulation.ByNote)
    Assert.Equal(1, int MelodyArticulation.ByChord)
    Assert.Equal(0, int MelodyLineProfile.PureByNote)
    Assert.Equal(1, int MelodyLineProfile.PureByChord)
    Assert.Equal(2, int MelodyLineProfile.Mixed)