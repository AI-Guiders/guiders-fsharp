namespace AIGuiders.Platform.Modeling.Notations

open System

/// <summary>Universal KV wire atom: Key + Sign + Value (GUIDERS-ADR-0021/0026).</summary>
[<CLIMutable>]
type NotationKvPair =
    { Key: string
      Sign: char
      Value: string }

[<RequireQualifiedAccess>]
module NotationKvPair =

    /// <summary>Split on first sign only (value may contain more signs).</summary>
    let trySplitFirst (segment: string) (sign: char) : Result<NotationKvPair, string> =
        if String.IsNullOrWhiteSpace segment then Error "Empty segment."
        else
            let trimmed = segment.Trim()
            let index = trimmed.IndexOf sign
            if index <= 0 then Error $"Missing KV sign '{sign}'."
            else
                Ok
                    { Key = trimmed.Substring(0, index).Trim()
                      Sign = sign
                      Value = trimmed.Substring(index + 1).Trim() }

/// <summary>Split list segments respecting nested bracket depth (CDP BracketLocate parity).</summary>
[<RequireQualifiedAccess>]
module NotationListSplit =

    let splitTopLevel (text: string) (separator: char) (openBracket: char) (closeBracket: char) : string list =
        let parts = ResizeArray<string>()
        let mutable depth = 0
        let mutable start = 0
        for i in 0 .. text.Length - 1 do
            let c = text.[i]
            if c = openBracket then depth <- depth + 1
            elif c = closeBracket then depth <- max 0 (depth - 1)
            elif c = separator && depth = 0 then
                parts.Add(text.Substring(start, i - start))
                start <- i + 1
        parts.Add(text.Substring start)
        List.ofSeq parts
