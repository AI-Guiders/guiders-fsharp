namespace AIGuiders.Platform.Modeling.Solution

open System
open System.Xml
open System.Xml.Linq

/// <summary>Parse/author the slnx wire (XML solution format, vs-solutionpersistence spec).</summary>
[<RequireQualifiedAccess>]
module SolutionNotation =

    let private xname (local: string) = XName.Get local

    let private attr (name: string) (el: XElement) : string =
        let a = el.Attribute(xname name)
        if isNull a then "" else a.Value

    /// <summary>Normalize a path to forward slashes (spec stores forward slashes).</summary>
    let normalizePath (p: string) : string = p.Replace('\\', '/').Trim()

    /// <summary>Parse slnx XML text. Unknown elements are tolerated; malformed XML → Error.</summary>
    let parse (xml: string) : Result<SolutionModel, string> =
        try
            let doc = XDocument.Parse(xml)
            let root = doc.Root
            if isNull root || root.Name.LocalName <> "Solution" then
                Error "root element is not Solution"
            else
                let projects =
                    root.Elements(xname "Project")
                    |> Seq.map (fun el ->
                        { Path = normalizePath (attr "Path" el)
                          Dependencies =
                            el.Descendants(xname "ProjectDependency")
                            |> Seq.map (fun d -> normalizePath (attr "Path" d))
                            |> Seq.toList })
                    |> Seq.distinctBy (fun p -> p.Path)
                    |> Seq.toList

                let folders =
                    root.Elements(xname "Folder")
                    |> Seq.map (fun el ->
                        { Name = attr "Name" el
                          Projects =
                            el.Elements(xname "Project")
                            |> Seq.map (fun p -> normalizePath (attr "Path" p))
                            |> Seq.toList })
                    |> Seq.toList

                let buildTypes =
                    root.Descendants(xname "BuildType")
                    |> Seq.map (fun el -> attr "Name" el)
                    |> Seq.toList

                let platforms =
                    root.Descendants(xname "Platform")
                    |> Seq.map (fun el -> attr "Name" el)
                    |> Seq.toList

                let properties =
                    root.Descendants(xname "Property")
                    |> Seq.choose (fun el ->
                        let n = el.Attribute(xname "Name")
                        let v = el.Attribute(xname "Value")
                        if isNull n || isNull v then None else Some (n.Value, v.Value))
                    |> Seq.toList

                let defaults =
                    if List.isEmpty buildTypes && List.isEmpty platforms then
                        SolutionModel.empty
                    else
                        { SolutionModel.empty with BuildTypes = []; Platforms = [] }

                Ok
                    { defaults with
                        Projects = projects
                        Folders = folders
                        BuildTypes = buildTypes
                        Platforms = platforms
                        Properties = properties }
        with ex ->
            Error ex.Message

    /// <summary>Author slnx XML text from a model (canonical element order: Configurations, Folders, Projects).</summary>
    let author (model: SolutionModel) : string =
        let root = XElement(xname "Solution")

        if not (List.isEmpty model.BuildTypes && List.isEmpty model.Platforms) then
            let cfg = XElement(xname "Configurations")
            if not (List.isEmpty model.BuildTypes) then
                let bt = XElement(xname "BuildTypes")
                model.BuildTypes |> List.iter (fun b -> bt.Add(XElement(xname "BuildType", XAttribute(xname "Name", b))))
                cfg.Add(bt)
            if not (List.isEmpty model.Platforms) then
                let pl = XElement(xname "Platforms")
                model.Platforms |> List.iter (fun p -> pl.Add(XElement(xname "Platform", XAttribute(xname "Name", p))))
                cfg.Add(pl)
            root.Add(cfg)

        model.Folders
        |> List.iter (fun f ->
            let el = XElement(xname "Folder", XAttribute(xname "Name", f.Name))
            f.Projects |> List.iter (fun p -> el.Add(XElement(xname "Project", XAttribute(xname "Path", p))))
            root.Add(el))

        model.Projects
        |> List.iter (fun p ->
            let el = XElement(xname "Project", XAttribute(xname "Path", normalizePath p.Path))
            p.Dependencies
            |> List.iter (fun d -> el.Add(XElement(xname "ProjectDependency", XAttribute(xname "Path", normalizePath d))))
            root.Add(el))

        model.Properties
        |> List.iter (fun (n, v) ->
            let container =
                let existing = root.Element(xname "Properties")
                if isNull existing then
                    let c = XElement(xname "Properties", XAttribute(xname "Name", "Solution Properties"))
                    root.Add(c)
                    c
                else existing
            container.Add(XElement(xname "Property", XAttribute(xname "Name", n), XAttribute(xname "Value", v))))

        let sb = Text.StringBuilder()
        let settings = XmlWriterSettings(Indent = true, OmitXmlDeclaration = true)
        use w = XmlWriter.Create(sb, settings)
        root.Save(w)
        w.Flush()
        sb.ToString()