namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.Collections.Concurrent
open System.IO
open AIGuiders.Platform.Modeling.Ide.Session
open FSharp.Compiler.CodeAnalysis

/// <summary>Materialize F# CompilerServices from <c>WorkspaceView</c> @ revision — MSBuild/ProjInfo once, then frozen.</summary>
module FcsCompilerServicesHost =
    let private projInfo = FcsProjInfoProjectOptionsSource()

    let private views =
        ConcurrentDictionary<string, WorkspaceView>(StringComparer.OrdinalIgnoreCase)

    let private optionsByProject =
        ConcurrentDictionary<string, FSharpProjectOptions>(StringComparer.OrdinalIgnoreCase)

    let private normalizeProject (projectPath: string) =
        if String.IsNullOrWhiteSpace projectPath then
            ""
        else
            Path.GetFullPath (projectPath.Trim())

    let tryGetView (anchorPath: string) =
        let key = if String.IsNullOrWhiteSpace anchorPath then "" else anchorPath.Trim()

        match views.TryGetValue key with
        | true, view -> Some view
        | false, _ -> None

    let tryGetOptions (projectPath: string) =
        let key = normalizeProject projectPath

        if String.IsNullOrWhiteSpace key then
            None
        else
            match optionsByProject.TryGetValue key with
            | true, options -> Some options
            | false, _ -> None

    let private materializeProject (project: WorkspaceProjectView) =
        if not (String.Equals(project.LanguageId, "fsharp", StringComparison.OrdinalIgnoreCase)) then
            project
        else
            match (projInfo :> IFcsProjectOptionsSource).TryLoad project.ProjectPath with
            | Ok options ->
                let key = normalizeProject project.ProjectPath
                optionsByProject[key] <- options

                { project with
                    CompileFiles = options.SourceFiles |> Array.toList }
            | Error err ->
                failwith $"F# compiler services materialize failed for '{project.ProjectPath}': {err.Message}"

    /// Query MSBuild once per F# project @ revision; freeze CompileFiles + FSharpProjectOptions.
    let materialize (view: WorkspaceView) =
        let key =
            if String.IsNullOrWhiteSpace view.AnchorPath then
                ""
            else
                view.AnchorPath.Trim()

        let projects = view.Projects |> List.map materializeProject
        let enriched = { view with Projects = projects }
        views[key] <- enriched
        enriched

    let invalidate (anchorPath: string option) =
        match anchorPath with
        | None ->
            views.Clear()
            optionsByProject.Clear()
        | Some path when not (String.IsNullOrWhiteSpace path) ->
            let key = path.Trim()

            match views.TryRemove key with
            | true, removed ->
                for project in removed.Projects do
                    let projectKey = normalizeProject project.ProjectPath

                    if not (String.IsNullOrWhiteSpace projectKey) then
                        optionsByProject.TryRemove projectKey |> ignore
            | false, _ -> ()
        | Some _ -> ()
