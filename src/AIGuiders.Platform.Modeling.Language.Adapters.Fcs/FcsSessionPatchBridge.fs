namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.Language

/// <summary>Map FCS semantic edits into federation <c>SessionPatch</c> (Δ_fs writes @ θ_rename).</summary>
module FcsSessionPatchBridge =
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
