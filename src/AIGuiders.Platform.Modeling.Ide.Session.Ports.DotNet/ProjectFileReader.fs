namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet

open System.IO
open System.Xml.Linq

module ProjectFileReader =
    let private includeLocalNames = Set [ "Compile"; "Content"; "EmbeddedResource" ]

    let readProjectReferences (projectPath: string) =
        if not (File.Exists projectPath) then
            []
        else
            let dir =
                match Path.GetDirectoryName projectPath with
                | null -> ""
                | value -> value
            let doc = XDocument.Load projectPath

            doc.Descendants()
            |> Seq.filter (fun el -> el.Name.LocalName = "ProjectReference")
            |> Seq.choose (fun el ->
                match el.Attribute(XName.Get "Include") with
                | null -> None
                | attr when System.String.IsNullOrWhiteSpace attr.Value -> None
                | attr ->
                    let combined =
                        Path.GetFullPath(Path.Combine(dir, attr.Value.Replace('/', Path.DirectorySeparatorChar)))

                    Some combined)
            |> Seq.distinct
            |> Seq.toList

    let private isSdkStyle (doc: XDocument) =
        match doc.Root with
        | null -> false
        | root ->
            match root.Attribute(XName.Get "Sdk") with
            | null -> false
            | attr -> not (System.String.IsNullOrWhiteSpace attr.Value)

    let private readSdkStyleSources (dir: string) (projectPath: string) =
        let patterns =
            match Path.GetExtension projectPath with
            | ".fsproj" -> [ "*.fs" ]
            | ".csproj" -> [ "*.cs" ]
            | _ -> []

        patterns
        |> List.collect (fun pattern ->
            Directory.GetFiles(dir, pattern, SearchOption.AllDirectories)
            |> Array.toList)
        |> List.map Path.GetFullPath
        |> List.distinct

    let readSourceFiles (projectPath: string) =
        if not (File.Exists projectPath) then
            []
        else
            let dir =
                match Path.GetDirectoryName projectPath with
                | null -> ""
                | value -> value
            let doc = XDocument.Load projectPath

            let explicit =
                doc.Descendants()
                |> Seq.filter (fun el -> includeLocalNames.Contains el.Name.LocalName)
                |> Seq.choose (fun el ->
                    match el.Attribute(XName.Get "Include") with
                    | null -> None
                    | attr when System.String.IsNullOrWhiteSpace attr.Value -> None
                    | attr ->
                        let combined =
                            Path.GetFullPath(Path.Combine(dir, attr.Value.Replace('/', Path.DirectorySeparatorChar)))

                        Some combined)
                |> Seq.distinct
                |> Seq.toList

            if not (List.isEmpty explicit) then
                explicit
            elif isSdkStyle doc then
                readSdkStyleSources dir projectPath
            else
                []
