module AIGuiders.Platform.Modeling.Notations.Command.Tests.AttachInvocationTests

open Xunit
open AIGuiders.Platform.Modeling.Notations.Command

[<Fact>]
let ``AttachInvocation root path has no verb segment`` () =
    match AttachInvocation.tryParsePath "attach" with
    | None -> Assert.Fail("expected root attach path")
    | Some None -> ()
    | Some(Some _) -> Assert.Fail("expected verb-less root")

[<Theory>]
[<InlineData("error")>]
[<InlineData("code")>]
[<InlineData("manual")>]
let ``AttachInvocation parses verb tail`` verb =
    match AttachInvocation.tryParsePath $"attach {verb}" with
    | Some(Some wire) -> Assert.Equal(verb, wire)
    | _ -> Assert.Fail($"expected attach {verb}")

[<Fact>]
let ``AttachInvocation rejects unknown verb`` () =
    Assert.True(AttachInvocation.tryParsePath "attach unknown" |> Option.isNone)
