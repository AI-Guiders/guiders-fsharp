namespace AIGuiders.Platform.Modeling.Ide.Session

type DotNetLanguage =
    | CSharp
    | FSharp

type DotNetProject = { Language: DotNetLanguage }

type NodeProject = { AnchorPath: string }

type GdlProject = { ProjectFile: string }

type PlanetProject =
    { LanguageId: string
      AnchorPath: string }

/// Standalone document unit (md/json/toml/yaml) fed by file-graph providers.
type DocProject = { Extension: string }

type ProjectKind =
    | DotNet of DotNetProject
    | Node of NodeProject
    | Gdl of GdlProject
    | Planet of PlanetProject
    | Doc of DocProject

type ProjectNode =
    { Id: ProjectId
      Kind: ProjectKind
      AbsolutePath: string
      Phase: LifecyclePhase
      Capabilities: CapabilityNode list }

module ProjectNode =
    let create id kind absolutePath capabilities =
        { Id = id
          Kind = kind
          AbsolutePath = absolutePath
          Phase = Unloaded
          Capabilities = capabilities }
