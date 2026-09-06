namespace AIGuiders.Platform.Modeling.Navigation.Policy

open System

/// <summary>Canonicalize one token (helper over RelatedKinds, string arity).</summary>
[<AutoOpen>]
module PresetMergeInternal =
    let canonicalToken (token: string) : string option =
        RelatedKinds.tryCanonicalKind (Some token)

    let canonicalList (tokens: string list option) : string list option =
        match tokens with
        | None -> None
        | Some ts ->
            ts
            |> List.choose canonicalToken
            |> function
                | [] -> None
                | xs -> Some xs

/// <summary>Merges a named preset with host/MCP request overrides (pure).</summary>
[<RequireQualifiedAccess>]
module PresetMerge =
    /// <summary>
    /// Non-null requestInclude/requestExclude override the preset side; when both preset
    /// and request specify exclude, lists are unioned (deduped by canonical kind).
    /// Returns (include, exclude, error). Error is Some only for an unknown preset.
    /// </summary>
    let merge
        (presetName: string option)
        (requestInclude: string list option)
        (requestExclude: string list option)
        : (string list option * string list option * string option) =
        let unknownPreset =
            match presetName with
            | Some p when not (String.IsNullOrWhiteSpace p) ->
                match Presets.tryGet (Some p) with
                | None -> Some $"Неизвестный пресет «%s{p.Trim()}»"
                | Some _ -> None
            | _ -> None

        match unknownPreset with
        | Some err -> (None, None, Some err)
        | None ->
            let presetInclude, presetExclude =
                match presetName with
                | Some p when not (String.IsNullOrWhiteSpace p) ->
                    match Presets.tryGet (Some p) with
                    | Some(def, _) -> (canonicalList def.IncludeKinds, canonicalList def.ExcludeKinds)
                    | None -> (None, None)
                | _ -> (None, None)

            let include =
                match requestInclude with
                | Some ri when ri.Length > 0 -> Some ri
                | _ -> presetInclude

            let exclude =
                match requestExclude, presetExclude with
                | Some re, Some pe when re.Length > 0 && pe.Length > 0 ->
                    let set = System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)

                    for x in pe @ re do
                        match canonicalToken x with
                        | Some c -> set.Add(c) |> ignore
                        | None -> ()

                    set
                    |> Seq.sortWith (fun a b -> String.Compare(a, b, StringComparison.Ordinal))
                    |> Seq.toList
                    |> Some
                | Some re, _ when re.Length > 0 -> Some re
                | _ -> presetExclude

            (include, exclude, None)

/// <summary>Kind filter: non-empty include is a whitelist; exclude subtracts. Unknown tokens ignored.</summary>
[<RequireQualifiedAccess>]
module KindFilter =
    type Filter =
        { Include: string list option
          Exclude: string list }

    let create (includeKinds: string list option) (excludeKinds: string list option) : Filter =
        let includeSet =
            match canonicalList includeKinds with
            | Some s when not (List.isEmpty s) -> Some s
            | _ -> None

        let excludeSet =
            match canonicalList excludeKinds with
            | Some s -> s
            | None -> []

        { Include = includeSet; Exclude = excludeSet }

    /// <summary>Effective include list (None = no whitelist).</summary>
    let effectiveInclude (f: Filter) : string list option =
        f.Include

    /// <summary>Effective exclude list.</summary>
    let effectiveExclude (f: Filter) : string list =
        f.Exclude

    /// <summary>Kind passes the filter (C#-parity semantics).</summary>
    let allows (f: Filter) (kind: string) : bool =
        match f.Include with
        | Some includeSet when not (List.contains kind includeSet) -> false
        | _ -> not (List.contains kind f.Exclude)

/// <summary>Default per-kind caps for related scenes (C# parity).</summary>
[<RequireQualifiedAccess>]
module KindCaps =
    let defaultRelated: (string * int) list =
        [ "same_directory", 4; "same_namespace", 4; "project_peer", 3 ]

open AIGuiders.Platform.Modeling.Navigation

/// <summary>Navigation profile: preset + limits, request overrides applied (pure).</summary>
[<CLIMutable>]
type NavigationProfile =
    { Preset: string option
      MaxRelated: int
      MaxNodes: int
      MaxEdges: int
      WithUsages: bool
      IncludeKinds: string list option
      ExcludeKinds: string list option }

/// <summary>Named profiles + request-time construction (C# parity).</summary>
[<RequireQualifiedAccess>]
module Profile =
    let exploreDefault: NavigationProfile =
        { Preset = Some "explore_default"
          MaxRelated = 24
          MaxNodes = 12
          MaxEdges = 24
          WithUsages = false
          IncludeKinds = None
          ExcludeKinds = None }

    let peersOnly: NavigationProfile =
        { Preset = Some "peers_only"
          MaxRelated = 16
          MaxNodes = 12
          MaxEdges = 24
          WithUsages = false
          IncludeKinds = None
          ExcludeKinds = None }

    /// <summary>Build profile from MCP/CSX explore args (preset + request overrides).</summary>
    let fromExplore
        (preset: string option)
        (maxRelated: int option)
        (requestInclude: string list option)
        (requestExclude: string list option)
        : NavigationProfile =
        let includeK, excludeK, _ = PresetMerge.merge preset requestInclude requestExclude

        { Preset = preset
          MaxRelated =
            match maxRelated with
            | Some mr when mr > 0 -> mr
            | _ -> 24
          MaxNodes = 12
          MaxEdges = 24
          WithUsages = false
          IncludeKinds = includeK
          ExcludeKinds =
            match excludeK with
            | Some ex when ex.Length > 0 -> Some ex
            | _ -> None }

    /// <summary>Scene caps from the profile (C# parity: default kind caps merged in).</summary>
    let toCaps (p: NavigationProfile) : SceneCaps =
        { MaxRelated = p.MaxRelated
          MaxNodes = p.MaxNodes
          MaxEdges = p.MaxEdges
          Preset = p.Preset
          KindCaps = Some(Map.ofList KindCaps.defaultRelated) }