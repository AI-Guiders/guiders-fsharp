namespace AIGuiders.Platform.Modeling.Routing

open System
open AIGuiders.Platform.Modeling.Core

[<RequireQualifiedAccess>]
module RouteRefusal =
    [<CompiledName("Refuse")>]
    let refuse factory raw reason go =
        factory raw reason go

    [<CompiledName("OutcomeNotOk")>]
    let outcomeNotOk (route: RoutedIntent) (pulse: string option) =
        { Raw = route.Raw
          Verb = route.Verb
          Ok = false
          Action = Unchecked.defaultof<string>
          Seat = Unchecked.defaultof<string>
          Go = route.Go
          Path = Unchecked.defaultof<string>
          DocId = Unchecked.defaultof<string>
          Cmd = route.Cmd
          Pulse = Option.defaultValue route.Reason pulse
          Reason = if String.IsNullOrEmpty route.Reason then "route_not_ok" else route.Reason
          Ship = Unchecked.defaultof<string> }
