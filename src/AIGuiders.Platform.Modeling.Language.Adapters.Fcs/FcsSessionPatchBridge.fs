namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.IO
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet
open AIGuiders.Platform.Modeling.Language

/// <summary>Map FCS semantic edits into federation <c>SessionPatch</c> (Δ_fs writes @ θ_rename).</summary>
module FcsSessionPatchBridge =
    let private normalizePath path = Path.GetFullPath path

    let renameFileChangesToPatch (changes: RenameFileChange[]) : SessionPatch =
        let writes =
            changes
            |> Array.map (fun change -> change.Path, change.NewText)
            |> Array.toList

        { FileSystem =
            { Replacements = []
              PathRenames = []
              Writes = writes
              Deletes = [] }
          Graph = GraphStructurePatch.empty }

    let patchToRenameFileChanges (patch: SessionPatch) : RenameFileChange[] =
        patch.FileSystem.Writes
        |> List.map (fun (path, text) -> { Path = path; NewText = text })
        |> List.toArray

    let private mergeSourceOverrides (contents: Map<string, string>) (overrides: Map<string, string>) =
        overrides
        |> Map.fold (fun acc path text -> Map.add (normalizePath path) text acc) contents

    let private flushWrites (patch: SessionPatch) (contents: Map<string, string>) =
        for path, _ in patch.FileSystem.Writes do
            let full = normalizePath path

            match Map.tryFind full contents with
            | Some text -> File.WriteAllText(full, text)
            | None ->
                match
                    contents
                    |> Map.tryFindKey (fun key _ ->
                        String.Equals(normalizePath key, full, StringComparison.OrdinalIgnoreCase))
                with
                | Some key -> File.WriteAllText(normalizePath key, contents.[key])
                | None -> ()

    /// Apply Δ through <c>SessionOrchestrator</c>, then flush touched files to disk (host IO).
    let tryApplyPatch (anchorPath: string) (patch: SessionPatch) (sourceOverrides: Map<string, string>) : Result<unit, string> =
        if List.isEmpty patch.FileSystem.Writes && List.isEmpty patch.FileSystem.Replacements then
            Ok()
        elif String.IsNullOrWhiteSpace anchorPath || not (File.Exists anchorPath) then
            Result.Error "apply requires solution_or_project_path for SessionOrchestrator."
        else
            try
                let session = DotNetSlnxGraphPort.loadSession anchorPath
                let graph = session.Graph
                let baseContents = SessionOrchestrator.loadContentsFromDisk graph
                let contents = mergeSourceOverrides baseContents sourceOverrides
                let runtime = SessionOrchestrator.create session contents

                match SessionOrchestrator.applyPatch runtime patch { Commit = None } with
                | PatchRejected reasons -> Result.Error(String.concat "; " reasons)
                | PatchApplied applied ->
                    flushWrites patch applied.Contents
                    Ok()
            with ex ->
                Result.Error ex.Message
