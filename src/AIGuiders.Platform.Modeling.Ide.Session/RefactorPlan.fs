namespace AIGuiders.Platform.Modeling.Ide.Session

/// <summary>Pure plan functions: θ → Δ = (Δ_fs, Δ_G). Preview = plan only; apply via <see cref="SessionPatch.apply" />.</summary>
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

    let planRename (contents: Map<string, string>) (spec: RenameSymbol) : SessionPatch =
        let replacements =
            spec.Files
            |> List.choose (fun path ->
                match Map.tryFind path contents with
                | None -> None
                | Some text when text.Contains spec.OldName ->
                    Some
                        { Path = path
                          Old = spec.OldName
                          New = spec.NewName }
                | Some _ -> None)

        { FileSystem =
            { Replacements = replacements
              PathRenames = []
              Writes = []
              Deletes = [] }
          Graph = GraphStructurePatch.empty }

    /// Move type to a new file: Δ_fs (source rewrite + new file) + ω update. G' ≠ G.
    let planMoveTypeToFile (spec: MoveTypeToFile) : SessionPatch =
        { FileSystem =
            { Replacements = []
              PathRenames = []
              Writes =
                [ spec.SourcePath, spec.UpdatedSourceContents
                  spec.TargetPath, spec.ExtractedContents ]
              Deletes = [] }
          Graph =
            { FileOwnershipUpdates = [ spec.TargetPath, spec.Owner ] } }

    /// Physical path rename within solution: ω follows via PathRenames in apply.
    let planMovePath (spec: MovePath) : SessionPatch =
        { FileSystem =
            { Replacements = []
              PathRenames = [ spec.From, spec.To ]
              Writes = []
              Deletes = [] }
          Graph = GraphStructurePatch.empty }
