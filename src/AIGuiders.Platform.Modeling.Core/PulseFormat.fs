namespace AIGuiders.Platform.Modeling.Core

open System

[<RequireQualifiedAccess>]
module PulseFormat =
    [<Literal>]
    let DefaultMaxChars = 240

    [<Literal>]
    let InventoryObserveMaxChars = 480

    [<CompiledName("Truncate")>]
    let truncate (value: string) (maxChars: int) =
        if String.IsNullOrEmpty value then
            value
        elif value.Length <= maxChars then
            value
        else
            value.Substring(0, maxChars) + "…"

    [<CompiledName("JoinBits")>]
    let joinBits (bits: seq<string>) (maxChars: int) =
        bits
        |> Seq.filter (fun s -> not (String.IsNullOrWhiteSpace s))
        |> Seq.map (fun s -> s.Trim())
        |> String.concat " "
        |> fun s -> truncate s maxChars
