namespace AIGuiders.Platform.Modeling.Combinations

open System

/// <summary>Documented merge semantics for platform combinators (GUIDERS-ADR-0030).</summary>
type Semantics =
    /// <summary>Field-level overlay: non-null overlay fields win; baseline fills gaps.</summary>
    | FieldOverlay
    /// <summary>Whole-section replace when overlay section is present.</summary>
    | SectionReplace
    /// <summary>Baseline wins on key collision (ship catalog + user additions).</summary>
    | ShipFirst
    /// <summary>Overlay wins on key collision (user hotkey overrides).</summary>
    | OverlayWins

/// <summary>Combines baseline with one overlay layer; policy lives at the call site.</summary>
type Combinator<'T> = 'T -> 'T -> 'T

/// <summary>Ordered fold over materialized layers (GUIDERS-ADR-0030) — pure.</summary>
[<RequireQualifiedAccess>]
module OrderedCombination =
    /// <summary>Fold left: baseline = head, each subsequent layer combines on top.
    /// At least one layer is required (C# parity).</summary>
    let fold (combiner: Combinator<'T>) (layers: 'T list) : 'T =
        match layers with
        | [] -> invalidArg "layers" "At least one layer is required."
        | head :: tail -> List.fold combiner head tail

    /// <summary>Project each layer to an accumulator, then fold with the combiner.</summary>
    let foldLayers
        (project: 'Layer -> 'Accum)
        (combiner: Combinator<'Accum>)
        (seed: 'Accum)
        (layers: 'Layer seq)
        : 'Accum =
        layers |> Seq.map project |> Seq.fold combiner seed