module AIGuiders.Platform.Modeling.Invocation.Tests.InvocationStageTests

open Xunit
open AIGuiders.Platform.Modeling.Invocation
open AIGuiders.Platform.Modeling.Notations.Argument
open AIGuiders.Platform.Modeling.Notations.Command

[<Fact>]
let ``Pipeline: raw to resolved for slash bar`` () =
    match SurfaceRegistry.specFor InvocationSurfaceId.SlashBar with
    | Error e -> Assert.Fail e
    | Ok spec ->
        let raw = InvocationStage.raw InvocationSurfaceId.SlashBar "/git plan verify"
        match InvocationStage.advanceToProjected spec raw with
        | Error e -> Assert.Fail e
        | Ok projected ->
            let path = InvocationNotation.fromPathSegments [ "git"; "plan"; "verify" ]
            let args = NormalizedArguments.FromRaw("", "slash")
            match InvocationStage.advanceToWireParsed InvocationSurfaceId.SlashBar path args projected with
            | Error e -> Assert.Fail e
            | Ok wireParsed ->
                match InvocationStage.advanceToResolved wireParsed "git.plan.verify" with
                | Error e -> Assert.Fail e
                | Ok(Resolved canon) ->
                    Assert.Equal("git.plan.verify", canon.CommandId)
                    Assert.Equal("git plan verify", canon.Path.CanonicalPath)
