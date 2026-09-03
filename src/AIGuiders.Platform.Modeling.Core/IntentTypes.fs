namespace AIGuiders.Platform.Modeling.Core

/// <summary>Host-execute result — product-neutral shape aligned with CDP CitizenRouteHost.Applied.</summary>
[<CLIMutable>]
type IntentOutcome =
    { Raw: string
      Verb: string
      Ok: bool
      Action: string
      Seat: string
      Go: string
      Path: string
      DocId: string
      Cmd: string
      Pulse: string
      Reason: string
      Ship: string }
/// <summary>Parsed intent before host execute — minimal cross-product envelope.</summary>
[<CLIMutable>]
type RoutedIntent =
    { Verb: string
      Raw: string
      Ok: bool
      Go: string
      Organ: string
      Path: string
      Detail: string
      Scene: string
      Cmd: string
      OldString: string
      NewString: string
      Op: string
      Reason: string }
