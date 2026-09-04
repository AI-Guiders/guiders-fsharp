module AIGuiders.Platform.Modeling.Gdl.Agent.Tests.ConformanceTests

open System.Collections.Generic
open Xunit
open AIGuiders.Platform.Modeling.Gdl.Agent

[<Fact>]
let ``Envelope: green pulse with hint rows`` () =
    let hints =
        [ { Kind = "go"; CommandId = Some "cdp_build"; ToolName = None; Label = Some "Build" } ]
        :> IReadOnlyList<NextHint>
    let e =
        { Ok = true
          Tier = DetailTier.Pulse
          Pulse = Some "gates ok"
          Reason = None
          Next = Some hints }
    Assert.True e.Ok
    Assert.Equal(DetailTier.Pulse, e.Tier)
    Assert.Equal(Some "gates ok", e.Pulse)
    Assert.Equal(1, (Option.get e.Next).Count)
    Assert.Equal(Some "cdp_build", (Option.get e.Next).[0].CommandId)

[<Fact>]
let ``Envelope: failure carries reason without pulse`` () =
    let e =
        { Ok = false
          Tier = DetailTier.Full
          Pulse = None
          Reason = Some "build failed"
          Next = None }
    Assert.False e.Ok
    Assert.Equal(Some "build failed", e.Reason)
    Assert.True e.Next.IsNone

[<Fact>]
let ``DetailTier: wire values 0..3`` () =
    Assert.Equal(0, int DetailTier.Pulse)
    Assert.Equal(1, int DetailTier.Slim)
    Assert.Equal(2, int DetailTier.Full)
    Assert.Equal(3, int DetailTier.Wide)