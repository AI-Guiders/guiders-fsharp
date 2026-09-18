namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open System
open System.IO
open AIGuiders.Platform.Modeling.Agent
open AIGuiders.Platform.Modeling.Build
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Notations.Bracket
open Xunit

/// Plan §10 TO-BE closure gate — audit doc + cross-phase evidence (ship-51).
type FederationPhase10ChecklistTests() =

    let repoRoot =
        Path.Combine(__SOURCE_DIRECTORY__, "..", "..") |> Path.GetFullPath

    [<Fact>]
    member _.``TO-BE audit document exists and declares blocked closure``() =
        let auditPath = Path.Combine(repoRoot, "docs", "federation", "model-extraction-to-be-audit.md") |> Path.GetFullPath
        Assert.True(File.Exists auditPath, auditPath)

        let text = File.ReadAllText auditPath
        Assert.Contains("Phase 1", text)
        Assert.Contains("Phase 2", text)
        Assert.Contains("Phase 3", text)
        Assert.Contains("Phase 4", text)
        Assert.Contains("CLOSURE: BLOCKED", text)
        Assert.Contains("P4-01", text)

    [<Fact>]
    member _.``Math ide-session maps omega to DocumentRegistry``() =
        let mathPath = Path.Combine(repoRoot, "docs", "math", "ide-session", "10-implementation.md") |> Path.GetFullPath
        Assert.True(File.Exists mathPath, mathPath)
        let text = File.ReadAllText mathPath
        Assert.Contains("DocumentRegistry", text)
        Assert.DoesNotContain("SolutionGraph.FileOwnership", text)

    [<Fact>]
    member _.``Living matrix tracker exists``() =
        let matrixPath = Path.Combine(repoRoot, "docs", "federation", "model-extraction-living-matrix.md") |> Path.GetFullPath
        Assert.True(File.Exists matrixPath, matrixPath)

    [<Fact>]
    member _.``Modeling.Gdl.Language project is deleted``() =
        let candidate = Path.Combine(repoRoot, "src", "AIGuiders.Platform.Modeling.Gdl.Language") |> Path.GetFullPath
        Assert.False(Directory.Exists candidate, $"legacy package still present: {candidate}")

    [<Fact>]
    member _.``Modeling.Agent package ships public envelope types``() =
        Assert.True(typeof<AgentResponseEnvelope>.IsPublic)

    [<Fact>]
    member _.``Attach conformance spec remains on disk``() =
        let attachSpec =
            Path.Combine(repoRoot, "docs", "conformance", "commandplane", "attach-schema.spec.json")
            |> Path.GetFullPath

        Assert.True(File.Exists attachSpec, attachSpec)

    [<Fact>]
    member _.``BuildDiagnostic carries RelationSpec witness field``() =
        let specField = typeof<BuildDiagnostic>.GetProperty("Spec")
        Assert.NotNull specField
        Assert.Equal(typeof<RelationSpec>, specField.PropertyType)

    [<Fact>]
    member _.``CodeEditWireEncoding ships Kind line and scope wire hints``() =
        Assert.Equal("@line:", CodeEditWireEncoding.LinePrefix)
        Assert.Equal("@scope:", CodeEditWireEncoding.ScopePrefix)
