namespace AIGuiders.Platform.Modeling.Notations.Command

open AIGuiders.Platform.Modeling.Notations.Command

/// <summary>Typed invocation surface: engage + wire parser + projection policy.</summary>
type InvocationSurfaceSpec =
    { Id: InvocationSurfaceId
      Engage: InvocationEngageKind
      Wire: InvocationWireKind
      /// <summary>MCP tool name, @intent strip, etc. — before Notations parse.</summary>
      RequiresSurfaceProjection: bool
      Description: string }

module InvocationSurfaceSpec =

    let builtin (id: InvocationSurfaceId) : InvocationSurfaceSpec option =
        match id with
        | InvocationSurfaceId.SlashBar ->
            Some
                { Id = id
                  Engage = InvocationEngageKind.Slash
                  Wire = InvocationWireKind.SlashPath
                  RequiresSurfaceProjection = false
                  Description = "Primary slash command bar" }
        | InvocationSurfaceId.SlashComposer ->
            Some
                { Id = id
                  Engage = InvocationEngageKind.Slash
                  Wire = InvocationWireKind.SlashPath
                  RequiresSurfaceProjection = false
                  Description = "Inline composer slash" }
        | InvocationSurfaceId.ConsoleFilter ->
            Some
                { Id = id
                  Engage = InvocationEngageKind.Slash
                  Wire = InvocationWireKind.ConsolePath
                  RequiresSurfaceProjection = false
                  Description = "Console-style filter line (path + kv/cli tail)" }
        | InvocationSurfaceId.Palette ->
            Some
                { Id = id
                  Engage = InvocationEngageKind.Slash
                  Wire = InvocationWireKind.SlashPath
                  RequiresSurfaceProjection = false
                  Description = "Fuzzy palette discovery → slash path" }
        | InvocationSurfaceId.EditorCcl ->
            Some
                { Id = id
                  Engage = InvocationEngageKind.Slash
                  Wire = InvocationWireKind.SlashPath
                  RequiresSurfaceProjection = false
                  Description = "Editor command line (CCL)" }
        | InvocationSurfaceId.EditorInline ->
            Some
                { Id = id
                  Engage = InvocationEngageKind.Slash
                  Wire = InvocationWireKind.SlashPath
                  RequiresSurfaceProjection = false
                  Description = "Editor inline slash" }
        | InvocationSurfaceId.CommandBar ->
            Some
                { Id = id
                  Engage = InvocationEngageKind.Slash
                  Wire = InvocationWireKind.SlashPath
                  RequiresSurfaceProjection = false
                  Description = "Forge command bar (domain catalog)" }
        | InvocationSurfaceId.BindingHotkey ->
            Some
                { Id = id
                  Engage = InvocationEngageKind.Binding
                  Wire = InvocationWireKind.KeySequence
                  RequiresSurfaceProjection = false
                  Description = "Hotkey / binding chord → commandId" }
        | InvocationSurfaceId.MelodyChord ->
            Some
                { Id = id
                  Engage = InvocationEngageKind.Melody
                  Wire = InvocationWireKind.KeySequence
                  RequiresSurfaceProjection = false
                  Description = "Melody slug / chord tree" }
        | InvocationSurfaceId.McpTool ->
            Some
                { Id = id
                  Engage = InvocationEngageKind.Binding
                  Wire = InvocationWireKind.None
                  RequiresSurfaceProjection = true
                  Description = "MCP tool name + schema args (MCPlane projection)" }
        | InvocationSurfaceId.CitizenIntent ->
            Some
                { Id = id
                  Engage = InvocationEngageKind.Slash
                  Wire = InvocationWireKind.SlashPath
                  RequiresSurfaceProjection = true
                  Description = "CDP @intent / citizen frame strip → slash IR" }
        | InvocationSurfaceId.Custom _ -> None

    let allBuiltins () : InvocationSurfaceSpec list =
        [ InvocationSurfaceId.SlashBar
          InvocationSurfaceId.SlashComposer
          InvocationSurfaceId.ConsoleFilter
          InvocationSurfaceId.Palette
          InvocationSurfaceId.EditorCcl
          InvocationSurfaceId.EditorInline
          InvocationSurfaceId.CommandBar
          InvocationSurfaceId.BindingHotkey
          InvocationSurfaceId.MelodyChord
          InvocationSurfaceId.McpTool
          InvocationSurfaceId.CitizenIntent ]
        |> List.choose builtin
