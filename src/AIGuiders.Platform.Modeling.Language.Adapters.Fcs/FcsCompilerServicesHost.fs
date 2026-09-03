namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.Collections.Concurrent
open AIGuiders.Platform.Modeling.Ide.Session

/// <summary>Materialize F# CompilerServices from orchestrator <c>WorkspaceView</c> @ revision.</summary>
module FcsCompilerServicesHost =
    let private views =
        ConcurrentDictionary<string, WorkspaceView>(StringComparer.OrdinalIgnoreCase)

    let tryGetView (anchorPath: string) =
        let key = if String.IsNullOrWhiteSpace anchorPath then "" else anchorPath.Trim()

        match views.TryGetValue key with
        | true, view -> Some view
        | false, _ -> None

    let materialize (view: WorkspaceView) =
        let key =
            if String.IsNullOrWhiteSpace view.AnchorPath then
                ""
            else
                view.AnchorPath.Trim()

        views[key] <- view

        for project in view.Projects do
            if String.Equals(project.LanguageId, "fsharp", StringComparison.OrdinalIgnoreCase) then
                FcsProjectOptions.warm project.ProjectPath

    let invalidate (anchorPath: string option) =
        match anchorPath with
        | None -> views.Clear()
        | Some path when not (String.IsNullOrWhiteSpace path) ->
            views.TryRemove(path.Trim()) |> ignore
        | Some _ -> ()
