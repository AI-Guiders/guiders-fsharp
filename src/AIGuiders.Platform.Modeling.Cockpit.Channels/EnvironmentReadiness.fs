namespace AIGuiders.Platform.Modeling.Cockpit.Channels

open System.Collections.Generic

/// <summary>Stable cell ids for Environment Readiness deck (ADR 0063).</summary>
module EnvironmentReadinessCellIds =
    [<Literal>]
    let DevToolsSection = "environment_dev_tools_section"
    [<Literal>]
    let Agent = "environment_agent"
    [<Literal>]
    let EnvSection = "environment_env_section"
    [<Literal>]
    let AgentNotesFile = "environment_agent_notes_file"
    [<Literal>]
    let AgentNotesCanonPath = "environment_agent_notes_canon_path"
    [<Literal>]
    let NetcoreDbgPath = "environment_netcoredbg_path"
    [<Literal>]
    let CSharpLsp = "environment_csharp_lsp"
    [<Literal>]
    let MarkdownLsp = "environment_markdown_lsp"
    [<Literal>]
    let DotnetSdk = "environment_dotnet_sdk"

/// <summary>C# LSP row mode for ER lamp strip (product-specific messaging).</summary>
[<CLIMutable>]
type EnvironmentReadinessCSharpProbeOptions =
    { InProcessRoslynEnabled: bool
      InProcessRoslynDetail: string }

/// <summary>Env vars snapshot for ER rows (ADR 0023 DAL shape).</summary>
[<CLIMutable>]
type EnvironmentReadinessEnvSnapshot =
    { AgentNotesFile: string
      AgentNotesConfigPath: string
      NetcoreDbgPath: string }

/// <summary>Headless settings slice for Environment Readiness channel.</summary>
[<CLIMutable>]
type EnvironmentReadinessSettings =
    { AgentNotesConfigPath: string }

/// <summary>Channel input: settings + LSP projection from DataBus (ADR 0099).</summary>
[<CLIMutable>]
type EnvironmentReadinessChannelContext =
    { Settings: EnvironmentReadinessSettings
      SolutionPath: string
      IdeHostLsp: AIGuiders.Platform.Modeling.Cockpit.DataBus.IdeHostStateChanged
      IsMcpStdioHost: bool
      ActiveAiProvider: string }

/// <summary>Channel output: lamp strip snapshot (ADR 0023).</summary>
[<CLIMutable>]
type EnvironmentReadinessSnapshot =
    { Rows: AnnunciatorLampItem list }

module EnvironmentReadiness =
    /// Merge extension rows onto a core snapshot (order: core first).
    let mergeExtension (core: EnvironmentReadinessSnapshot) (extensionRows: AnnunciatorLampItem list) =
        if List.isEmpty extensionRows then core
        else { core with Rows = List.append core.Rows extensionRows }