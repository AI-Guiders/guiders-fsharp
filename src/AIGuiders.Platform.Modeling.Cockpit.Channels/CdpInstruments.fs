namespace AIGuiders.Platform.Modeling.Cockpit.Channels

open System.Collections.Generic

// ── CDP-domain CDS instruments (QRH / Arch board) — M-shapes extracted from CDP ──

/// <summary>eQRH step — text + optional go-verb/action (CIDE EICAS feed source=qrh).
/// Nullability mirrors C# `string?`.</summary>
[<CLIMutable>]
type QrhStep =
    { Text: string
      Go: string
      Action: string | null }

/// <summary>eQRH page — systems/abnormal/emergency shelf (parity: IdeQrhChannel.Page).</summary>
[<CLIMutable>]
type QrhPage =
    { Id: string
      Shelf: string
      Title: string
      Condition: string
      Signals: string list
      MemoryItems: string list
      Steps: QrhStep list
      Related: string list
      PackAnchors: string list
      LlmCue: string | null
      Builtin: bool }

/// <summary>When-phases/ECL probe raises page in SA suggest (overlay-friendly).</summary>
[<CLIMutable>]
type QrhSuggestRule =
    { Phases: string list
      Ecl: string list
      Score: int }

/// <summary>SA suggest: hot page + related (parity: IdeQrhChannel.Suggest).</summary>
[<CLIMutable>]
type QrhSuggest =
    { HotId: string
      RelatedIds: string list
      Pulse: string }

module Qrh =

    /// Parity with CideQrhLatch.Publish: related minus hot, cap 4.
    let relatedMinusHot (snap: QrhSuggest) : string list =
        snap.RelatedIds
        |> List.filter (fun id -> not (System.String.IsNullOrWhiteSpace id))
        |> List.filter (fun id -> id <> snap.HotId)
        |> List.truncate 4

/// <summary>Arch board role slot — a role in the as-built/plan wiring (parity: RoleSlot).</summary>
[<CLIMutable>]
type ArchRoleSlot =
    { Id: string
      Role: string
      Status: string
      Note: string }

/// <summary>Arch board edge — wiring between two roles (feeds/projects/mounts/wires).</summary>
[<CLIMutable>]
type ArchEdge =
    { FromRoleId: string
      ToRoleId: string
      Kind: string }

/// <summary>Arch board document — as-built or plan wiring (parity: BoardDoc).</summary>
[<CLIMutable>]
type ArchBoardDoc =
    { Title: string
      Mode: string
      Profile: string
      UpdatedUtc: string
      Roles: ArchRoleSlot list
      Edges: ArchEdge list
      FocusRoleId: string }

module ArchBoard =

    /// Transport→CCU→Channel→CDS→Compositor→Surface — canonical cockpit circuit chain.
    let canonicalChain : (string * string * string) list =
        [ ("transport-ingest", "ccu-core", "feeds")
          ("dal-core", "ccu-core", "feeds")
          ("ccu-core", "ch-core", "feeds")
          ("ch-core", "cds-core", "feeds")
          ("cds-core", "comp-core", "projects")
          ("comp-core", "surf-core", "projects") ]