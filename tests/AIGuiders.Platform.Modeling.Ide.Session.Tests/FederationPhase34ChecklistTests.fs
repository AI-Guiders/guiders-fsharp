namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open System
open System.IO
open System.Text.RegularExpressions
open Xunit
open AIGuiders.Platform.Modeling.CommandPlane
open AIGuiders.Platform.Modeling.Language.Adapters.Fcs

/// Plan §10 Phase 3–4 verification gate — progress toward TO-BE, not checklist tick theater.
type FederationPhase34ChecklistTests() =

    let modelingSrcRoot =
        Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src") |> Path.GetFullPath

    let forbiddenIoPatterns =
        [| Regex(@"\bFile\.")
           Regex(@"\bDirectory\.")
           Regex(@"System\.IO\.File")
           Regex(@"System\.IO\.Directory") |]

    let isModelingPackage (path: string) =
        path.IndexOf("AIGuiders.Platform.Modeling.", StringComparison.OrdinalIgnoreCase) >= 0

    let lineIsComment (line: string) =
        let trimmed = line.TrimStart()
        trimmed.StartsWith("//") || trimmed.StartsWith("(*")

    [<Fact>]
    member _.``Modeling packages contain no direct File or Directory IO``() =
        let violations = ResizeArray()

        if not (Directory.Exists modelingSrcRoot) then
            Assert.Fail $"modeling src root not found: {modelingSrcRoot}"

        for file in Directory.EnumerateFiles(modelingSrcRoot, "*.fs", SearchOption.AllDirectories) do
            if isModelingPackage file then
                let lines = File.ReadAllLines file

                for i in 0 .. lines.Length - 1 do
                    let line = lines.[i]

                    if not (lineIsComment line) then
                        for pattern in forbiddenIoPatterns do
                            if pattern.IsMatch line then
                                violations.Add($"{file}:{i + 1}:{line.Trim()}")

        if violations.Count > 0 then
            Assert.Fail(String.Join(Environment.NewLine, violations))

    [<Fact>]
    member _.``AttachSchema catalog and conformance spec remain wired``() =
        Assert.Equal(6, AttachSchemaCatalog.allVerbs.Length)

        let specPath =
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "..",
                "..",
                "docs",
                "conformance",
                "commandplane",
                "attach-schema.spec.json"
            )
            |> Path.GetFullPath

        Assert.True(File.Exists specPath)

    [<Fact>]
    member _.``FCS modeling ports remain pure shapes without host IO types``() =
        Assert.True(typeof<IFcsSourceTextSource>.IsPublic)
        Assert.True(typeof<IFcsSolutionGraphSource>.IsPublic)
        Assert.True(typeof<IFcsProjectOwnershipSource>.IsPublic)
        Assert.True(typeof<IFcsSessionPatchApplier>.IsPublic)
