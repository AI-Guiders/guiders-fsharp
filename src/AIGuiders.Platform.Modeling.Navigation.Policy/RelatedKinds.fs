namespace AIGuiders.Platform.Modeling.Navigation.Policy

open System

/// <summary>Canonical related-kind tokens for navigation scenes (GUIDERS-ADR-0033).</summary>
[<RequireQualifiedAccess>]
module RelatedKinds =
    [<Literal>]
    let PartialPeer = "partial_peer"
    [<Literal>]
    let ProjectPeer = "project_peer"
    [<Literal>]
    let XamlCodeBehindPair = "xaml_codebehind_pair"
    [<Literal>]
    let TestCounterpart = "test_counterpart"
    [<Literal>]
    let SameNamespace = "same_namespace"
    [<Literal>]
    let SameDirectory = "same_directory"

    let all: string list =
        [ PartialPeer; ProjectPeer; XamlCodeBehindPair; TestCounterpart; SameNamespace; SameDirectory ]

    /// <summary>Canonical kind name or None when the token is unknown (case-insensitive).</summary>
    let tryCanonicalKind (token: string option) : string option =
        match token with
        | None -> None
        | Some t when String.IsNullOrWhiteSpace t -> None
        | Some t ->
            let s = t.Trim()
            all
            |> List.tryFind (fun k -> String.Equals(k, s, StringComparison.OrdinalIgnoreCase))