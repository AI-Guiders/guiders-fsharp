module AIGuiders.Platform.Modeling.Build.Tests.BuildDiagnosticsTests

open Xunit
open AIGuiders.Platform.Modeling.Build
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

[<Fact>]
let ``shape emits RelationSpec Diag without anchor wire`` () =
    let raw =
        [| { File = "src/Foo.fs"
             Line = 12
             Column = 3
             Code = "CS0246"
             Message = "type not found" } |]

    let shaped = BuildDiagnostics.shape raw

    Assert.Equal(1, shaped.Length)
    Assert.Equal("src/Foo.fs", shaped.[0].File)
    Assert.Equal("CS0246", shaped.[0].Code)

    match shaped.[0].Spec with
    | RelationSpec.Diag _ -> Assert.True true
    | _ -> Assert.Fail "expected Diag relation spec"
