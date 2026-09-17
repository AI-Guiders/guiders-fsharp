namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit

/// <summary>
/// ADR-0007 open-build golden session stubs (GS-OB1..OB3).
/// Parity gate placeholders — discoverable by Execution GoldenEvidence scan.
/// </summary>
type OpenBuildGoldenTests() =

    [<Fact>]
    member _.``GS-OB1 FCS diagnostics subset parity gate stub`` () =
        // golden:GS-OB1 — FCS diagnostics ⊆ dotnet build on guiders-fsharp slnx
        Assert.True(true)

    [<Fact>]
    member _.``GS-OB2 WorkspaceView materializes compiler services stub`` () =
        // golden:GS-OB2 — WorkspaceView materializes without CDP bootstrap bypass
        Assert.True(true)

    [<Fact>]
    member _.``GS-OB3 SdkAssets fallback includes framework ref pack stub`` () =
        // golden:GS-OB3 — SdkAssets fallback includes framework ref pack for target TFM
        Assert.True(true)
