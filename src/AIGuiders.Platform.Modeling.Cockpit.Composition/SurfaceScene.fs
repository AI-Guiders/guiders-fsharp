namespace AIGuiders.Platform.Modeling.Cockpit.Composition

open System.Collections.Generic
open System.Text.Json

/// <summary>Surface scene uses opaque struct JsonElement rows — nullability via wrapper DU.</summary>
type OptionalRow =
    | Present of JsonElement
    | Absent

/// <summary>Opaque row carrier for JSON wire payloads (struct JsonElement has no nullness).</summary>
[<RequireQualifiedAccess>]
module Rows =
    /// Null is not a proper value for struct JsonElement — use Absent.
    let absent = Absent

/// <summary>Inputs projected by peels — compositor assembles the seats surface (ADR 0036).</summary>
[<NoComparison>]
type SeatsSurfaceScene =
    { SchemaVersion: string
      Mfd: string
      View: JsonElement
      Seats: JsonElement
      Session: JsonElement
      Instrument: OptionalRow
      Alert: OptionalRow
      Pressure: OptionalRow
      Next: JsonElement
      Focus: OptionalRow
      Go: OptionalRow
      Warm: OptionalRow
      Pins: (string | null) list
      Layouts: string list
      ThrashNote: string | null
      Loci: OptionalRow
      GoVerbs: string list }

/// <summary>Seats payload — seat count badge.</summary>
[<CLIMutable>]
type SeatsSurfacePayload =
    { SeatCount: int }

/// <summary>Legacy tiles-mode desk surface inputs (prefer seats).</summary>
[<NoComparison>]
type TilesSurfaceScene =
    { SchemaVersion: string
      Mfd: string
      Session: JsonElement
      Tiles: OptionalRow
      Alert: OptionalRow
      Next: JsonElement
      Focus: OptionalRow
      Go: OptionalRow
      Warm: OptionalRow
      Pins: string list
      Layouts: string list
      Loci: OptionalRow
      GoVerbs: string list }