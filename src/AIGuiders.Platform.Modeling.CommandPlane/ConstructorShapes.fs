namespace AIGuiders.Platform.Modeling.CommandPlane

/// <summary>Leaf constructor segment: id + label + wire/display min widths.</summary>
[<CLIMutable>]
type ConstructorSegmentDef =
    { SegmentId: string
      Label: string
      WireMinWidth: int
      DisplayMinWidth: int }

/// <summary>Leaf constructor — emits one wire part from segments (parity: LeafConstructorDefinition).</summary>
[<CLIMutable>]
type LeafConstructorDef =
    { Id: string
      Label: string
      Segments: ConstructorSegmentDef list
      WirePattern: string
      DisplayPattern: string }

/// <summary>Composite slot — binds a slot id to a leaf constructor (parity: ConstructorSlotDefinition).</summary>
[<CLIMutable>]
type ConstructorSlotDef =
    { SlotId: string
      ConstructorId: string
      SeparatorBefore: string }

/// <summary>Composite constructor — combines leaf slots into one wire value (parity: CompositeConstructorDefinition).</summary>
[<CLIMutable>]
type CompositeConstructorDef =
    { Id: string
      Label: string
      Slots: ConstructorSlotDef list
      WirePattern: string }

/// <summary>Constructor registry (parity: ValueConstructorRegistry, pure part).</summary>
module ConstructorRegistry =

    type ConstructorDef =
        | Leaf of LeafConstructorDef
        | Composite of CompositeConstructorDef

        member this.Id =
            match this with
            | Leaf leaf -> leaf.Id
            | Composite composite -> composite.Id

    type Registry = Map<string, ConstructorDef>

    let empty : Registry = Map.empty

    let register (def: ConstructorDef) (registry: Registry) : Registry =
        Map.add def.Id def registry

    let tryGet (id: string) (registry: Registry) : ConstructorDef option =
        Map.tryFind id registry

/// <summary>Step-completion row for command surfaces (parity: ArgCompletionItem, pure part).</summary>
[<CLIMutable>]
type ArgCompletionRow =
    { InsertText: string
      CommandPath: string
      Help: string
      StepSegment: string
      PickValue: string }

type ArgCompletionKind =
    | Segment = 0
    | Picker = 1
    | ConstructorEntry = 2
    | ConstructorStep = 3