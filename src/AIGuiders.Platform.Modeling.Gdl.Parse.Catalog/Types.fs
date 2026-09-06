namespace AIGuiders.Platform.Modeling.Gdl.Parse.Catalog

open AIGuiders.Platform.Modeling.Gdl.Authoring

type CatalogDefaults =
    { VariableKind: string option
      CommandScope: string option
      CommandSurfaces: string list
      GrammarKeyboardBinding: string option
      GrammarKeyboardMelody: string option
      BindingChordRoot: string option
      CommandFlavor: string option }

type CatalogChannel =
    { Surface: string
      Sub: string option
      PlanetId: string option
      CommandGrammar: string option
      ArgumentGrammar: string option }

type CatalogVariable = { Name: string; Kind: string option }

type CatalogHelp = { Target: string; Field: string; Text: string }

type CatalogPhrase = { Name: string; Phrase: string }

type CatalogProfileEntry = { Arg: string; Entry: string; Ref: string }

type CatalogProfile =
    { Name: string
      Entries: CatalogProfileEntry list
      BundleSource: string option }

type CatalogCommandRow =
    { Command: string
      Columns: Map<string, string> }

type CatalogBindingRow = { Gesture: string; Command: string; Role: string option }

type CatalogMelodyRow = { Slug: string; Command: string }

type CatalogMcpRow = { Command: string; Expose: string }

type CatalogDocument =
    { Planet: string
      Imports: string list
      Defaults: CatalogDefaults
      Channels: CatalogChannel list
      Variables: CatalogVariable list
      Helps: CatalogHelp list
      Phrases: CatalogPhrase list
      Profiles: CatalogProfile list
      Commands: CatalogCommandRow list
      Bindings: CatalogBindingRow list
      Melodies: CatalogMelodyRow list
      Mcp: CatalogMcpRow list
      Executors: Map<string, string> }

type CatalogParseResult =
    { Document: CatalogDocument option
      Diagnostics: AuthoringDiagnostic list }

module CatalogDocument =

    let empty : CatalogDocument =
        { Planet = ""
          Imports = []
          Defaults =
            { VariableKind = None
              CommandScope = None
              CommandSurfaces = []
              GrammarKeyboardBinding = None
              GrammarKeyboardMelody = None
              BindingChordRoot = None
              CommandFlavor = None }
          Channels = []
          Variables = []
          Helps = []
          Phrases = []
          Profiles = []
          Commands = []
          Bindings = []
          Melodies = []
          Mcp = []
          Executors = Map.empty }
