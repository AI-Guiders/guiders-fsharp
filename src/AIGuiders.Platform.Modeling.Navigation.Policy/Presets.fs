namespace AIGuiders.Platform.Modeling.Navigation.Policy

open System

/// <summary>Preset definition: include whitelist / exclude blacklist (both optional).</summary>
[<CLIMutable>]
type PresetDefinition =
    { IncludeKinds: string list option
      ExcludeKinds: string list option }

/// <summary>Named navigation presets — merge policy SSOT (GUIDERS-ADR-0033).
/// Pure data + pure rules; JSON presentation of the catalog is an Execution concern.</summary>
[<RequireQualifiedAccess>]
module Presets =
    let private catalog: (string * PresetDefinition) list =
        [ "peers_only",
          { IncludeKinds = Some [ RelatedKinds.PartialPeer; RelatedKinds.ProjectPeer ]; ExcludeKinds = None }
          "no_namespace_noise",
          { IncludeKinds = None
            ExcludeKinds = Some [ RelatedKinds.SameNamespace; RelatedKinds.SameDirectory ] }
          "tests_and_peers",
          { IncludeKinds =
              Some [ RelatedKinds.PartialPeer; RelatedKinds.ProjectPeer; RelatedKinds.TestCounterpart ]
            ExcludeKinds = None }
          "structure_only",
          { IncludeKinds =
              Some
                  [ RelatedKinds.PartialPeer
                    RelatedKinds.ProjectPeer
                    RelatedKinds.XamlCodeBehindPair
                    RelatedKinds.SameDirectory ]
            ExcludeKinds = None }
          "explore_default",
          { IncludeKinds = None; ExcludeKinds = Some [ RelatedKinds.ProjectPeer ] } ]

    /// <summary>
    /// No-filter definition (+true) when presetId is null/empty; found definition otherwise.
    /// None when the preset name is unknown.
    /// </summary>
    let tryGet (presetId: string option) : (PresetDefinition * bool) option =
        match presetId with
        | None -> Some({ IncludeKinds = None; ExcludeKinds = None }, true)
        | Some p when String.IsNullOrWhiteSpace p -> Some({ IncludeKinds = None; ExcludeKinds = None }, true)
        | Some p ->
            let key = p.Trim()

            catalog
            |> List.tryFind (fun (name, _) -> String.Equals(name, key, StringComparison.Ordinal))
            |> Option.map (fun (_, def) -> (def, true))

    /// <summary>Kind passes the preset filter: empty/no preset passes; unknown preset denies.</summary>
    let allowsKind (presetId: string option) (kind: string) : bool =
        match tryGet presetId with
        | None -> false
        | Some(def, _) ->
            let deniedByWhitelist =
                match def.IncludeKinds with
                | Some include when include.Length > 0 -> not (List.contains kind include)
                | _ -> false

            if deniedByWhitelist then
                false
            else
                match def.ExcludeKinds with
                | Some exclude when exclude.Length > 0 -> not (List.contains kind exclude)
                | _ -> true

    /// <summary>Preset names in catalog order (discovery helper).</summary>
    let names: string list = catalog |> List.map fst