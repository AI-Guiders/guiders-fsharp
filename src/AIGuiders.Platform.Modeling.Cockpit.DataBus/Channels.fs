namespace AIGuiders.Platform.Modeling.Cockpit.DataBus

type ChannelId = ChannelId of string

module ChannelId =
    let ideHealth = ChannelId "ide-health"
    let environmentReadiness = ChannelId "environment-readiness"

    let value (ChannelId id) = id
