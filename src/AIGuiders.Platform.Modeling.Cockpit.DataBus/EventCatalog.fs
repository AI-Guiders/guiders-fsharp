namespace AIGuiders.Platform.Modeling.Cockpit.DataBus

/// <summary>Stable event id for bus catalog and dispatch policy (C# type name parity).</summary>
type EventId =
    | BuildStateChanged
    | TestsStateChanged
    | DebugStateChanged
    | GitStateChanged
    | IdeHostStateChanged
    | StartupProjectPathChanged
    | DeskSurfaceBuilt

    member this.TypeName =
        match this with
        | BuildStateChanged -> "BuildStateChanged"
        | TestsStateChanged -> "TestsStateChanged"
        | DebugStateChanged -> "DebugStateChanged"
        | GitStateChanged -> "GitStateChanged"
        | IdeHostStateChanged -> "IdeHostStateChanged"
        | StartupProjectPathChanged -> "StartupProjectPathChanged"
        | DeskSurfaceBuilt -> "DeskSurfaceBuilt"

module EventId =
    let tryParse (name: string) =
        match name with
        | "BuildStateChanged" -> Some BuildStateChanged
        | "TestsStateChanged" -> Some TestsStateChanged
        | "DebugStateChanged" -> Some DebugStateChanged
        | "GitStateChanged" -> Some GitStateChanged
        | "IdeHostStateChanged" -> Some IdeHostStateChanged
        | "StartupProjectPathChanged" -> Some StartupProjectPathChanged
        | "DeskSurfaceBuilt" -> Some DeskSurfaceBuilt
        | _ -> None

    let all =
        [ BuildStateChanged
          TestsStateChanged
          DebugStateChanged
          GitStateChanged
          IdeHostStateChanged
          StartupProjectPathChanged
          DeskSurfaceBuilt ]

type BuildStateChanged =
    { IsBuilding: bool
      LastExitCode: int option
      LastBuildSucceeded: bool option }

type TestsStateChanged = { Summary: string; ImpactedBadge: int }

type GitStateChanged = { Line: string; CockpitShort: string }

type IdeHostStateChanged =
    { CSharpLspProcessActive: bool
      MarkdownLspProcessActive: bool
      CSharpLspHostPresent: bool
      MarkdownLspHostPresent: bool }

type StartupProjectPathChanged = { ProjectPath: string option }

type DeskSurfaceBuilt =
    { Mode: string
      SeatCount: int
      Go: string option
      Utc: System.DateTimeOffset }

module EventCatalog =
    /// Events wired into IDE Health CCU fold (ADR 0097 quarry).
    let ideHealthEvents =
        [ BuildStateChanged
          TestsStateChanged
          DebugStateChanged
          GitStateChanged
          IdeHostStateChanged
          StartupProjectPathChanged ]
