namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open System
open System.Reflection
open AIGuiders.Platform.Modeling.Documentation.Correspondence
open AIGuiders.Platform.Modeling.Language
open Xunit

/// Plan §10 Phase 2 verification gate — progress toward TO-BE, not checklist tick theater.
type FederationPhase2ChecklistTests() =

    [<Fact>]
    member _.``Modeling has no legacy BracketAnchorSpan wire type``() =
        let languageAssembly = typeof<TextEdit>.Assembly
        Assert.Null(languageAssembly.GetType("AIGuiders.Platform.Modeling.Language.BracketAnchorSpan"))

        let correspondenceAssembly = typeof<DocToCodeWitness>.Assembly
        Assert.Null(correspondenceAssembly.GetType("AIGuiders.Platform.Modeling.Documentation.Correspondence.ReverseAnchor"))

    [<Fact>]
    member _.``DocToCodeWitness uses typed CorrespondenceRelationKind not string Kind``() =
        let kindField = typeof<DocToCodeWitness>.GetProperty("Kind")
        Assert.NotNull kindField
        Assert.Equal(typeof<CorrespondenceRelationKind>, kindField.PropertyType)
