namespace AIGuiders.Platform.Modeling.Notations.Keyboard

open System.Collections.Generic

[<System.Flags>]
type ChordModifierKeys =
    | None = 0
    | Control = 1
    | Alt = 2
    | Shift = 4
    | Meta = 8

[<AbstractClass>]
type NormalizedSequenceStep() = class end

type NormalizedChordStep(modifiers: ChordModifierKeys, keySymbol: string) =
    inherit NormalizedSequenceStep()
    member val Modifiers = modifiers with get, set
    member val KeySymbol = keySymbol with get, set

    override _.Equals(other: obj) =
        match other with
        | :? NormalizedChordStep as o -> o.Modifiers = modifiers && o.KeySymbol = keySymbol
        | _ -> false

    override _.GetHashCode() = System.HashCode.Combine(int modifiers, keySymbol)

    override _.ToString() = $"NormalizedChordStep {{ KeySymbol = {keySymbol}, Modifiers = {modifiers} }}"

type NormalizedPlainKeyStep(keySymbol: string) =
    inherit NormalizedSequenceStep()
    member val KeySymbol = keySymbol with get, set

    override _.Equals(other: obj) =
        match other with
        | :? NormalizedPlainKeyStep as o -> o.KeySymbol = keySymbol
        | _ -> false

    override _.GetHashCode() = keySymbol.GetHashCode()

    override _.ToString() = $"NormalizedPlainKeyStep {{ KeySymbol = {keySymbol} }}"

type NormalizedKeySequence(steps: IReadOnlyList<NormalizedSequenceStep>) =
    member val Steps = steps with get, set

    static member Empty = NormalizedKeySequence([||] :> IReadOnlyList<_>)
