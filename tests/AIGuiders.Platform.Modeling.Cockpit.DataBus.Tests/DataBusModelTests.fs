namespace AIGuiders.Platform.Modeling.Cockpit.DataBus.Tests

open Xunit
open AIGuiders.Platform.Modeling.Cockpit.DataBus

type DataBusModelTests() =

    [<Fact>]
    member _.``DispatchPolicy default matches C# burst flags``() =
        let policy = DispatchPolicy.defaultPolicy

        Assert.True(DispatchPolicy.isBurst DebugStateChanged policy)
        Assert.True(DispatchPolicy.isBurst GitStateChanged policy)
        Assert.True(DispatchPolicy.isBurst IdeHostStateChanged policy)
        Assert.False(DispatchPolicy.isBurst BuildStateChanged policy)
        Assert.False(DispatchPolicy.isBurst TestsStateChanged policy)
        Assert.False(DispatchPolicy.isBurst StartupProjectPathChanged policy)

        Assert.Equal(Burst, DispatchPolicy.mode DebugStateChanged policy)
        Assert.Equal(Reliable, DispatchPolicy.mode BuildStateChanged policy)

    [<Fact>]
    member _.``EventId roundtrips C# type names``() =
        for id in EventId.all do
            let parsed = EventId.tryParse id.TypeName
            Assert.True(parsed.IsSome)
            Assert.Equal(Some id, parsed)

    [<Fact>]
    member _.``IdeHealth projection graph validates``() =
        Assert.True(IdeHealthProjection.validation.IsValid)

    [<Fact>]
    member _.``IdeHealth projection lists all bus events``() =
        let subscribed = Set.ofList IdeHealthProjection.subscribedEvents
        let expected = Set.ofList EventCatalog.ideHealthEvents
        Assert.True(Set.isSubset expected subscribed)
        Assert.True(Set.isSubset subscribed expected)

    [<Fact>]
    member _.``BuildStateFold matches platform Apply semantics``() =
        let prior = BuildStateSnapshot.Empty

        let building =
            BuildStateFold.apply prior
                { IsBuilding = true
                  LastExitCode = None
                  LastBuildSucceeded = None }

        Assert.True(building.IsBuilding)

        let finished =
            BuildStateFold.apply building
                { IsBuilding = false
                  LastExitCode = Some 0
                  LastBuildSucceeded = Some true }

        Assert.False(finished.IsBuilding)
        Assert.Equal(Some 0, finished.LastExitCode)
        Assert.Equal(Some true, finished.LastBuildSucceeded)
