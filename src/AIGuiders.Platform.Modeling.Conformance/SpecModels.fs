namespace AIGuiders.Platform.Modeling.Conformance

open System.Collections.Generic

// ── Navigation spec shapes (parity: Conformance.Navigation wire records) ──

/// <summary>One navigation conformance vector: id + wire JSON + profile + expect.</summary>
[<CLIMutable>]
type NavigationSpecVector =
    { Id: string
      WireJson: string
      ProfileJson: string
      ExpectJson: string }

/// <summary>Navigation conformance spec document — kind/surface/version + vectors.</summary>
[<CLIMutable>]
type NavigationSpecDocument =
    { Kind: string
      Surface: string
      Version: int
      Source: string
      Vectors: NavigationSpecVector list }

/// <summary>Expected scene shape after navigation walk (parity: NavigationExpectWire).</summary>
[<CLIMutable>]
type NavigationExpectation =
    { NodeCount: int
      Kinds: string list
      ExcludedKinds: string list
      MaxKindCounts: (string * int) list }

module NavigationExpectation =

    /// Parity with NavigationSpecConformance.TryValidateVector checks (pure part).
    let checkKinds (actualKinds: string list) (expect: NavigationExpectation) : string list =
        let actual = Set.ofList actualKinds
        let errors = ResizeArray<string>()

        for kind in expect.Kinds do
            if not (Set.contains kind actual) then
                errors.Add($"expected kind \"{kind}\" missing.")

        for kind in expect.ExcludedKinds do
            if Set.contains kind actual then
                errors.Add($"excluded kind \"{kind}\" present.")

        for (kind, maxCount) in expect.MaxKindCounts do
            let count = actualKinds |> List.filter (fun k -> k = kind) |> List.length
            if count > maxCount then
                errors.Add($"kind \"{kind}\" count {count} exceeds max {maxCount}.")

        List.ofSeq errors

// ── Policy spec shapes (parity: Conformance.Policies wire records) ──

/// <summary>One policy conformance vector: baseline + overlay + expect JSON.</summary>
[<CLIMutable>]
type PolicySpecVector =
    { Id: string
      BaselineJson: string
      OverlayJson: string
      ExpectJson: string }

/// <summary>Policy conformance spec document — policy/semantics + vectors.</summary>
[<CLIMutable>]
type PolicySpecDocument =
    { Kind: string
      Policy: string
      Semantics: string
      Version: int
      Source: string
      Vectors: PolicySpecVector list }

/// <summary>Slash path wire row (parity: SlashPathWire).</summary>
[<CLIMutable>]
type SlashPathRow =
    { Path: string
      CommandId: string }

/// <summary>Binding entry wire row (parity: BindingEntryWire).</summary>
[<CLIMutable>]
type BindingEntryRow =
    { Key: string
      Gesture: string }