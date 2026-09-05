namespace AIGuiders.Platform.Modeling.Ide.Session

/// Default κ catalog per τ (§1.5 T1–T3).
module ProjectCapabilityCatalog =
    let forKind (kind: ProjectKind) =
        match kind with
        | DotNet _ -> CapabilityCatalog.defaultDotNet ()
        | Node _ -> CapabilityCatalog.defaultNode ()
        | Gdl _ -> CapabilityCatalog.defaultGdl ()
        | Planet _ -> CapabilityCatalog.defaultPlanet ()
        | Doc _ -> CapabilityCatalog.defaultDoc ()

    /// Alias for orchestrator call sites — same catalog, τ-keyed.

    /// Alias for orchestrator call sites — same catalog, τ-keyed.
    let forTau = forKind
