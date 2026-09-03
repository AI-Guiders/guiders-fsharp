namespace AIGuiders.Platform.Modeling.Cockpit.DataBus

/// <summary>Signal stratum (CIDE ADR 0095) — semantic level, not severity.</summary>
type HealthStratum =
    | Workspace
    | Solution
    | Ide

/// <summary>Scope within solution stratum.</summary>
type HealthScope =
    | SolutionScope
    | ProjectScope

/// <summary>Segment source in IDE Health strip.</summary>
type HealthSegmentSource =
    | Build
    | Tests
    | Debug
    | Git
