namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Ide.Session
open Xunit

/// <summary>Conformance to math §7.4: Γ_π, H, IB1, SN2, SN4, F# ordered units.</summary>
module CompileGraphTests =

    let pid = ProjectId.create @"D:\repo\src\App\App.fsproj"

    let fsharpGraph () =
        { Project = pid
          Topology = OrderedUnits [ "Prelude.fs"; "Types.fs"; "Logic.fs"; "Main.fs" ]
          Granularity = FileOrModule }

    let diamondGraph () =
        //      a
        //     / \
        //    b   c
        //     \ /
        //      d
        { Project = pid
          Topology =
            ExplicitEdges
                [ { From = "a"; To = "b" }
                  { From = "a"; To = "c" }
                  { From = "b"; To = "d" }
                  { From = "c"; To = "d" } ]
          Granularity = FileOrModule }

    /// IB1, F# order: a change in one unit rebuilds it and everything after it.
    [<Fact>]
    let ``IB1 ordered: change Types.fs rebuilds Types, Logic, Main`` () =
        let u' = CompileGraph.minimalRebuild (fsharpGraph ()) (FilesChanged [ "Types.fs" ])
        Assert.Equal<Set<string>>(Set.ofList [ "Types.fs"; "Logic.fs"; "Main.fs" ], u')

    /// IB1, F# order: the last unit rebuilds only itself.
    [<Fact>]
    let ``IB1 ordered: change Main.fs rebuilds only Main`` () =
        let u' = CompileGraph.minimalRebuild (fsharpGraph ()) (FilesChanged [ "Main.fs" ])
        Assert.Equal<Set<string>>(Set.ofList [ "Main.fs" ], u')

    /// IB1 explicit edges: transitive closure over the diamond.
    [<Fact>]
    let ``IB1 diamond: change a rebuilds all, change b rebuilds b and d only`` () =
        let g = diamondGraph ()
        Assert.Equal<Set<string>>(Set.ofList [ "a"; "b"; "c"; "d" ], CompileGraph.minimalRebuild g (FilesChanged [ "a" ]))
        Assert.Equal<Set<string>>(Set.ofList [ "b"; "d" ], CompileGraph.minimalRebuild g (FilesChanged [ "b" ]))

    /// SN1: sniper semantic delta uses the same closure as the coarse file path.
    [<Fact>]
    let ``SN1 sniper delta closes over E_pi`` () =
        let u' = CompileGraph.minimalRebuild (diamondGraph ()) (SemanticDelta [ "c" ])
        Assert.Equal<Set<string>>(Set.ofList [ "c"; "d" ], u')

    /// SN2: no delta, no emit — not a full-project fallback.
    [<Fact>]
    let ``SN2 empty delta is NoEmit`` () =
        let plan = EmitPlanner.plan (fsharpGraph ()) (FilesChanged []) ArtifactCache.empty
        Assert.Equal(NoEmit, plan)

    /// SN4: recompile exactly U', reuse the cached rest.
    [<Fact>]
    let ``SN4 plan recomputes U-prime and reuses cache outside it`` () =
        let cache =
            [ "Prelude.fs"; "Types.fs"; "Logic.fs"; "Main.fs" ]
            |> List.map (fun u -> { Unit = u; Key = "k" + u; Revision = 1L; Artifacts = [ u + ".dll" ] })
            |> List.fold (fun c e -> ArtifactCache.put e c) ArtifactCache.empty

        match EmitPlanner.plan (fsharpGraph ()) (FilesChanged [ "Logic.fs" ]) cache with
        | NoEmit -> Assert.Fail "expected an emit plan"
        | Emit(recompile, reuse) ->
            Assert.Equal<string list>([ "Logic.fs"; "Main.fs" ], recompile)
            Assert.Equal<string list>([ "Prelude.fs"; "Types.fs" ], reuse)

    /// §7.4 H: same frozen inputs — same key; any input change — new key.
    [<Fact>]
    let ``H unitKey is content-addressed and stable`` () =
        let k1 = ArtifactCache.unitKey pid "Logic.fs" "let x = 1" "inv-v1"
        let k2 = ArtifactCache.unitKey pid "Logic.fs" "let x = 1" "inv-v1"
        let k3 = ArtifactCache.unitKey pid "Logic.fs" "let x = 2" "inv-v1"
        let k4 = ArtifactCache.unitKey pid "Logic.fs" "let x = 1" "inv-v2"
        Assert.Equal(k1, k2)
        Assert.NotEqual<string>(k1, k3)
        Assert.NotEqual<string>(k1, k4)

    /// BS2: buildDriver is a capability attribute; the catalog default is the tactical MSBuild port.
    [<Fact>]
    let ``BS2 buildTool defaults to msbuild-interop and switches by attribute`` () =
        let build = CapabilityCatalog.buildTool ()
        Assert.Equal(Some MsBuildInterop, build.Attributes.BuildDriver)
        Assert.Equal("msbuild-interop", BuildDriver.toWire MsBuildInterop)
        Assert.Equal("guiders-build", BuildDriver.toWire GuidersBuild)
        // a driver switch is an attribute change, not a schema change (BS2)
        let switched = { build.Attributes with BuildDriver = Some GuidersBuild }
        Assert.Equal(Some GuidersBuild, switched.BuildDriver)
        Assert.Equal(build.Kind, { build with Attributes = switched }.Kind)
