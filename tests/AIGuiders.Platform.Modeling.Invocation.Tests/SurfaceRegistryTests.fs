module AIGuiders.Platform.Modeling.Invocation.Tests.SurfaceRegistryTests

open Xunit
open AIGuiders.Platform.Modeling.Invocation
open AIGuiders.Platform.Modeling.Notations.Command

[<Fact>]
let ``ParseId: federation GDL wires`` () =
    let cases =
        [ "slash.bar", InvocationSurfaceId.SlashBar
          "palette", InvocationSurfaceId.Palette
          "console.filter", InvocationSurfaceId.ConsoleFilter
          "ccl.filter", InvocationSurfaceId.EditorCcl
          "command-bar", InvocationSurfaceId.CommandBar
          "mcp.tool", InvocationSurfaceId.McpTool ]

    for wire, expected in cases do
        match SurfaceRegistry.tryParseId wire with
        | Ok id -> Assert.Equal(expected, id)
        | Error e -> Assert.Fail($"{wire}: {e}")

[<Fact>]
let ``ParseList: dash catalog defaults validate`` () =
    match SurfaceRegistry.tryParseFederationDefaults() with
    | Ok specs ->
        Assert.Equal(4, specs.Length)
        Assert.Contains(specs, fun s -> s.Id = InvocationSurfaceId.SlashBar)
        Assert.Contains(specs, fun s -> s.Wire = InvocationWireKind.ConsolePath && s.Id = InvocationSurfaceId.ConsoleFilter)
    | Error e -> Assert.Fail e

[<Fact>]
let ``SpecFor: console uses ConsolePath wire`` () =
    match SurfaceRegistry.specFor InvocationSurfaceId.ConsoleFilter with
    | Ok spec ->
        Assert.Equal(InvocationEngageKind.Slash, spec.Engage)
        Assert.Equal(InvocationWireKind.ConsolePath, spec.Wire)
        Assert.False spec.RequiresSurfaceProjection
    | Error e -> Assert.Fail e

[<Fact>]
let ``SpecFor: mcp requires surface projection`` () =
    match SurfaceRegistry.specFor InvocationSurfaceId.McpTool with
    | Ok spec ->
        Assert.True spec.RequiresSurfaceProjection
        Assert.Equal(InvocationWireKind.None, spec.Wire)
    | Error e -> Assert.Fail e

[<Fact>]
let ``Custom surface gets planet fallback spec`` () =
    match SurfaceRegistry.specFor(InvocationSurfaceId.Custom "forge.zoo") with
    | Ok spec ->
        Assert.Equal(InvocationSurfaceId.Custom "forge.zoo", spec.Id)
        Assert.Equal(InvocationWireKind.SlashPath, spec.Wire)
    | Error e -> Assert.Fail e
