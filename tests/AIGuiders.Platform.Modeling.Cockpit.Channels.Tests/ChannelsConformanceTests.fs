module AIGuiders.Platform.Modeling.Cockpit.Channels.Tests.ConformanceTests

open System.Text.Json
open Xunit
open AIGuiders.Platform.Modeling.Cockpit.Channels
open AIGuiders.Platform.Modeling.Cockpit.Cds
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
let ``QRH: relatedMinusHot filters hot, keeps order, caps 4`` () =
    let suggest =
        { HotId = "qrh-nav"
          RelatedIds = [ "qrh-sys"; "qrh-nav"; ""; "qrh-chk"; "qrh-gates"; "qrh-ecl" ]
          Pulse = "qrh" }
    let related = Qrh.relatedMinusHot suggest
    Assert.Equal(4, related.Length)
    Assert.Equal<string>([ "qrh-sys"; "qrh-chk"; "qrh-gates"; "qrh-ecl" ], related)

[<Fact>]
let ``QRH: page shape surfaces (shelf taxonomy)`` () =
    let page: QrhPage =
        { Id = "qrh-nav"
          Shelf = "systems"
          Title = "Navigation"
          Condition = "always"
          Signals = []
          MemoryItems = [ "goto nav" ]
          Steps = [ { Text = "Open nav"; Go = "nav"; Action = null } ]
          Related = []
          PackAnchors = []
          LlmCue = null
          Builtin = true }
    Assert.Equal("systems", page.Shelf)
    Assert.Equal(1, page.Steps.Length)

[<Fact>]
let ``ArchBoard: canonical circuit chain transport→surface`` () =
    let chain = ArchBoard.canonicalChain
    Assert.Equal(6, chain.Length)
    // first link feeds ccu, last projects to surface
    let (from0, to0, _) = List.head chain
    Assert.Equal("transport-ingest", from0)
    let (_, toLast, kindLast) = List.last chain
    Assert.Equal("surf-core", toLast)
    Assert.Equal("projects", kindLast)

[<Fact>]
let ``ArchBoard: doc shape round-trip`` () =
    let doc: ArchBoardDoc =
        { Title = "as-built · cdp"
          Mode = "as_built"
          Profile = "cide"
          UpdatedUtc = "2026-09-06T22:00:00Z"
          Roles =
            [ { Id = "ccu-core"; Role = "ccu"; Status = "done"; Note = "units" }
              { Id = "cds-core"; Role = "cds"; Status = "open"; Note = "" } ]
          Edges = [ { FromRoleId = "ccu-core"; ToRoleId = "cds-core"; Kind = "feeds" } ]
          FocusRoleId = "ccu-core" }
    Assert.Equal(2, doc.Roles.Length)
    Assert.Equal("ccu-core", doc.Roles[0].Id)
    Assert.Equal(1, doc.Edges.Length)

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
          ThrashNote = ""
          Loci = Absent
          GoVerbs = [] }
    Assert.Equal("SolutionExplorer", scene.Mfd)
    Assert.Equal(2, scene.Pins.Length + scene.Layouts.Length)

// ── CDS Circuit — routing eval (parity with CDP AttentionRoutingUnit) ──

let private input (mfdExplicit: string | null) (goVerb: string | null) seatsMode (defaultMfd: string | null) =
    { MfdExplicit = mfdExplicit
      GoVerb = goVerb
      SeatsMode = seatsMode
      DefaultMfd = defaultMfd }

[<Fact>]
let ``CDS routing: explicit MFD wins, unknown page falls to nav`` () =
    let d = CdsRouting.route (input "sys" null false "mfd")
    Assert.Equal("sys", d.Mfd)

    let unknown = CdsRouting.route (input "bogus" null false "mfd")
    Assert.Equal("nav", unknown.Mfd)

[<Fact>]
let ``CDS routing: seats-mode defaults to nav, else default MFD`` () =
    let seats = CdsRouting.route (input null null true "mfd")
    Assert.Equal("nav", seats.Mfd)

    let plain = CdsRouting.route (input null null false "gates")
    Assert.Equal("gates", plain.Mfd)

    let unknownDefault = CdsRouting.route (input null null false "mfd")
    Assert.Equal("nav", unknownDefault.Mfd)

[<Fact>]
let ``CDS routing: go-verb page routes and forces nav on nav`` () =
    let sys = CdsRouting.route (input null "sys" false "mfd")
    Assert.Equal("sys", sys.Mfd)
    Assert.Equal("sys", sys.GoVerb)

    let nav = CdsRouting.route (input null "nav" false "mfd")
    Assert.Equal("nav", nav.Mfd)
    Assert.Equal("", nav.GoVerb)
    Assert.True nav.DeskDetailNavForced

[<Fact>]
let ``CDS routing: layout verbs null out, page+goVerb promotes`` () =
    let layout = CdsRouting.route (input null "layout" false "mfd")
    Assert.Equal("nav", layout.Mfd)
    Assert.Equal("", layout.GoVerb)

    let promoted = CdsRouting.route (input "chk" null false "mfd")
    Assert.Equal("chk", promoted.Mfd)
    Assert.Equal("chk", promoted.GoVerb)

[<Fact>]
let ``CDS desk detail: focus escalates slim to nav, omit clamps, unknown clamps`` () =
    let focused = CdsRouting.resolveDeskDetail "slim" "buffer:doc-1"
    Assert.Equal("nav", focused.DeskDetail)
    Assert.True focused.WantNav

    let omit = CdsRouting.resolveDeskDetail "omit" ""
    Assert.Equal("slim", omit.DeskDetail)
    Assert.False omit.WantNav

    let compact = CdsRouting.resolveDeskDetail "compact" ""
    Assert.Equal("slim", compact.DeskDetail)

    let full = CdsRouting.resolveDeskDetail "full" ""
    Assert.Equal("full", full.DeskDetail)
    Assert.True full.WantNav