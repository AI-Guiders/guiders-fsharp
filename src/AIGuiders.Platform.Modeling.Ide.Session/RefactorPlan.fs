namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

/// Pure plan functions: θ → Δ = (Δ_fs, Δ_G). Preview = plan only; apply via SessionPatch.apply.
module RefactorPlan =
    type RenameSymbol =
        { OldName: string
          NewName: string
          Files: string list }

    type MoveTypeToFile =
        { TypeName: string
          SourcePath: string
          TargetPath: string
          Owner: ProjectId
          UpdatedSourceContents: string
          ExtractedContents: string }

    type MovePath = { From: string; To: string }

    let planRename (registry: DocumentRegistry) (contents: Map<DocId, DocumentText>) (spec: RenameSymbol) : SessionPatch =
        let replacements =
            spec.Files
            |> List.choose (fun path ->
                match DocumentRegistryOps.resolvePath (LogicalPath.Create path) registry with
                | None -> None
                | Some docId ->
                    match Map.tryFind docId contents with
                    | None -> None
                    | Some (DocumentText text) when text.Contains spec.OldName ->
                        Some
                            { DocId = docId
                              Old = spec.OldName
                              New = spec.NewName }
                    | Some _ -> None)

        { FileSystem =
            { Replacements = replacements
              PathRenames = []
              Writes = []
              Deletes = [] }
          Graph = GraphStructurePatch.empty }

    let planMoveTypeToFile (spec: MoveTypeToFile) : SessionPatch =
        { FileSystem =
            { Replacements = []
              PathRenames = []
              Writes =
                [ spec.SourcePath, spec.UpdatedSourceContents
                  spec.TargetPath, spec.ExtractedContents ]
              Deletes = [] }
          Graph =
            { GraphStructurePatch.empty with
                DocumentAssignments = [ spec.TargetPath, spec.Owner ] } }

    let planMovePath (spec: MovePath) : SessionPatch =
        { FileSystem =
            { Replacements = []
              PathRenames = [ spec.From, spec.To ]
              Writes = []
              Deletes = [] }
          Graph = GraphStructurePatch.empty }
