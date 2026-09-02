namespace AIGuiders.Platform.Modeling.Gdl.Presentation

type AttentionDisplayRole =
    | Unknown = 0
    | Pfd = 1
    | Forward = 2
    | Mfd = 3
    | PmOneOf = 4
    | Eicas = 5
    | Briefing = 6
    | Hud = 7

type ZoneComposeKind =
    | Split = 0
    | OneOf = 1

type TopologyArrangement =
    | SingleSurfaceCompositional = 0
    | SingleHostOneOf = 1
    | MultiHost = 2

type LogicalDisplayHost =
    { HostIndex: int
      HostId: string
      Role: AttentionDisplayRole
      Compose: ZoneComposeKind
      ChannelStack: string list
      ActiveChannel: string }

type PresentationTopology =
    { Arrangement: TopologyArrangement
      Hosts: LogicalDisplayHost list
      SourceWire: string }

    member this.HostCount = List.length this.Hosts

type TopologyParseResult =
    { Topology: PresentationTopology option
      Error: string option }

    member this.IsSuccess = Option.isSome this.Topology && Option.isNone this.Error

    static member Ok(topology: PresentationTopology) =
        { Topology = Some topology; Error = None }

    static member Fail(message: string) =
        { Topology = None; Error = Some message }
