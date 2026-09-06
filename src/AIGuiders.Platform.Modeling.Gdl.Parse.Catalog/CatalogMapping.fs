namespace AIGuiders.Platform.Modeling.Gdl.Parse.Catalog

open System
open System.Collections.Generic
open AIGuiders.Platform.Modeling.Gdl.Command
open AIGuiders.Platform.Modeling.Gdl.Core

/// Parse document → spine payload (CatalogPayload { Planet; Routes }).
/// DOI triple convention: `domain.object.intent` (ADR-0154) — 3/2/1 segment fallback.
[<RequireQualifiedAccess>]
module CatalogMapping =

    let toRouteEntry (row: CatalogCommandRow) : CatalogRouteEntry =
        let doi = row.Command.Split('.') |> Array.map (fun s -> s.Trim())
        let domain = if doi.Length >= 1 then doi.[0] else ""
        let ``object`` = if doi.Length >= 2 then doi.[1] else ""
        let intent = if doi.Length >= 3 then doi.[2] else ""
        let args = row.Columns |> Map.tryFind "args" |> Option.defaultValue ""
        let help = row.Columns |> Map.tryFind "help" |> Option.defaultValue ""

        { Path = CatalogRouteEntry.normalizePath row.Command
          CommandId = row.Command
          Help = help
          ArgTailKind =
            if String.IsNullOrWhiteSpace args then CommandArgTailKind.None
            else CommandArgTailKind.Optional
          Domain = domain
          Object = ``object``
          Intent = intent
          PathRole = CatalogPathRole.Canonical
          Group = row.Columns |> Map.tryFind "group"
          ArgTail = args
          ArgPickerChoices = List<CommandPickerChoice>() :> IReadOnlyList<CommandPickerChoice>
          ArgHint = None
          ArgConstructors = List<ArgConstructorBinding>() :> IReadOnlyList<ArgConstructorBinding> }

    let toPayload (doc: CatalogDocument) : CatalogPayload =
        { Planet = doc.Planet
          Routes = (doc.Commands |> List.map toRouteEntry |> List.toArray) :> IReadOnlyList<CatalogRouteEntry> }
