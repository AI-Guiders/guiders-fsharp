namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open FsCheck
open FsCheck.Xunit
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Ide.Session

/// <summary>
/// Math §7.4 as executable axioms: every property is the axiom stated ∀Γ,Δ.
/// Generators: ordered unit lists, induced-order DAGs (edges only i&lt;j — acyclic by construction), sublists as deltas.
/// </summary>
module CompileGraphProperties =

    let pid = ProjectId.create @"D:\repo\src\App\App.fsproj"

    // --- generators (FsCheck 2.x API) ---

    let genUnits : Gen<CompilationUnitId list> =
        gen {
            let! n = Gen.choose (0, 10)
            return List.init n (fun i -> sprintf "u%d" i)
        }

    /// n flags from one random bitmask — no list padding (an empty Gen.listOf would loop forever).
    let genFlags (n: int) : Gen<bool list> =
        if n <= 0 then Gen.constant []
        else
            Gen.choose (0, (1 <<< n) - 1)
            |> Gen.map (fun mask -> List.init n (fun i -> (mask >>> i) &&& 1 = 1))

    let genOrderedGraph : Gen<CompileGraph> =
        genUnits
        |> Gen.map (fun us -> { Project = pid; Topology = OrderedUnits us; Granularity = FileOrModule })

    let chainEdges (us: CompilationUnitId list) =
        us
        |> List.pairwise
        |> List.map (fun (a, b) -> { From = a; To = b })

    /// Induced-order DAG: edges only from earlier to later units — acyclic by construction.
    let genDagGraph : Gen<CompileGraph> =
        gen {
            let! us = genUnits
            let! flags = genFlags (us.Length * (us.Length - 1) / 2)
            let indexed = List.indexed us
            let flagMap =
                [ for i in 0 .. indexed.Length - 1 do
                      for j in (i + 1) .. indexed.Length - 1 do
                          yield (i, j) ]
                |> List.zip flags
                |> List.filter fst
                |> List.map snd
                |> Set.ofList
            let edges =
                [ for (i, u) in indexed do
                      for (j, v) in indexed do
                          if j > i && Set.contains (i, j) flagMap then yield { From = u; To = v } ]
            return { Project = pid; Topology = ExplicitEdges edges; Granularity = FileOrModule }
        }

    let genSublist (us: CompilationUnitId list) : Gen<CompilationUnitId list> =
        genFlags us.Length
        |> Gen.map (fun flags -> List.zip us flags |> List.filter snd |> List.map fst)

    // --- IB1: U' = Δ_U ∪ descendants(Δ_U), for any Γ and Δ ---

    [<Property>]
    let ``IB1 ordered: roots stay in, U-prime never leaves U-pi`` () =
        Prop.forAll (Arb.fromGen genOrderedGraph) (fun g ->
            let us = CompileGraph.units g
            Prop.forAll (Arb.fromGen (genSublist us)) (fun roots ->
                let up = CompileGraph.minimalRebuild g (FilesChanged roots)
                let rootsIn = roots |> List.forall (fun r -> Set.contains r up)
                let staysIn = up |> Set.forall (fun u -> List.contains u us)
                rootsIn && staysIn))

    [<Property>]
    let ``IB1 dag: closure is root-closed and transitive`` () =
        Prop.forAll (Arb.fromGen genDagGraph) (fun g ->
            let us = CompileGraph.units g
            let edges = match g.Topology with ExplicitEdges es -> es | _ -> []
            Prop.forAll (Arb.fromGen (genSublist us)) (fun roots ->
                let up = CompileGraph.minimalRebuild g (FilesChanged roots)
                let rootsIn = roots |> List.forall (fun r -> Set.contains r up)
                let transitive = edges |> List.forall (fun e -> not (Set.contains e.From up) || Set.contains e.To up)
                rootsIn && transitive))

    [<Property>]
    let ``IB1 monotone: bigger delta never shrinks U-prime`` () =
        Prop.forAll (Arb.fromGen genOrderedGraph) (fun g ->
            let us = CompileGraph.units g
            let twoSublists =
                Gen.two (genSublist us)
                |> Gen.map (fun (small, extra) -> small, List.distinct (small @ extra))
            Prop.forAll (Arb.fromGen twoSublists) (fun (small, big) ->
                let u1 = CompileGraph.minimalRebuild g (FilesChanged small)
                let u2 = CompileGraph.minimalRebuild g (FilesChanged big)
                Set.isSubset u1 u2))

    /// The two representations of E_π agree: an ordered list is its chain written out as explicit edges.
    [<Property>]
    let ``ordered topology equals its explicit chain oracle`` () =
        Prop.forAll (Arb.fromGen genOrderedGraph) (fun g ->
            let us = CompileGraph.units g
            let chainG = { g with Topology = ExplicitEdges(chainEdges us) }
            Prop.forAll (Arb.fromGen (genSublist us)) (fun roots ->
                let viaOrdered = CompileGraph.minimalRebuild g (FilesChanged roots)
                let viaChain = CompileGraph.minimalRebuild chainG (FilesChanged roots)
                viaOrdered = viaChain))

    // --- SN2/SN4: emit plan ---

    [<Property>]
    let ``SN2/SN4 plan: NoEmit iff U-prime empty, else recompile U-prime and reuse cached rest`` () =
        Prop.forAll (Arb.fromGen genOrderedGraph) (fun g ->
            let us = CompileGraph.units g
            Prop.forAll (Arb.fromGen (genSublist us)) (fun roots ->
                Prop.forAll (Arb.fromGen (genSublist us)) (fun cached ->
                    let cache =
                        cached
                        |> List.map (fun u -> { Unit = u; Key = "k:" + u; Revision = 1L; Artifacts = [] })
                        |> List.fold (fun c e -> ArtifactCache.put e c) ArtifactCache.empty
                    let up = CompileGraph.minimalRebuild g (FilesChanged roots)
                    match EmitPlanner.plan g (FilesChanged roots) cache with
                    | NoEmit ->
                        Set.isEmpty up
                    | Emit (recompile, reuse) ->
                        let recompileIsU = Set.ofList recompile = up
                        let reuseIsRest = Set.ofList reuse = ((Set.ofList cached) - up)
                        not (Set.isEmpty up) && recompileIsU && reuseIsRest)))

    // --- H: content-addressed keys ---

    [<Property>]
    let ``H unitKey is deterministic`` (unit: string) (contents: string) (invocation: string) =
        let k1 = ArtifactCache.unitKey pid unit contents invocation
        let k2 = ArtifactCache.unitKey pid unit contents invocation
        k1 = k2

    [<Property>]
    let ``H unitKey separates inputs`` (unit: string) (contents: string) (invocation: string) (other: string) =
        // null is outside the frozen-contents domain (unitKey normalizes it to the empty component)
        (not (isNull contents) && not (isNull other) && other <> contents)
        ==> lazy
            (let k1 = ArtifactCache.unitKey pid unit contents invocation
             let k2 = ArtifactCache.unitKey pid unit other invocation
             k1 <> k2)

    // --- BS2: driver wire ---

    [<Property>]
    let ``BS2 toWire is injective over the driver set`` () =
        let drivers = [ MsBuildInterop; DotNetCli; GuidersBuild; Npm ]
        let wires = drivers |> List.map BuildDriver.toWire
        let injective = List.length (List.distinct wires) = List.length drivers
        let nonEmpty = wires |> List.forall (fun w -> not (System.String.IsNullOrEmpty w))
        injective && nonEmpty
