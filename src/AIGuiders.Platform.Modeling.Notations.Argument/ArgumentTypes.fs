namespace AIGuiders.Platform.Modeling.Notations.Argument

open System
open System.Collections.Generic
open System.Runtime.InteropServices

type ArgumentSlotKind =
    | Flag = 0
    | Value = 1
    | Positional = 2

type ArgumentSlot
    (
        name: string,
        [<Optional; DefaultParameterValue(ArgumentSlotKind.Value)>] kind: ArgumentSlotKind,
        [<Optional; DefaultParameterValue("")>] longOption: string,
        [<Optional; DefaultParameterValue("")>] shortOption: string
    ) =
    member val Name = name with get, set
    member val Kind = kind with get, set
    member val LongOption = longOption with get, set
    member val ShortOption = shortOption with get, set

[<AllowNullLiteral>]
type ArgumentNotationProfile
    (
        [<Optional; DefaultParameterValue("")>] readerId: string,
        [<Optional; DefaultParameterValue(null: IReadOnlyList<ArgumentSlot>)>] slots: IReadOnlyList<ArgumentSlot>
    ) =
    member val ReaderId = readerId with get, set
    member val Slots = slots with get, set

    member this.IsEmpty =
        String.IsNullOrWhiteSpace this.ReaderId
        && (isNull this.Slots || this.Slots.Count = 0)

    static member Merge(existing: ArgumentNotationProfile, incoming: ArgumentNotationProfile) =
        if isNull incoming || incoming.IsEmpty then existing
        elif isNull existing || existing.IsEmpty then incoming
        else
            let reader =
                if String.IsNullOrWhiteSpace incoming.ReaderId then existing.ReaderId
                else incoming.ReaderId
            let slots =
                if not (isNull incoming.Slots) && incoming.Slots.Count > 0 then incoming.Slots else existing.Slots
            ArgumentNotationProfile(reader, slots)

[<RequireQualifiedAccess>]
module ArgumentReaders =
    [<Literal>]
    let Kv = "kv"
    [<Literal>]
    let Cli = "cli"
    [<Literal>]
    let Positional = "positional"
    [<Literal>]
    let Delimited = "delimited"
    [<Literal>]
    let Colon = "colon"
    [<Literal>]
    let Raw = "raw"

type NormalizedArguments
    (
        [<Optional; DefaultParameterValue("")>] raw: string,
        [<Optional; DefaultParameterValue(null: IReadOnlyDictionary<string, string>)>] slots: IReadOnlyDictionary<string, string>,
        [<Optional; DefaultParameterValue("")>] readerId: string
    ) =
    member val Raw = raw with get, set
    member val Slots = slots with get, set
    member val ReaderId = readerId with get, set

    static member FromRaw(raw: string, [<Optional; DefaultParameterValue("")>] readerId: string) =
        NormalizedArguments(raw, null, readerId)

    static member FromSlots(slots: IReadOnlyDictionary<string, string>, [<Optional; DefaultParameterValue("")>] readerId: string) =
        NormalizedArguments(null, slots, readerId)
