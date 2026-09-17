namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.IO
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet
open AIGuiders.Platform.Modeling.Language
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

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

    let hostLoadContentsFromDisk (ownership: Map<string, ProjectId>) =
        ownership
        |> Map.keys
        |> Seq.choose (fun path ->
            if File.Exists path then
                Some(path, File.ReadAllText path)
            else
                None)
        |> Map.ofSeq

    let private flushWrites (patch: SessionPatch) (registry: DocumentRegistry) (contents: Map<DocId, DocumentText>) =
        for path, _ in patch.FileSystem.Writes do
            match DocumentRegistryOps.resolvePath (LogicalPath.Create path) registry with
            | None -> ()
            | Some docId ->
                match Map.tryFind docId contents with
                | Some (DocumentText text) -> File.WriteAllText(normalizePath path, text)
                | None -> ()

    /// Apply Δ through <c>SessionOrchestrator</c>; disk read via slnx document ownership.
    let tryApplyPatch (anchorPath: string) (patch: SessionPatch) (sourceOverrides: Map<string, string>) : Result<unit, string> =
        if List.isEmpty patch.FileSystem.Writes && List.isEmpty patch.FileSystem.Replacements then
            Ok()
        elif String.IsNullOrWhiteSpace anchorPath || not (File.Exists anchorPath) then
            Result.Error "apply requires solution_or_project_path for SessionOrchestrator."
        else
            try
                let ownership = DotNetSlnxGraphPort.loadDocumentOwnership anchorPath

                let pathContents =
                    ownership
                    |> Map.toSeq
                    |> Seq.map (fun (path, _) ->
                        let text =
                            match Map.tryFind path sourceOverrides with
                            | Some t -> t
                            | None when File.Exists path -> File.ReadAllText path
                            | _ -> ""

                        path, text)

                let session = DotNetSlnxGraphPort.loadSession anchorPath
                let runtime = SessionOrchestrator.create session pathContents ownership

                match SessionOrchestrator.applyPatch runtime patch { Commit = None } with
                | PatchRejected reasons -> Result.Error(String.concat "; " reasons)
                | PatchApplied applied ->
                    flushWrites patch applied.Registry applied.Contents
                    Ok()
            with ex ->
                Result.Error ex.Message
