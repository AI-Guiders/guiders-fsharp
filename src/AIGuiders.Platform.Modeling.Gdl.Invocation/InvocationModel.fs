namespace AIGuiders.Platform.Modeling.Gdl.Invocation

open System
open System.Collections.Generic

/// <summary>Post-resolve command path (catalog longest-prefix or explicit console path).</summary>
type NormalizedCommandLine =
    { CanonicalPath: string
      PathSegments: IReadOnlyList<string> }

/// <summary>
/// Arg-tail interaction during InvocationLinePhase.Arg (GUIDERS-ADR-0043).
/// Notation-agnostic — distinct from InvocationEngageKind.
/// Implemented by CommandPlane guilds (Constructors, PrefixArmed, ArgSuggestions, …).
/// </summary>
type ArgMechanic =
    | Picker = 1
    | FreeText = 2
    | Optional = 3
    | Constructor = 4
    | TypedInput = 5

/// <summary>
/// How invocation started after engage consume (GUIDERS-ADR-0015) — notation-bound.
/// Distinct from ArgMechanic (arg-tail interaction, notation-agnostic).
/// </summary>
type InvocationEngageKind =
    | Slash = 1
    | Melody = 2
    | Binding = 3

/// <summary>
/// Where the user is on the invocation line after engage (GUIDERS-ADR-0043).
/// Orthogonal to engage (Slash / Melody / Binding per ADR-0015).
/// </summary>
type InvocationLinePhase =
    /// <summary>Completing command path or melody slug steps.</summary>
    | Path = 0
    /// <summary>Collecting arg tail — ArgMechanic applies here.</summary>
    | Arg = 1
    /// <summary>Line is runnable — Enter executes.</summary>
    | Ready = 2