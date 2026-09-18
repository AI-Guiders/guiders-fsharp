namespace AIGuiders.Platform.Modeling.CodeCenter.Tests

open DashSpec.Modeling.CodeCenter

module ConformanceFixtures =

    let sampleText = "@dashboard demo\n    tab x as \"T\"\nend dashboard\n"

    let createSession documentId text =
        DashSpecCodeCenterSession.createDocumentSession documentId text

    let createDemoSession () = createSession "doc://demo" sampleText

    let rebuild text = DashSpecDocumentGraph.rebuildFromText text
