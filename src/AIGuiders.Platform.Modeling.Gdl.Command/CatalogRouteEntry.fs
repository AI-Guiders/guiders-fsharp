namespace AIGuiders.Platform.Modeling.Gdl.Command

open System
open System.Collections.Generic

/// <summary>Resolved catalog row: path + command_id (CIDE quarry, headless).</summary>
type CatalogRouteEntry =
    { Path: string
      CommandId: string
      Help: string
      ArgTailKind: CommandArgTailKind
      Domain: string
      Object: string
      Intent: string
      PathRole: CatalogPathRole
      Group: string option
      ArgTail: string
      ArgPickerChoices: IReadOnlyList<CommandPickerChoice>
      ArgHint: string option
      ArgConstructors: IReadOnlyList<ArgConstructorBinding> }

module CatalogRouteEntry =

    /// <summary>Lower-strip leading slash (C# NormalizePath).</summary>
    let normalizePath (path: string) : string =
        let mutable p = path.Trim()
        if p.StartsWith('/') then p <- p.Substring(1)
        p.Trim()

    /// <summary>Canonical when path equals descriptor path case-insensitively (C# ResolvePathRole).</summary>
    let resolvePathRole (d: CommandDescriptor) (path: string) : CatalogPathRole =
        let same =
            String.Equals(normalizePath path, normalizePath d.Path, StringComparison.OrdinalIgnoreCase)
        if same then CatalogPathRole.Canonical else CatalogPathRole.Alias

    /// <summary>Descriptor → catalog row with explicit path role (C# FromDescriptor overload).</summary>
    let fromDescriptorRole (d: CommandDescriptor) (path: string) (pathRole: CatalogPathRole) : CatalogRouteEntry =
        { Path = path
          CommandId = d.CommandId
          Help = d.Help |> Option.defaultValue ""
          ArgTailKind = CommandArgTailPolicy.kindOf d
          Domain = d.Domain
          Object = d.Object
          Intent = d.Intent
          PathRole = pathRole
          Group = d.Group
          ArgTail = d.ArgTail
          ArgPickerChoices = d.ArgPickerChoices
          ArgHint = d.ArgHint
          ArgConstructors = d.ArgConstructors }

    /// <summary>Descriptor → catalog row; role resolved from path (C# FromDescriptor).</summary>
    let fromDescriptor (d: CommandDescriptor) (path: string) : CatalogRouteEntry =
        fromDescriptorRole d path (resolvePathRole d path)

    let resolvedPickerChoices (e: CatalogRouteEntry) : IReadOnlyList<CommandPickerChoice> =
        e.ArgPickerChoices

    let resolvedConstructors (e: CatalogRouteEntry) : IReadOnlyList<ArgConstructorBinding> =
        e.ArgConstructors

    /// <summary>ADR-0154 DOI triple + path role (C# SemanticFields).</summary>
    let semanticFields (e: CatalogRouteEntry) : CatalogSemanticFields =
        { Domain = e.Domain; Object = e.Object; Intent = e.Intent; PathRole = e.PathRole }