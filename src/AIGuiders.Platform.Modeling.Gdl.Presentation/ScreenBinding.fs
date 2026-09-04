namespace AIGuiders.Platform.Modeling.Gdl.Presentation

open System.Collections.Generic

/// <summary>How a logical host maps to a physical screen at runtime (deployment profile).</summary>
type PhysicalScreenSelectorKind =
    | Primary = 0
    | Index = 1
    | DeviceName = 2
    /// <summary>Single ultrawide — host occupies a normalized region (0..1).</summary>
    | UltrawideRegion = 3

type PhysicalScreenSelector =
    { Kind: PhysicalScreenSelectorKind
      ScreenIndex: int option
      DeviceName: string option
      RegionLeft: double option
      RegionTop: double option
      RegionWidth: double option
      RegionHeight: double option }

module PhysicalScreenSelector =

    /// <summary>Primary-screen selector (C# default-args shape).</summary>
    let primary : PhysicalScreenSelector =
        { Kind = PhysicalScreenSelectorKind.Primary
          ScreenIndex = None
          DeviceName = None
          RegionLeft = None
          RegionTop = None
          RegionWidth = None
          RegionHeight = None }

    /// <summary>Index-based selector (C# default-args shape).</summary>
    let byIndex (index: int) : PhysicalScreenSelector =
        { Kind = PhysicalScreenSelectorKind.Index
          ScreenIndex = Some index
          DeviceName = None
          RegionLeft = None
          RegionTop = None
          RegionWidth = None
          RegionHeight = None }

    /// <summary>Ultrawide normalized region selector (0..1).</summary>
    let ultrawide (left: double) (top: double) (width: double) (height: double) : PhysicalScreenSelector =
        { Kind = PhysicalScreenSelectorKind.UltrawideRegion
          ScreenIndex = None
          DeviceName = None
          RegionLeft = Some left
          RegionTop = Some top
          RegionWidth = Some width
          RegionHeight = Some height }

/// <summary>Runtime binding: logical LogicalDisplayHost.HostIndex → physical screen.</summary>
type DisplayHostBinding =
    { HostIndex: int
      Screen: PhysicalScreenSelector }

/// <summary>Operator / machine display layout — separate from PresentationTopology.</summary>
type DisplayBindingProfile =
    { ProfileId: string
      Bindings: IReadOnlyList<DisplayHostBinding> }