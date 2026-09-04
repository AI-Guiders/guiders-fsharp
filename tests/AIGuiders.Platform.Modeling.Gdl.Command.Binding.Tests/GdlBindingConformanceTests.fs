module AIGuiders.Platform.Modeling.Gdl.Command.Binding.Tests.ConformanceTests

open System
open System.Collections.Generic
open Xunit
open AIGuiders.Platform.Modeling.Notations.Keyboard
open AIGuiders.Platform.Modeling.Gdl.Command.Binding

[<Fact>]
let ``FromFlatEntry: command key keeps Command target and trims`` () =
    let d = BindingDescriptor.fromFlatEntry " desk/files/delete " " ctrl+shift+p "
    Assert.Equal("desk/files/delete", d.BindingKey)
    Assert.Equal("ctrl+shift+p", d.GestureWire)
    Assert.Equal(BindingTargetKind.Command, d.TargetKind)

[<Fact>]
let ``FromFlatEntry: cascade_chord key upgrades to ChordRoot (case-insensitive)`` () =
    let lower = BindingDescriptor.fromFlatEntry "cascade_chord" "g"
    Assert.Equal(BindingTargetKind.ChordRoot, lower.TargetKind)
    let upper = BindingDescriptor.fromFlatEntry "CASCADE_CHORD" "g"
    Assert.Equal(BindingTargetKind.ChordRoot, upper.TargetKind)

[<Fact>]
let ``FromFlatEntry: blank key or gesture wire throws`` () =
    Assert.Throws<ArgumentException>(fun () -> BindingDescriptor.fromFlatEntry "" "g" |> ignore)
    Assert.Throws<ArgumentException>(fun () -> BindingDescriptor.fromFlatEntry "  " "g" |> ignore)
    Assert.Throws<ArgumentException>(fun () -> BindingDescriptor.fromFlatEntry "k" null |> ignore)
    Assert.Throws<ArgumentException>(fun () -> BindingDescriptor.fromFlatEntry "k" "   " |> ignore)

[<Fact>]
let ``CommandId: only command-target rows resolve`` () =
    let cmd = BindingDescriptor.fromFlatEntry "desk/op" "g"
    Assert.Equal(Some "desk/op", BindingDescriptor.commandId cmd)
    let chord = BindingDescriptor.fromFlatEntry BindingWellKnownKeys.CascadeChord "g"
    Assert.True (BindingDescriptor.commandId chord).IsNone

[<Fact>]
let ``WellKnownKeys: cascade chord wire constant`` () =
    Assert.Equal("cascade_chord", BindingWellKnownKeys.CascadeChord)

[<Fact>]
let ``NormalizedKeySequence: empty has no steps; chord and plain steps compose`` () =
    Assert.Equal(0, Seq.length NormalizedKeySequence.Empty.Steps)
    let seq =
        NormalizedKeySequence(
            [| NormalizedChordStep(ChordModifierKeys.Control ||| ChordModifierKeys.Shift, "p") :> NormalizedSequenceStep
               NormalizedPlainKeyStep "escape" :> NormalizedSequenceStep |]
            :> IReadOnlyList<NormalizedSequenceStep>)
    Assert.Equal(2, Seq.length seq.Steps)

[<Fact>]
let ``BindingEntry: descriptor with optional normalized gesture`` () =
    let d = BindingDescriptor.fromFlatEntry "desk/op" "ctrl+p"
    let withGesture = { Descriptor = d; NormalizedGesture = Some NormalizedKeySequence.Empty }
    Assert.Same(NormalizedKeySequence.Empty, withGesture.NormalizedGesture.Value)
    let bare = { Descriptor = d; NormalizedGesture = None }
    Assert.True bare.NormalizedGesture.IsNone

[<Fact>]
let ``Enums: binding target and document format values`` () =
    Assert.Equal(0, int BindingTargetKind.Command)
    Assert.Equal(1, int BindingTargetKind.ChordRoot)
    Assert.Equal(2, int BindingTargetKind.SurfaceOpener)
    Assert.Equal(0, int BindingDocumentFormat.Toml)
    Assert.Equal(1, int BindingDocumentFormat.Json)