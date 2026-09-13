namespace AIGuiders.Platform.Modeling.Invocation

open AIGuiders.Platform.Modeling.Notations.Argument
open AIGuiders.Platform.Modeling.Notations.Command

/// <summary>Projection pipeline stages (ADR-0021 §13.1). Pure labels in I0; CommandPlane drives transitions I3+.</summary>
type InvocationStage =
    | Raw of surface: InvocationSurfaceId * text: string
    | SurfaceProjected of surface: InvocationSurfaceId * wireText: string
    | WireParsed of surface: InvocationSurfaceId * path: NormalizedCommandLine * args: NormalizedArguments
    | Resolved of canonical: CanonicalInvocation

module InvocationStage =

    let raw surface text = Raw(surface, text)

    /// <summary>I0: identity except surfaces that require projection slot (pass-through wire).</summary>
    let projectSurface (spec: InvocationSurfaceSpec) (text: string) : Result<string, string> =
        if System.String.IsNullOrWhiteSpace text then Error "Empty invocation text."
        elif spec.RequiresSurfaceProjection then Ok(text.Trim())
        else Ok(text.Trim())

    let advanceToProjected (spec: InvocationSurfaceSpec) (Raw(surface, text)) =
        match projectSurface spec text with
        | Ok wire -> Ok(SurfaceProjected(surface, wire))
        | Error e -> Error e

    let advanceToWireParsed surface path args (SurfaceProjected(surface', wire)) =
        if surface <> surface' then Error "Surface id mismatch in pipeline."
        else Ok(WireParsed(surface, path, args))

    let advanceToResolved (WireParsed(surface, path, args)) commandId =
        Ok(Resolved(CanonicalInvocation.create commandId path args))
