namespace AIGuiders.Gdl.Authoring

open System.IO
open System.Text

type AuthoringLine = { LineNumber: int; Text: string }

[<RequireQualifiedAccess>]
module AuthoringSource =
    let stripComment (line: string) =
        match line.IndexOf '#' with
        | -1 -> line
        | i -> line.Substring(0, i).TrimEnd()

    let fromRawLines (rawLines: string list) =
        rawLines
        |> List.mapi (fun index text ->
            { LineNumber = index + 1
              Text = stripComment text })

    let fromText (text: string) =
        text.Replace("\r\n", "\n").Split '\n' |> Array.toList |> fromRawLines

    let fromFile (path: string) =
        fromText (File.ReadAllText(path, Encoding.UTF8))
