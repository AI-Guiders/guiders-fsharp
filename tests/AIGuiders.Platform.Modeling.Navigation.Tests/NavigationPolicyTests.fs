module AIGuiders.Platform.Modeling.Navigation.Tests.NavigationPolicyTests

open Xunit
open AIGuiders.Platform.Modeling.Navigation
open AIGuiders.Platform.Modeling.Navigation.Policy

[<Fact>]
let ``related kinds: canonical tokens and case-insensitive lookup`` () =
    Assert.Equal(6, RelatedKinds.all.Length)
    Assert.Equal(Some "project_peer", RelatedKinds.tryCanonicalKind (Some "Project_Peer"))
    Assert.Equal(None, RelatedKinds.tryCanonicalKind (Some "bogus_kind"))
    Assert.Equal(None, RelatedKinds.tryCanonicalKind None)

[<Fact>]
let ``presets: unknown preset denies; empty passes`` () =
    Assert.False(Presets.allowsKind (Some "no_such_preset") "project_peer")
    Assert.True(Presets.allowsKind None "anything")
    Assert.True(Presets.allowsKind (Some "   ") "anything")

[<Fact>]
let ``presets: whitelist and exclude honored`` () =
    // peers_only: include = partial_peer + project_peer
    Assert.True(Presets.allowsKind (Some "peers_only") "project_peer")
    Assert.False(Presets.allowsKind (Some "peers_only") "same_namespace")
    // no_namespace_noise: exclude = same_namespace + same_directory
    Assert.True(Presets.allowsKind (Some "no_namespace_noise") "project_peer")
    Assert.False(Presets.allowsKind (Some "no_namespace_noise") "same_namespace")

[<Fact>]
let ``merge: unknown preset returns error`` () =
    let (inc, exc, err) = PresetMerge.merge (Some "bogus") None None
    Assert.Equal(None, inc)
    Assert.Equal(None, exc)
    Assert.True(err.IsSome)

[<Fact>]
let ``merge: request overrides preset include; exclude unioned`` () =
    let (inc, exc, err) =
        PresetMerge.merge
            (Some "no_namespace_noise")
            (Some [ "test_counterpart" ])
            (Some [ "SAME_DIRECTORY"; "test_counterpart" ])

    Assert.True(err.IsNone)
    // request include overrides preset include
    Assert.Equal<string list option>(Some [ "test_counterpart" ], inc)
    // exclude unioned + canonicalized + sorted
    Assert.Equal<string list option>(Some [ "same_directory"; "same_namespace"; "test_counterpart" ], exc)

[<Fact>]
let ``merge: preset exclude applies when request absent`` () =
    let (inc, exc, err) = PresetMerge.merge (Some "explore_default") None None
    Assert.True(err.IsNone)
    Assert.Equal(None, inc)
    Assert.Equal<string list option>(Some [ "project_peer" ], exc)

[<Fact>]
let ``kind filter: whitelist + subtract + unknown tokens ignored`` () =
    let f = KindFilter.create (Some [ "project_peer"; "bogus" ]) (Some [ "same_directory"; "bogus" ])
    Assert.True(KindFilter.allows f "project_peer")
    Assert.False(KindFilter.allows f "test_counterpart") // not in whitelist
    Assert.False(KindFilter.allows f "same_directory") // excluded
    Assert.Equal<string list option>(Some [ "project_peer" ], KindFilter.effectiveInclude f)
    Assert.Equal<string list>([ "same_directory" ], KindFilter.effectiveExclude f)

[<Fact>]
let ``profile: fromExplore defaults and caps`` () =
    let p = Profile.fromExplore (Some "peers_only") (Some 8) None None
    Assert.Equal(Some "peers_only", p.Preset)
    Assert.Equal(8, p.MaxRelated)
    let caps = Profile.toCaps p
    Assert.Equal(8, caps.MaxRelated)
    Assert.Equal(12, caps.MaxNodes)
    Assert.Equal(24, caps.MaxEdges)
    Assert.True(caps.KindCaps.IsSome)
    Assert.Equal(3, caps.KindCaps.Value["project_peer"])

[<Fact>]
let ``scene: empty has schema caps and summary`` () =
    let caps = Profile.toCaps Profile.exploreDefault
    let anchor = { Path = "src/Foo.cs"; Line = None; Column = None; SolutionPath = None }
    let scene = Scene.empty anchor Mode.Related caps
    Assert.Equal(Schemes.SceneV1, scene.Schema)
    Assert.Equal<Node list>([], scene.Nodes)
    Assert.Contains("Foo.cs", scene.Summary)