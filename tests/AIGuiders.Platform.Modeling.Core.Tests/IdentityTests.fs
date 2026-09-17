module AIGuiders.Platform.Modeling.Core.Tests.IdentityTests

open System
open Xunit
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Paths

[<Fact>]
let ``NumericId: ofCounter round-trips`` () =
    let id = NumericId.ofCounter 42L
    Assert.Equal(42L, NumericId.value id)

[<Fact>]
let ``DocId and DiagnosticRef do not share carrier space at type level`` () =
    let doc = DocId.mint (NumericId.ofCounter 1L)
    let diag = DiagnosticRef.mint (NumericId.ofCounter 1L)
    Assert.Equal(NumericId.ofCounter 1L, DocId.carrier doc)
    Assert.Equal(NumericId.ofCounter 1L, DiagnosticRef.carrier diag)
    Assert.NotEqual<DocId>(doc, DocId.mint (NumericId.ofCounter 2L))

[<Fact>]
let ``CarrierWire.parseNumeric accepts bigint ingress`` () =
    match CarrierWire.parseNumeric (CarrierWire.Numeric 999I) with
    | Ok id -> Assert.Equal(999L, NumericId.value id)
    | Error e -> Assert.Fail e

[<Fact>]
let ``CarrierWire.parseNumeric rejects non-numeric wire`` () =
    match CarrierWire.parseNumeric (CarrierWire.Text "x") with
    | Ok _ -> Assert.Fail "expected error"
    | Error _ -> ()

[<Fact>]
let ``ProjectId wraps normalized LogicalPath`` () =
    let pid = ProjectId.create (LogicalPath.Create @"src\Foo.fs")
    Assert.Equal("src/Foo.fs", (ProjectId.path pid).Value)

[<Fact>]
let ``parseCommit accepts 64-char hex as Sha256`` () =
    let hex = String('a', 64)
    match ParseCommit.parse hex with
    | Ok ref ->
        Assert.Equal(32, (Sha256.bytes (CommitRef.carrier ref)).Length)
    | Error e -> Assert.Fail e

[<Fact>]
let ``parseCommit rejects invalid hash`` () =
    match ParseCommit.parse "not-a-hash" with
    | Ok _ -> Assert.Fail "expected error"
    | Error _ -> ()

[<Fact>]
let ``GitPin carries optional CommitRef`` () =
    let hex = String('b', 64)
    match ParseCommit.parse hex with
    | Ok commit ->
        let pin = { GitPin.Commit = Some commit }
        Assert.True pin.Commit.IsSome
    | Error e -> Assert.Fail e
