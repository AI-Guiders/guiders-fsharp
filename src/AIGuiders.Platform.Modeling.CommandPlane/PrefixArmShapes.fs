namespace AIGuiders.Platform.Modeling.CommandPlane

/// <summary>PAC disposition: no match / ready wire / arm constructor (GUIDERS-ADR-0038).</summary>
type PrefixArmDisposition =
    | NoMatch = 0
    | Ready = 1
    | ArmConstructor = 2

/// <summary>Result of a prefix-arm profile match (parity: PrefixArmMatch).</summary>
[<CLIMutable>]
type PrefixArmMatch =
    { Disposition: PrefixArmDisposition
      Wire: string
      DisplayTail: string
      RootConstructorId: string
      Segments: (string * string) list }

module PrefixArmMatch =

    let noMatch : PrefixArmMatch =
        { Disposition = PrefixArmDisposition.NoMatch
          Wire = ""
          DisplayTail = ""
          RootConstructorId = ""
          Segments = [] }

    let ready wire displayTail =
        { noMatch with Disposition = PrefixArmDisposition.Ready; Wire = wire; DisplayTail = displayTail }

    let armConstructor rootId displayTail segments =
        { noMatch with
            Disposition = PrefixArmDisposition.ArmConstructor
            RootConstructorId = rootId
            DisplayTail = displayTail
            Segments = segments }

/// <summary>Surface-neutral arg site for PAC profiles (parity: PrefixArmSite).</summary>
[<CLIMutable>]
type PrefixArmSite =
    { ArgHint: string
      Help: string
      ArgTailKind: string }