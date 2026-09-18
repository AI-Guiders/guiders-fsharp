namespace AIGuiders.Platform.Modeling.CodeCenter.Tests

open Xunit
open AIGuiders.Platform.Modeling.CodeCenter
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

module RePlannableThetaRegistryTests =

    [<Fact>]
    let ``V8d registry contains MoveMember and Extract rows`` () =
        let move = RePlannableThetaRegistry.lookup "MoveMember"
        let extract = RePlannableThetaRegistry.lookup "Extract"
        Assert.True(move.IsSome)
        Assert.True(extract.IsSome)
        Assert.Equal(DeltaOrReplan, move.Value.ReplayMode)
        Assert.Equal(DeltaOrReplan, extract.Value.ReplayMode)

    [<Fact>]
    let ``V8d committed ledger entries satisfy registry rows`` () =
        let session = DocumentSession.Create("doc://demo", "@dashboard demo\nend dashboard\n")
        let node = session.TryResolve({ Offset = 5; TierHint = Some "Semantic" }).Value

        match session.ApplyStructural(RenameMember(node.NodeId.Value, "renamed")) with
        | Ok(session', entry) ->
            Assert.True(RePlannableThetaRegistry.satisfiesEntry entry)
            Assert.True(entry.PhiRef.IsSome)
            Assert.Equal(1, session'.CommittedCount)
        | Error e -> Assert.Fail e
