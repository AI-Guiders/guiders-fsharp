namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

module private Samples =
    let fsharpProject =
        let id = ProjectId.create @"D:\repo\src\App\App.fsproj"

        ProjectNode.create
            id
            (DotNet { Language = FSharp })
            (ProjectId.value id)
            (CapabilityCatalog.defaultDotNet ())

    let csharpProject =
        let id = ProjectId.create @"D:\repo\src\Lib\Lib.csproj"

        ProjectNode.create
            id
            (DotNet { Language = CSharp })
            (ProjectId.value id)
            (CapabilityCatalog.defaultDotNet ())

    let mixedSolution =
        let fs = fsharpProject
        let cs = csharpProject

        let buildCap = GraphNodeId.capability fs.Id Build
        let compilerCap = GraphNodeId.capability fs.Id CompilerServices

        SolutionGraph.create
            @"D:\repo\App.slnx"
            [ fs; cs ]
            (Map.ofList [ @"D:\repo\src\App\Module.fs", fs.Id ])
            [ { From = buildCap
                To = compilerCap
                Kind = Requires
                Attributes = Map.empty } ]

type SessionGraphTests() =

    [<Fact>]
    member _.``Gdl project uses Gdl capability catalog``() =
        let id = ProjectId.create @"D:\repo\deck\deck.gdlproj"

        let project =
            ProjectNode.create
                id
                (Gdl { ProjectFile = "deck.gdlproj" })
                (ProjectId.value id)
                (ProjectCapabilityCatalog.forKind (Gdl { ProjectFile = "deck.gdlproj" }))

        let graph = SolutionGraph.create @"D:\repo\App.slnx" [ project ] Map.empty []
        let result = GraphValidation.validate graph
        Assert.True(result.IsValid, result.Issues |> List.map (fun i -> i.Message) |> String.concat "; ")

    [<Fact>]
    member _.``Mixed solution graph validates``() =
        let result = GraphValidation.validate Samples.mixedSolution
        Assert.True(result.IsValid, result.Issues |> List.map (fun i -> i.Message) |> String.concat "; ")

    [<Fact>]
    member _.``Duplicate capability is rejected``() =
        let project =
            { Samples.fsharpProject with
                Capabilities =
                    CapabilityCatalog.compilerServices ()
                    :: CapabilityCatalog.compilerServices ()
                    :: [] }

        let graph =
            SolutionGraph.create @"D:\repo\App.slnx" [ project ] Map.empty []

        let result = GraphValidation.validate graph
        Assert.False(result.IsValid)
        Assert.Contains(result.Issues, fun i -> i.Message.Contains("Duplicate capability"))

    [<Fact>]
    member _.``Requires cycle is rejected``() =
        let fs = Samples.fsharpProject
        let a = GraphNodeId.capability fs.Id CompilerServices
        let b = GraphNodeId.capability fs.Id Build

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ fs ]
                Map.empty
                [ { From = a; To = b; Kind = Requires; Attributes = Map.empty }
                  { From = b; To = a; Kind = Requires; Attributes = Map.empty } ]

        let result = GraphValidation.validate graph
        Assert.False(result.IsValid)
        Assert.Contains(result.Issues, fun i -> i.Message.Contains("Cycle"))

    [<Fact>]
    member _.``Adaptive without rules is rejected``() =
        let cap =
            { CapabilityCatalog.staticAnalysisAdaptive () with
                Attributes =
                    { CapabilityCatalog.staticAnalysisAdaptive().Attributes with
                        AdaptiveRules = [] } }

        let project = { Samples.fsharpProject with Capabilities = [ cap ] }

        let graph = SolutionGraph.create @"D:\repo\App.slnx" [ project ] Map.empty []
        let result = GraphValidation.validate graph
        Assert.False(result.IsValid)

    [<Fact>]
    member _.``LifecyclePhase canAdvance respects ordering``() =
        Assert.True(LifecyclePhase.canAdvanceTo Unloaded DesignTime)
        Assert.True(LifecyclePhase.canAdvanceTo DesignTime CompileTime)
        Assert.False(LifecyclePhase.canAdvanceTo CompileTime DesignTime)
