namespace AIGuiders.Platform.Modeling.Documentation.Correspondence.Tests

open FsCheck
open FsCheck.FSharp
open FsCheck.Xunit
open AIGuiders.Platform.Modeling.Paths
open AIGuiders.Platform.Modeling.Documentation.Correspondence

/// <summary>
/// BracketWire + CorrespondenceRelationKind as executable laws (GUIDERS-FSHARP-ADR-0010).
/// Profiles are intentionally distinct: build/tryParseBracket is the legacy F/M/L profile;
/// buildCodeEdit emits the Kind: canon wire, resolved by Execution's boundary parser —
/// hence a shape law, not a round-trip, for buildCodeEdit.
/// </summary>
module CorrespondenceProperties =

    // --- generators ---

    let genPath : Gen<string> =
        Gen.nonEmptyListOf (Gen.elements [ 'a'; 'b'; 'x'; '1'; '9'; '/'; '\\' ])
        |> Gen.map (fun cs -> System.String(Array.ofList cs))
        |> Gen.filter (fun p -> LogicalPathOps.normalize p <> "")

    let genWord : Gen<string> =
        Gen.nonEmptyListOf (Gen.elements ([ 'a' .. 'z' ] @ [ '0' .. '9' ]))
        |> Gen.map (fun cs -> System.String(Array.ofList cs))

    let genLine : Gen<int> = Gen.choose (1, 10_000)

    let genKind : Gen<CorrespondenceRelationKind> =
        Gen.elements
            [ CorrespondenceRelationKind.Documents
              CorrespondenceRelationKind.ImplementsObligation
              CorrespondenceRelationKind.Related
              CorrespondenceRelationKind.Constrains
              CorrespondenceRelationKind.Normates
              CorrespondenceRelationKind.VerifiedBy ]

    let opt (g: Gen<'a>) : Gen<'a option> =
        Gen.frequency [ (3, Gen.map Some g); (1, Gen.constant None) ]

    /// Separators the keySep regex is required to tolerate between wire keys.
    let genSepVariant : Gen<string> =
        Gen.elements [ "; "; " "; "\t; "; ";   "; " ; " ]

    // --- legacy profile: build ∘ tryParseBracket round-trip ---

    /// Member wins: no L keys on the wire, so lines are lost by design (C# parity).
    [<Property>]
    let ``BRACKET round-trip: member wire loses lines by design`` () =
        Prop.forAll (Arb.fromGen genPath) (fun file ->
            Prop.forAll (Arb.fromGen genWord) (fun memberKey ->
                Prop.forAll (Arb.fromGen genLine) (fun line ->
                    match BracketWire.tryParseBracket (BracketWire.build file (Some line) (Some line) (Some memberKey)) with
                    | Some(f, None, None, Some m) ->
                        f = LogicalPathOps.normalize file && m = memberKey
                    | _ -> false)))

    /// Line span: L2 collapses to L on parse (C# parity); single line carries no L2.
    [<Property>]
    let ``BRACKET round-trip: line span parses back, L2 collapses to L`` () =
        Prop.forAll (Arb.fromGen genPath) (fun file ->
            Prop.forAll (Arb.fromGen (Gen.choose (1, 9999))) (fun ls ->
                Prop.forAll (Arb.fromGen (Gen.choose (ls, ls + 500))) (fun le ->
                    match BracketWire.tryParseBracket (BracketWire.build file (Some ls) (Some le) None) with
                    | Some(f, Some a, Some b, None) ->
                        f = LogicalPathOps.normalize file && a = ls && b = ls
                    | _ -> false)))

    /// No line, no member: bare file wire round-trips to the normalized file alone.
    [<Property>]
    let ``BRACKET round-trip: bare file wire yields normalized file only`` () =
        Prop.forAll (Arb.fromGen genPath) (fun file ->
            match BracketWire.tryParseBracket (BracketWire.build file None None None) with
            | Some(f, None, None, None) -> f = LogicalPathOps.normalize file
            | _ -> false)

    /// Whitespace invariance: any tolerated separator between keys parses identically
    /// to the canonical "; " form of the same keys (compared against the canonical form,
    /// not against build — member wins suppresses L there by design).
    [<Property>]
    let ``BRACKET whitespace invariance: separator variants parse identically`` () =
        Prop.forAll (Arb.fromGen genPath) (fun file ->
            Prop.forAll (Arb.fromGen genWord) (fun memberKey ->
                Prop.forAll (Arb.fromGen genLine) (fun line ->
                    Prop.forAll (Arb.fromGen (Gen.two genSepVariant)) (fun (s1, s2) ->
                        let norm = LogicalPathOps.normalize file
                        let canonBody = $"F:{norm}; M:{memberKey}; L:{line}"
                        let variantBody = $"F:{norm}{s1}M:{memberKey}{s2}L:{line}"
                        BracketWire.tryParseBracket ("[" + variantBody + "]")
                        = BracketWire.tryParseBracket ("[" + canonBody + "]")))))

    /// Totality: parse never throws; Some implies a non-empty normalized file.
    [<Property>]
    let ``BRACKET totality: parse never throws, Some implies normalized non-empty file`` () =
        Prop.forAll (Arb.fromGen (Gen.listOf (Gen.elements [ 'a'; 'F'; 'M'; 'L'; ':'; ';'; '['; ']'; ' '; '\\' ]))) (fun s ->
            match BracketWire.tryParseBracket (System.String(Array.ofList s)) with
            | Some(f, _, _, _) ->
                f.Length > 0 && not (f.Contains('\\')) && f = f.Trim()
            | None -> true)

    // --- Kind: canon profile — shape law (resolved by Execution's boundary parser) ---

    [<Property>]
    let ``CODEEDIT shape: Kind first, File normalized, Member/Line per options`` () =
        Prop.forAll (Arb.fromGen genPath) (fun file ->
            Prop.forAll (Arb.fromGen (opt genWord)) (fun memberKey ->
                Prop.forAll (Arb.fromGen (opt genLine)) (fun line ->
                    let wire = BracketWire.buildCodeEdit file memberKey line
                    let norm = LogicalPathOps.normalize file
                    let startsOk = wire.StartsWith("[Kind:CodeEdit; File:" + norm)
                    let memberOk =
                        match memberKey with
                        | Some m when not (System.String.IsNullOrWhiteSpace m) -> wire.Contains("; Member:" + m)
                        | _ -> not (wire.Contains("Member:"))
                    let lineOk =
                        match line with
                        | Some ln -> wire.Contains("; Line:" + string ln)
                        | None -> not (wire.Contains("Line:"))
                    let endsOk = wire.EndsWith("]")
                    startsOk && memberOk && lineOk && endsOk)))

    // --- CorrespondenceRelationKind: wire bijection ---

    [<Property>]
    let ``KIND bijection: tryParse . toWire = Some, wire names distinct`` () =
        let all =
            [ CorrespondenceRelationKind.Documents
              CorrespondenceRelationKind.ImplementsObligation
              CorrespondenceRelationKind.Related
              CorrespondenceRelationKind.Constrains
              CorrespondenceRelationKind.Normates
              CorrespondenceRelationKind.VerifiedBy ]
        let wires = all |> List.map CorrespondenceRelationKind.toWire
        let distinct = List.distinct wires = wires
        let roundTrips = all |> List.forall (fun k -> CorrespondenceRelationKind.tryParse (CorrespondenceRelationKind.toWire k) = Some k)
        distinct && roundTrips

    [<Property>]
    let ``KIND parse total: unknown wire is None, case/trim tolerated`` () =
        Prop.forAll (Arb.fromGen genWord) (fun noise ->
            let unknown = "not-a-kind-" + noise
            CorrespondenceRelationKind.tryParse unknown = None
            && CorrespondenceRelationKind.tryParse "  NORMATES " = Some CorrespondenceRelationKind.Normates)
