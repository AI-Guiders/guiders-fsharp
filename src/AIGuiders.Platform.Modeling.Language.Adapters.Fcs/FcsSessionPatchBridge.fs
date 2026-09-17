namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.Language

/// <summary>Pure FCS semantic edit ↔ federation <c>SessionPatch</c> mapping (Δ_fs writes @ θ_rename).</summary>
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

    /// Disk-backed apply — delegates to Execution-bound <see cref="IFcsSessionPatchApplier"/>.
    let tryApplyPatch (anchorPath: string) (patch: SessionPatch) (sourceOverrides: Map<string, string>) =
        FcsSessionPatchApply.tryApply anchorPath patch sourceOverrides
