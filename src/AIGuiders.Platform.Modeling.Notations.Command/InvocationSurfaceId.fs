namespace AIGuiders.Platform.Modeling.Notations.Command

/// <summary>Stable invocation surface id (ADR-0009). Not notation dialect, not cockpit UI.</summary>
type InvocationSurfaceId =
    | SlashBar
    | SlashComposer
    | ConsoleFilter
    | Palette
    | EditorCcl
    | EditorInline
    | CommandBar
    | BindingHotkey
    | MelodyChord
    | McpTool
    | CitizenIntent
    | Custom of wire: string

module InvocationSurfaceId =

    let toWire (id: InvocationSurfaceId) : string =
        match id with
        | SlashBar -> "slash.bar"
        | SlashComposer -> "slash.composer"
        | ConsoleFilter -> "console.filter"
        | Palette -> "palette"
        | EditorCcl -> "editor-ccl"
        | EditorInline -> "editor-inline"
        | CommandBar -> "command-bar"
        | BindingHotkey -> "binding.hotkey"
        | MelodyChord -> "melody.chord"
        | McpTool -> "mcp.tool"
        | CitizenIntent -> "citizen.intent"
        | Custom wire -> wire.Trim()

    let tryParse (wire: string) : InvocationSurfaceId option =
        if System.String.IsNullOrWhiteSpace wire then None
        else
            let key = wire.Trim().ToLowerInvariant()
            match key with
            | "slash.bar" -> Some SlashBar
            | "slash.composer" -> Some SlashComposer
            | "console.filter" -> Some ConsoleFilter
            | "palette" -> Some Palette
            | "editor-ccl" -> Some EditorCcl
            | "ccl.filter" -> Some EditorCcl
            | "editor-inline" -> Some EditorInline
            | "command-bar" -> Some CommandBar
            | "binding.hotkey" -> Some BindingHotkey
            | "melody.chord" -> Some MelodyChord
            | "mcp.tool" -> Some McpTool
            | "citizen.intent" -> Some CitizenIntent
            | _ -> Some(Custom key)
