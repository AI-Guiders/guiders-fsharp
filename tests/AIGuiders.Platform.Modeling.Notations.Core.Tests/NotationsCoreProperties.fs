namespace AIGuiders.Platform.Modeling.Notations.Core.Tests

open FsCheck
open FsCheck.FSharp
open FsCheck.Xunit
open AIGuiders.Platform.Modeling.Notations

/// <summary>
/// Notations.Core as executable laws (GUIDERS-FSHARP-ADR-0010):
/// NotationListSplit — byte-conserving recomposition, shielding, idempotence on parts;
/// NotationKvPair — first-sign parse/construct round-trip, error classification.
/// </summary>
module NotationsCoreProperties =

    // --- generators ---

    let letters = [ 'a' .. 'z' ] @ [ '0' .. '9' ]

    let str (cs: char list) : string = System.String(Array.ofList cs)

    let genWord : Gen<string> = Gen.listOf (Gen.elements letters) |> Gen.map str

    let genNonEmptyWord : Gen<string> =
        Gen.nonEmptyListOf (Gen.elements letters) |> Gen.map str

    /// Pairwise-distinct separator and brackets, drawn from fixed pools so generated
    /// texts can contain all of them.
    let genSepAndBrackets : Gen<char * char * char> =
        Gen.elements [ ('=', '[', ']'); (',', '(', ')'); ('|', '{', '}') ]

    let genSign : Gen<char> = Gen.elements [ '='; ':'; '|' ]

    let genTextWith (chars: char list) : Gen<string> =
        Gen.listOf (Gen.elements chars) |> Gen.map str

    // --- NotationListSplit laws ---

    /// Split loses no bytes: joining the parts with the separator reproduces the input.
    [<Property>]
    let ``LIST recomposition: join . split = id`` () =
        Prop.forAll (Arb.fromGen genSepAndBrackets) (fun (sep, o, c) ->
            Prop.forAll (Arb.fromGen (genTextWith (letters @ [ sep; o; c; ' ' ]))) (fun text ->
                let parts = NotationListSplit.splitTopLevel text sep o c
                System.String.Join(string sep, parts) = text))

    /// Shielding: a separator inside brackets never splits the text.
    [<Property>]
    let ``LIST shielding: bracketed separator stays inside one part`` () =
        Prop.forAll (Arb.fromGen genSepAndBrackets) (fun (sep, o, c) ->
            Prop.forAll (Arb.fromGen genWord) (fun p1 ->
                Prop.forAll (Arb.fromGen genWord) (fun p2 ->
                    Prop.forAll (Arb.fromGen (genTextWith (letters @ [ sep ]))) (fun inner ->
                        let protectedText = str [ o ] + p1 + string sep + inner + p2 + str [ c ]
                        let parts = NotationListSplit.splitTopLevel protectedText sep o c
                        List.length parts = 1))))

    /// Idempotence on parts: no part of a split contains a top-level separator,
    /// so re-splitting any part yields the part alone.
    [<Property>]
    let ``LIST idempotent on parts: split(part) = [part]`` () =
        Prop.forAll (Arb.fromGen genSepAndBrackets) (fun (sep, o, c) ->
            Prop.forAll (Arb.fromGen (genTextWith (letters @ [ sep; o; c; ' ' ]))) (fun text ->
                let parts = NotationListSplit.splitTopLevel text sep o c
                parts |> List.forall (fun p -> NotationListSplit.splitTopLevel p sep o c = [ p ])))

    /// Constructive round-trip: known parts without top-level separators recombine exactly.
    [<Property>]
    let ``LIST constructive: split(p1 + sep + p2) = [p1; p2] for separator-free parts`` () =
        Prop.forAll (Arb.fromGen genSepAndBrackets) (fun (sep, o, c) ->
            Prop.forAll (Arb.fromGen (Gen.two genWord)) (fun (p1, p2) ->
                let parts = NotationListSplit.splitTopLevel (p1 + string sep + p2) sep o c
                parts = [ p1; p2 ]))

    // --- NotationKvPair laws ---

    /// Parse/construct round-trip: key without the sign reconstructs exactly (after trim).
    [<Property>]
    let ``KV round-trip: trySplitFirst(k + sign + v) = Ok(k, v) for sign-free k`` () =
        Prop.forAll (Arb.fromGen genSign) (fun sign ->
            Prop.forAll (Arb.fromGen genNonEmptyWord) (fun k ->
                Prop.forAll (Arb.fromGen (genTextWith (letters @ [ sign ]))) (fun v ->
                    match NotationKvPair.trySplitFirst (k + string sign + v) sign with
                    | Ok kv -> kv.Key = k && kv.Sign = sign && kv.Value = v.Trim()
                    | Error e -> failwith $"expected Ok, got: {e}")))

    /// First-sign-only: everything after the first sign is value, signs included.
    [<Property>]
    let ``KV first-sign-only: value may contain more signs`` () =
        Prop.forAll (Arb.fromGen genSign) (fun sign ->
            Prop.forAll (Arb.fromGen genNonEmptyWord) (fun k ->
                Prop.forAll (Arb.fromGen (Gen.two genWord)) (fun (v1, v2) ->
                    let expected = v1 + string sign + v2
                    match NotationKvPair.trySplitFirst (k + string sign + expected) sign with
                    | Ok kv -> kv.Key = k && kv.Value = expected
                    | Error e -> failwith $"expected Ok, got: {e}")))

    /// Error classification: empty/whitespace segment, absent sign, sign at position zero.
    [<Property>]
    let ``KV errors: empty segment or sign missing/at zero is Error`` () =
        Prop.forAll (Arb.fromGen genSign) (fun sign ->
            Prop.forAll (Arb.fromGen genWord) (fun rest ->
                let emptyErr =
                    match NotationKvPair.trySplitFirst "   " sign with
                    | Error e -> e.Contains("Empty")
                    | Ok _ -> false
                let signFree = rest.Replace(string sign, "")
                let missingErr =
                    // an empty/whitespace remainder is the "Empty segment" error class — still an Error
                    match NotationKvPair.trySplitFirst signFree sign with
                    | Error e -> signFree.Trim().Length = 0 || e.Contains("Missing KV sign")
                    | Ok _ -> false
                let zeroErr =
                    match NotationKvPair.trySplitFirst (string sign + rest) sign with
                    | Error e -> e.Contains("Missing KV sign")
                    | Ok _ -> false
                emptyErr && missingErr && zeroErr))

    /// Characterization: key is a non-empty sign-free prefix of the trimmed input;
    /// value is its trimmed suffix (spaces between key and sign are legal and get trimmed away).
    [<Property>]
    let ``KV characteristic: Ok implies trimmed prefix key and trimmed suffix value`` () =
        Prop.forAll (Arb.fromGen genSign) (fun sign ->
            Prop.forAll (Arb.fromGen (genTextWith (letters @ [ sign; ' ' ]))) (fun s ->
                match NotationKvPair.trySplitFirst s sign with
                | Ok kv ->
                    let trimmed = s.Trim()
                    not (System.String.IsNullOrWhiteSpace kv.Key)
                    && not (kv.Key.Contains(string sign))
                    && trimmed.StartsWith(kv.Key)
                    && trimmed.EndsWith(kv.Value)
                | Error _ -> true))
