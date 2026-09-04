namespace AIGuiders.Platform.Modeling.Notations.Command

open System
open System.Collections.Generic

/// <summary>Normalized command-line notation (C# InvocationNotation).</summary>
[<RequireQualifiedAccess>]
module InvocationNotation =

    /// <summary>Filter blank segments, join with single space (C# FromPathSegments).</summary>
    let fromPathSegments (segments: string seq) : NormalizedCommandLine =
        let list =
            segments
            |> Seq.filter (fun s -> not (String.IsNullOrWhiteSpace s))
            |> Seq.toList
        { CanonicalPath = String.Join(' ', list)
          PathSegments = (list :> IReadOnlyList<string>) }

    /// <summary>Case-insensitive canonical path equality (C# PathsEqual).</summary>
    let pathsEqual (a: NormalizedCommandLine) (b: NormalizedCommandLine) : bool =
        String.Equals(a.CanonicalPath, b.CanonicalPath, StringComparison.OrdinalIgnoreCase)