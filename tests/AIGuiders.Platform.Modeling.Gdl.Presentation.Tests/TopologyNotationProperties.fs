namespace AIGuiders.Platform.Modeling.Gdl.Presentation.Tests

open FsCheck
open FsCheck.FSharp
open FsCheck.Xunit
open AIGuiders.Platform.Modeling.Gdl.Presentation

/// <summary>
/// TopologyNotation.parse as executable laws (GUIDERS-FSHARP-ADR-0010).
/// The parser had no property coverage before this file — these are additions, not migrations.
/// Grammar (from source): 'single'; one OneOf group; two groups (two single-channel splits,
/// or exactly one OneOf); three groups with Pfd/Forward/Mfd role assignment.
/// </summary>
module TopologyNotationProperties =

    // --- generators ---

    let genChannel : Gen<string> =
        Gen.nonEmptyListOf (Gen.elements ([ 'a' .. 'z' ] @ [ '0' .. '9' ] @ [ '_'; '-' ]))
        |> Gen.map (fun cs -> System.String(Array.ofList cs))

    let genSplitGroup : Gen<string> =
        genChannel |> Gen.map (fun c -> $"({c})")

    let genOneOfGroup : Gen<string> =
        gen {
            let! n = Gen.choose (2, 4)
            let! chans = Gen.listOfLength n genChannel
            return "(" + System.String.Join("/", chans) + ")"
        }

    let genValidGroup : Gen<string> =
        Gen.frequency [ (1, genSplitGroup); (1, genOneOfGroup) ]

    /// Valid wires with their expected shape (arrangement, host count).
    let genValidWire : Gen<string * TopologyArrangement * int> =
        gen {
            let! pick = Gen.choose (1, 6)
            match pick with
            | 1 -> return "single", TopologyArrangement.SingleSurfaceCompositional, 0
            | 2 ->
                let! g = genOneOfGroup
                return g, TopologyArrangement.SingleHostOneOf, 1
            | 3 ->
                let! a = genSplitGroup
                let! b = genSplitGroup
                return a + b, TopologyArrangement.MultiHost, 2
            | 4 ->
                let! a = genOneOfGroup
                let! b = genSplitGroup
                return a + b, TopologyArrangement.MultiHost, 2
            | 5 ->
                let! a = genSplitGroup
                let! b = genOneOfGroup
                return a + b, TopologyArrangement.MultiHost, 2
            | _ ->
                let! g1 = genValidGroup
                let! g2 = genValidGroup
                let! g3 = genValidGroup
                return g1 + g2 + g3, TopologyArrangement.MultiHost, 3
        }

    let topologyShape (t: PresentationTopology) =
        t.Arrangement,
        t.Hosts
        |> List.map (fun h -> h.Role, h.Compose, h.ChannelStack, h.ActiveChannel)

    // --- laws ---

    /// Valid wires parse Ok with the expected arrangement and host count.
    [<Property>]
    let ``TOPO valid: shape matches the grammar arm`` () =
        Prop.forAll (Arb.fromGen genValidWire) (fun (wire, arrangement, hostCount) ->
            let r = TopologyNotation.parse wire
            r.IsSuccess
            && (match r.Topology with
                | Some t ->
                    t.Arrangement = arrangement
                    && t.HostCount = hostCount
                    && t.SourceWire = wire
                | None -> false))

    /// Structural invariants of every parsed host list.
    [<Property>]
    let ``TOPO hosts: indices 0..n-1, active = head stack, OneOf needs 2+ channels`` () =
        Prop.forAll (Arb.fromGen genValidWire) (fun (wire, _, _) ->
            match (TopologyNotation.parse wire).Topology with
            | None -> true
            | Some t ->
                let indicesOk = t.Hosts |> List.mapi (fun i h -> i, h) |> List.forall (fun (i, h) -> h.HostIndex = i)
                let activeOk = t.Hosts |> List.forall (fun h -> h.ActiveChannel = List.head h.ChannelStack)
                let oneOfOk = t.Hosts |> List.forall (fun h -> h.Compose <> ZoneComposeKind.OneOf || List.length h.ChannelStack >= 2)
                let idOk = t.Hosts |> List.forall (fun h -> h.HostId.Length > 0)
                indicesOk && activeOk && oneOfOk && idOk)

    /// Case and surrounding whitespace do not change the parsed topology (source wire does change — excluded).
    [<Property>]
    let ``TOPO invariance: case and surrounding whitespace preserve the topology`` () =
        Prop.forAll (Arb.fromGen genValidWire) (fun (wire, _, _) ->
            let base' = (TopologyNotation.parse wire).Topology |> Option.map topologyShape
            let upper = (TopologyNotation.parse (wire.ToUpper())).Topology |> Option.map topologyShape
            let padded = (TopologyNotation.parse ("  " + wire + " ")).Topology |> Option.map topologyShape
            base' = upper && base' = padded)

    /// Mixing '+' and '/' inside one group is rejected.
    [<Property>]
    let ``TOPO mixed: '+' with '/' in one group fails`` () =
        Prop.forAll (Arb.fromGen (Gen.three genChannel)) (fun (a, b, c) ->
            let r = TopologyNotation.parse $"({a}/{b}+{c})"
            (not r.IsSuccess) && r.Error.Value.Contains("Mixed"))

    /// A single Split group is spatial split — rejected with guidance.
    [<Property>]
    let ``TOPO single-split: one dedicated group alone fails`` () =
        Prop.forAll (Arb.fromGen genChannel) (fun c ->
            let r = TopologyNotation.parse $"({c})"
            (not r.IsSuccess) && r.Error.Value.Contains("spatial split"))

    /// Totality: parse never throws on arbitrary junk; Ok implies 0–3 hosts; blank fails.
    [<Property>]
    let ``TOPO totality: any wire yields Ok or Fail, Ok has 0-3 hosts, blank fails`` () =
        Prop.forAll (Arb.fromGen (Gen.listOf (Gen.elements [ '('; ')'; '+'; '/'; 'a'; 'b'; '1'; '_'; '-'; ' ' ]))) (fun chars ->
            let wire = System.String(Array.ofList chars)
            let r = TopologyNotation.parse wire
            if System.String.IsNullOrWhiteSpace wire then
                (not r.IsSuccess) && r.Error.Value.Contains("empty")
            else
                match r.Topology with
                | Some t -> t.HostCount >= 0 && t.HostCount <= 3
                | None -> true)
