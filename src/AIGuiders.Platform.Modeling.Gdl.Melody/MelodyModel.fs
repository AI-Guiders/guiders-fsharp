namespace AIGuiders.Platform.Modeling.Gdl.Melody

open System
open System.Collections.Generic

/// <summary>How one step in a melody line is played (GUIDERS-ADR-0015 §7).</summary>
type MelodyArticulation =
    /// <summary>Single key after chord root (e.g. b).</summary>
    | ByNote = 0
    /// <summary>Simultaneous modifier+key as one step (e.g. Ctrl+R).</summary>
    | ByChord = 1

/// <summary>Line-level capture and validation policy for a melody (GUIDERS-ADR-0015 §7).</summary>
type MelodyLineProfile =
    /// <summary>Every step is ByNote (CIDE/Glass default).</summary>
    | PureByNote = 0
    /// <summary>Every step is ByChord.</summary>
    | PureByChord = 1
    /// <summary>Explicit hybrid line — steps may mix note and chord articulation.</summary>
    | Mixed = 2

/// <summary>One step in a melody line after chord root is engaged.</summary>
type MelodyStep =
    { Articulation: MelodyArticulation
      /// <summary>Note character or chord wire (e.g. b, Ctrl+R).</summary>
      Wire: string
      /// <summary>Optional tail slot parser id for parametric steps.</summary>
      ReaderId: string option }

/// <summary>Sequential play line for one melody alias (slug + steps + profile).</summary>
type MelodyLine =
    { Slug: string
      Profile: MelodyLineProfile
      Steps: IReadOnlyList<MelodyStep>
      /// <summary>Parametric argument notation after slug resolves (ArgumentNotationProfile wire — execution-side).</summary>
      Help: string option }

/// <summary>
/// Catalog projection for melody mechanic — keyboard line after chord root (GUIDERS-ADR-0015).
/// Palette c: discoverability reuses slug/Help; it is not this descriptor's execution surface.
/// </summary>
type MelodyDescriptor =
    { CommandId: string
      Slug: string
      Profile: MelodyLineProfile
      Steps: IReadOnlyList<MelodyStep>
      Help: string option }

module MelodyDescriptor =

    /// <summary>Descriptor → play line projection (C# ToLine).</summary>
    let toLine (d: MelodyDescriptor) : MelodyLine =
        { Slug = d.Slug
          Profile = d.Profile
          Steps = d.Steps
          Help = d.Help }

    /// <summary>Slug-only descriptor with PureByNote default (C# FromSlug).</summary>
    let fromSlug (commandId: string) (slug: string) (help: string option) : MelodyDescriptor =
        { CommandId = commandId
          Slug = slug
          Profile = MelodyLineProfile.PureByNote
          Steps = [||] :> IReadOnlyList<MelodyStep>
          Help = help }