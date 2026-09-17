module AIGuiders.Platform.Modeling.Language.Tests.LanguageEditsTests

open Xunit
open AIGuiders.Platform.Modeling.Language

[<Fact>]
let ``BufferEditOutcome fromText carries selection`` () =
    let o = BufferEditOutcome.fromText "hello" 0 5
    Assert.Equal(Some "hello", o.Text)
    Assert.Equal(Some 0, o.SelectionStart)
    Assert.Equal(Some 5, o.SelectionEnd)
