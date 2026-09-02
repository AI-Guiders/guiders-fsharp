namespace AIGuiders.Platform.Modeling.Cockpit.DataBus

type DispatchMode =
    | Reliable
    | Burst

type DispatchPolicy = { BurstEvents: Set<EventId> }

module DispatchPolicy =
    let create (burstEvents: EventId seq) =
        { BurstEvents = burstEvents |> Set.ofSeq }

    let allReliable = { BurstEvents = Set.empty }

    /// Parity with <c>Platform.Cockpit.DataBus.DataBusEventPolicy.Default</c> (CIDE ADR 0099).
    let defaultPolicy =
        create
            [ DebugStateChanged
              GitStateChanged
              IdeHostStateChanged ]

    let isBurst (eventId: EventId) (policy: DispatchPolicy) =
        Set.contains eventId policy.BurstEvents

    let mode eventId policy =
        if isBurst eventId policy then Burst else Reliable

    let tryFromTypeName (name: string) (isBurstFlag: bool) =
        EventId.tryParse name
        |> Option.map (fun id -> id, isBurstFlag)

    /// C# Execution runtime: map CLR event type name to burst/reliable policy.
    let isBurstForTypeName (typeName: string) (policy: DispatchPolicy) =
        match EventId.tryParse typeName with
        | Some id -> isBurst id policy
        | None -> false
