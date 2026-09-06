namespace AIGuiders.Platform.Modeling.Gdl.Parse.Catalog

open System
open System.Collections.Generic
open AIGuiders.Platform.Modeling.Gdl.Command
open AIGuiders.Platform.Modeling.Gdl.Core

/// Parse document → spine payload (CatalogPayload { Planet; Routes }).
/// DOI (Domain/Object/Intent, ADR-0154) comes ONLY from explicit catalog columns —
/// never inferred from the command id shape.
[<RequireQualifiedAccess>]
module CatalogMapping =

    let toRouteEntry (row: CatalogCommandRow) : CatalogRouteEntry =
        let args = row.Columns |> Map.tryFind "args" |> Option.defaultValue ""
        let help = row.Columns |> Map.tryFind "help" |> Option.defaultValue ""

        { Path = CatalogRouteEntry.normalizePath row.Command
          CommandId = row.Command
          Help = help
          ArgTailKind =
            if String.IsNullOrWhiteSpace args then CommandArgTailKind.None
            else CommandArgTailKind.Optional
          Domain = row.Columns |> Map.tryFind "domain" |> Option.defaultValue ""
          Object = row.Columns |> Map.tryFind "object" |> Option.defaultValue ""
          Intent = row.Columns |> Map.tryFind "intent" |> Option.defaultValue ""
          PathRole = CatalogPathRole.Canonical
          Group = row.Columns |> Map.tryFind "group"
          ArgTail = args
          ArgPickerChoices = List<CommandPickerChoice>() :> IReadOnlyList<CommandPickerChoice>
          ArgHint = None
          ArgConstructors = List<ArgConstructorBinding>() :> IReadOnlyList<ArgConstructorBinding> }

    let toPayload (doc: CatalogDocument) : CatalogPayload =
        { Planet = doc.Planet
          Routes = (doc.Commands |> List.map toRouteEntry |> List.toArray) :> IReadOnlyList<CatalogRouteEntry> }
