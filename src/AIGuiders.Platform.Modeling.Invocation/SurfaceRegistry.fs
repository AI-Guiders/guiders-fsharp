namespace AIGuiders.Platform.Modeling.Invocation

open System
open AIGuiders.Platform.Modeling.Notations.Command

/// <summary>GDL catalog surface strings ↔ typed ids (ADR-0008 I0).</summary>
module SurfaceRegistry =

    let tryParseId (wire: string) : Result<InvocationSurfaceId, string> =
        match InvocationSurfaceId.tryParse wire with
        | Some id -> Ok id
        | None -> Error $"Unknown invocation surface '{wire}'."

    let tryParseList (commaSeparated: string) : Result<InvocationSurfaceId list, string> =
        if String.IsNullOrWhiteSpace commaSeparated then Ok []
        else
            commaSeparated.Split(',', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries)
            |> Array.toList
            |> List.fold
                (fun acc part ->
                    match acc with
                    | Error _ -> acc
                    | Ok ids ->
                        match tryParseId part with
                        | Ok id -> Ok(id :: ids)
                        | Error e -> Error e)
                (Ok [])
            |> Result.map List.rev

    let specFor (id: InvocationSurfaceId) : Result<InvocationSurfaceSpec, string> =
        match InvocationSurfaceSpec.builtin id with
        | Some spec -> Ok spec
        | None ->
            match id with
            | InvocationSurfaceId.Custom wire ->
                Ok
                    { Id = id
                      Engage = InvocationEngageKind.Slash
                      Wire = InvocationWireKind.SlashPath
                      RequiresSurfaceProjection = false
                      Description = $"Planet custom surface '{wire}'" }
            | _ -> Error $"No builtin spec for {InvocationSurfaceId.toWire id}."

    let validateGdlSurfaces (surfaces: string list) : Result<InvocationSurfaceSpec list, string> =
        surfaces
        |> List.fold
            (fun acc wire ->
                match acc with
                | Error _ -> acc
                | Ok specs ->
                    match tryParseId wire with
                    | Error e -> Error e
                    | Ok id ->
                        match specFor id with
                        | Ok spec -> Ok(spec :: specs)
                        | Error e -> Error e)
            (Ok [])
        |> Result.map List.rev

    /// <summary>Federation catalog defaults from dash.catalog.gdl (conformance golden).</summary>
    let federationDefaultSurfaceWires =
        [ "slash.bar"; "palette"; "console.filter"; "ccl.filter" ]

    let tryParseFederationDefaults () =
        validateGdlSurfaces federationDefaultSurfaceWires
