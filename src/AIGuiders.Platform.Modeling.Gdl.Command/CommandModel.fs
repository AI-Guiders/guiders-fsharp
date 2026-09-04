namespace AIGuiders.Platform.Modeling.Gdl.Command

open System
open System.Collections.Generic

/// <summary>Arg tail policy after canonical slash path (CIDE ADR-0150).</summary>
type CommandArgTailKind =
    | None = 0
    | Optional = 1
    | Required = 2
    /// <summary>Forge extension: picker:id — clients without picker UI degrade to Optional.</summary>
    | Picker = 3
    /// <summary>Implicit editor selection span (FORGE-ADR-0064).</summary>
    | ImplicitSelection = 4
    /// <summary>Implicit or tail-parsed 1-based line range (CIDE ADR-0081).</summary>
    | ImplicitLineRange = 5

type CommandPickerChoiceKind =
    | Value = 0
    | Constructor = 1

/// <summary>Picker row for the arg-tail menu.</summary>
type CommandPickerChoice =
    { Value: string
      Label: string option
      Hint: string option
      Kind: CommandPickerChoiceKind }

/// <summary>Virtual arg-entry row that opens a value constructor tree (GUIDERS-ADR-0035).</summary>
type ArgConstructorBinding =
    { ConstructorId: string
      Label: string
      Hint: string option }

/// <summary>Arg-menu entry kind from .catalog profiles (GUIDERS-ADR-0047 §8).</summary>
type ArgTailEntryKind =
    | Preset
    | Constructor
    | FreeText
    | PickerForSlot

type ArgTailMenuEntry =
    { Arg: string
      Kind: ArgTailEntryKind
      Ref: string }

/// <summary>Structured arg-menu from .catalog profiles (GUIDERS-ADR-0047 §8).</summary>
type ArgTailProfile =
    { Name: string
      Menu: IReadOnlyList<ArgTailMenuEntry> }

/// <summary>Canonical vs alias path (CIDE ADR-0154 elision).</summary>
type CatalogPathRole =
    | Canonical = 0
    | Alias = 1

/// <summary>ADR-0154 domain · object · intent triple.</summary>
type CatalogSemanticFields =
    { Domain: string
      Object: string
      Intent: string
      PathRole: CatalogPathRole }
    member this.DomainOmittedInPath =
        this.PathRole = CatalogPathRole.Alias && not (String.IsNullOrEmpty this.Domain)

type CommandDocumentFormat =
    | Json = 0
    | Toml = 1
    | Xml = 2

/// <summary>
/// Cross-product slash command descriptor (Forge capabilities + CIDE TOML + platform index).
/// ADR-0154 DOI + ADR-0150 arg_tail. ArgumentNotationProfile (Notations.Argument wire) stays execution-side.
/// </summary>
type CommandDescriptor =
    { Domain: string
      Object: string
      Intent: string
      CommandId: string
      Path: string
      PathAliases: IReadOnlyList<string>
      Help: string option
      Group: string option
      ArgTail: string
      ArgHint: string option
      ArgPickerChoices: IReadOnlyList<CommandPickerChoice>
      ArgConstructors: IReadOnlyList<ArgConstructorBinding>
      Surfaces: IReadOnlyList<string>
      /// <summary>Catalog scope tags — empty = all scopes (GUIDERS-ADR-0044). Not invoker surfaces.</summary>
      Scope: IReadOnlyList<string>
      RequiredCapabilities: IReadOnlyList<string>
      Tier: string option
      PluginId: string option
      RequiresDestructiveConfirm: bool }

module CommandDescriptor =

    /// <summary>ArgTail wire default (C# init "optional").</summary>
    let DefaultArgTail = "optional"

    /// <summary>Canonical path + non-empty aliases (C# AllPaths).</summary>
    let allPaths (d: CommandDescriptor) : IReadOnlyList<string> =
        seq {
            yield d.Path
            for alias in d.PathAliases do
                if not (String.IsNullOrWhiteSpace alias) then yield alias
        }
        |> Seq.toArray
        :> IReadOnlyList<string>