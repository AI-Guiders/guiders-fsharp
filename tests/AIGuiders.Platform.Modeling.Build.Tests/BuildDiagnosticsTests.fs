module AIGuiders.Platform.Modeling.Build.Tests.BuildDiagnosticsTests

open Xunit
open AIGuiders.Platform.Modeling.Build
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

[<Fact>]
let ``BuildDiagnostic carries RelationSpec witness field`` () =
    let spec = RelationSpec.Diag(DiagnosticRef.mint(NumericId.ofCounter 1L))

    let shaped =
        { File = "src/Foo.fs"
          Line = 12
          Column = 3
          Code = "CS0246"
          Message = "type not found"
          Spec = spec }

    match shaped.Spec with
    | RelationSpec.Diag _ -> Assert.True true
    | _ -> Assert.Fail "expected Diag relation spec"
