module AIGuiders.Platform.Modeling.Notations.Command.Tests.InvocationConformanceTests

open System.Collections.Generic
open Xunit
open AIGuiders.Platform.Modeling.Notations.Command

[<Fact>]
let ``FromPathSegments: filters blanks and joins with single space`` () =
    let cmd = InvocationNotation.fromPathSegments [ "desk"; ""; "  "; "files"; "delete" ]
    Assert.Equal("desk files delete", cmd.CanonicalPath)
    Assert.Equal<string list>([ "desk"; "files"; "delete" ], List.ofSeq cmd.PathSegments)

[<Fact>]
let ``FromPathSegments: empty and all-blank inputs normalize to empty path`` () =
    let empty = InvocationNotation.fromPathSegments []
    Assert.Equal("", empty.CanonicalPath)
    Assert.Empty empty.PathSegments
    let blank = InvocationNotation.fromPathSegments [ ""; "   " ]
    Assert.Equal("", blank.CanonicalPath)
    Assert.Empty blank.PathSegments

[<Fact>]
let ``PathsEqual: case-insensitive on canonical path`` () =
    let a = InvocationNotation.fromPathSegments [ "Desk"; "Files" ]
    let b = InvocationNotation.fromPathSegments [ "desk"; "files" ]
    let c = InvocationNotation.fromPathSegments [ "desk"; "files"; "delete" ]
    Assert.True (InvocationNotation.pathsEqual a b)
    Assert.False (InvocationNotation.pathsEqual a c)

[<Fact>]
let ``Enums: wire values match C# IR`` () =
    Assert.Equal(1, int ArgMechanic.Picker)
    Assert.Equal(2, int ArgMechanic.FreeText)
    Assert.Equal(3, int ArgMechanic.Optional)
    Assert.Equal(4, int ArgMechanic.Constructor)
    Assert.Equal(5, int ArgMechanic.TypedInput)
    Assert.Equal(1, int InvocationEngageKind.Slash)
    Assert.Equal(2, int InvocationEngageKind.Melody)
    Assert.Equal(3, int InvocationEngageKind.Binding)
    Assert.Equal(0, int InvocationLinePhase.Path)
    Assert.Equal(1, int InvocationLinePhase.Arg)
    Assert.Equal(2, int InvocationLinePhase.Ready)

[<Fact>]
let ``NormalizedCommandLine: fromPathSegments preserves canonical path and segments`` () =
    let model = InvocationNotation.fromPathSegments [ "desk"; "files"; "delete" ]
    Assert.Equal("desk files delete", model.CanonicalPath)
    Assert.Equal<string list>([ "desk"; "files"; "delete" ], List.ofSeq model.PathSegments)

[<Fact>]
let ``NormalizedCommandLine: pathsEqual is case-insensitive on canonical path`` () =
    let a = InvocationNotation.fromPathSegments [ "Desk"; "Files" ]
    let b = InvocationNotation.fromPathSegments [ "desk"; "files" ]
    Assert.True (InvocationNotation.pathsEqual a b)