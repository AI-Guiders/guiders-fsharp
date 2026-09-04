namespace AIGuiders.Platform.Modeling.Gdl.Binding

open System
open AIGuiders.Platform.Modeling.Notations.Keyboard

type BindingTargetKind =
    | Command = 0
    | ChordRoot = 1
    | SurfaceOpener = 2

type BindingDocumentFormat =
    | Toml = 0
    | Json = 1

/// <summary>Well-known binding keys shared across catalogs.</summary>
module BindingWellKnownKeys =

    [<Literal>]
    let CascadeChord = "cascade_chord"

/// <summary>Binding catalog row: key + gesture wire + target kind.</summary>
type BindingDescriptor =
    { BindingKey: string
      GestureWire: string
      TargetKind: BindingTargetKind }

module BindingDescriptor =

    /// <summary>Flat catalog entry → descriptor; cascade_chord key upgrades to ChordRoot (C# FromFlatEntry).</summary>
    let fromFlatEntry (bindingKey: string) (gestureWire: string) : BindingDescriptor =
        if String.IsNullOrWhiteSpace bindingKey then
            invalidArg (nameof bindingKey) "Binding key is null or whitespace."
        if String.IsNullOrWhiteSpace gestureWire then
            invalidArg (nameof gestureWire) "Gesture wire is null or whitespace."
        let kind =
            if String.Equals(bindingKey, BindingWellKnownKeys.CascadeChord, StringComparison.OrdinalIgnoreCase)
            then BindingTargetKind.ChordRoot
            else BindingTargetKind.Command
        { BindingKey = bindingKey.Trim()
          GestureWire = gestureWire.Trim()
          TargetKind = kind }

    /// <summary>Command id — only command-target rows resolve to one (C# CommandId).</summary>
    let commandId (d: BindingDescriptor) : string option =
        if d.TargetKind = BindingTargetKind.Command then Some d.BindingKey else None

/// <summary>Binding entry: descriptor + optional normalized gesture.</summary>
type BindingEntry =
    { Descriptor: BindingDescriptor
      NormalizedGesture: NormalizedKeySequence option }