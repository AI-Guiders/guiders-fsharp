module AIGuiders.Platform.Modeling.Cockpit.Channels.Tests.ConformanceTests

open System.Text.Json
open Xunit
open AIGuiders.Platform.Modeling.Cockpit.Channels
open AIGuiders.Platform.Modeling.Cockpit.Ids
open AIGuiders.Platform.Modeling.Cockpit.Composition

[<Fact>]
let ``Lamp algebra: severity rank W < C < A`` () =
    Assert.True (AnnunciatorLamp.severityRank Ok < AnnunciatorLamp.severityRank Advisory)
    Assert.True (AnnunciatorLamp.severityRank Advisory < AnnunciatorLamp.severityRank Caution)
    Assert.True (AnnunciatorLamp.severityRank Caution < AnnunciatorLamp.severityRank Critical)

[<Fact>]
let ``Lamp algebra: maxLevel drives master lamp`` () =
    let strip =
        [ { Id = "a1"; Title = "t1"; Detail = "d"; Level = Ok; LampShortLabel = "OK" }
          { Id = "a2"; Title = "t2"; Detail = "d"; Level = Critical; LampShortLabel = "A" } ]
    Assert.Equal(Critical, AnnunciatorLamp.maxLevel strip)
    Assert.Equal(Ok, AnnunciatorLamp.maxLevel [])

[<Fact>]
let ``EnvReadiness: mergeExtension keeps core first, skips empty`` () =
    let core =
        { Rows =
            [ { Id = EnvironmentReadinessCellIds.Agent
                Title = "agent"
                Detail = "d"
                Level = Ok
                LampShortLabel = "G" } ] }
    let ext =
        [ { Id = "e1"; Title = "x"; Detail = "d"; Level = Advisory; LampShortLabel = "X" } ]
    let merged = EnvironmentReadiness.mergeExtension core ext
    Assert.Equal(2, List.length merged.Rows)
    Assert.Equal(EnvironmentReadinessCellIds.Agent, (List.head merged.Rows).Id)
    Assert.Equal(core, EnvironmentReadiness.mergeExtension core [])

[<Fact>]
let ``IdeHealth: decideScope — project iff startup AND signal`` () =
    let project = IdeHealth.decideScope "src/Module.fs" false true
    Assert.True project.IsProjectScope
    Assert.Equal("src/Module.fs", project.ProjectPath)

    let solution = IdeHealth.decideScope "" false true
    Assert.False solution.IsProjectScope

    let noSignal = IdeHealth.decideScope "src/Module.fs" false false
    Assert.False noSignal.IsProjectScope

[<Fact>]
let ``IdeHealth: summarize idle/running/paused`` () =
    Assert.Equal("idle", IdeHealth.summarizeDebug false false 0 0)
    Assert.Equal("running\u2026", IdeHealth.summarizeDebug true false 0 0)
    Assert.Equal("paused \u00B7 frames 3, vars 2", IdeHealth.summarizeDebug true true 3 2)

[<Fact>]
let ``Ids: hit shape + score sort`` () =
    let h1 = IdsFeatureHit.make "go build" 5 "cdp_build"
    let h2 = IdsFeatureHit.make "go test" 9 "cdp_test"
    Assert.Equal("go build", h1.Go)
    let sorted = IdsFeatureHit.byScoreDesc [ h1; h2 ] |> List.ofSeq
    Assert.Equal("cdp_test", (List.head sorted).Tool)

[<Fact>]
let ``Composition: seats scene schema surfaces`` () =
    let scene =
        { SchemaVersion = "1"
          Mfd = "SolutionExplorer"
          View = JsonElement()
          Seats = JsonElement()
          Session = JsonElement()
          Instrument = Absent
          Alert = Absent
          Pressure = Absent
          Next = JsonElement()
          Focus = Absent
          Go = Absent
          Warm = Absent
          Pins = [ "c1" ]
          Layouts = [ "code+git" ]
          ThrashNote = null
          Loci = Absent
          GoVerbs = [] }
    Assert.Equal("SolutionExplorer", scene.Mfd)
    Assert.Equal(2, scene.Pins.Length + scene.Layouts.Length)