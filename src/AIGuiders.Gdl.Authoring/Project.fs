namespace AIGuiders.Gdl.Authoring

open System.IO

type AuthoringDocumentKind =
    | LogicalFile
    | FederationImport

type AuthoringDocumentRef =
    { Kind: AuthoringDocumentKind
      Path: string }

type ResolvedAuthoringDocument =
    { Ref: AuthoringDocumentRef
      Text: string option
      DisplayPath: string }

type AuthoringProject =
    { WorkspaceRoot: string
      Entry: string
      Documents: ResolvedAuthoringDocument list }

type AuthoringProjectLoadResult =
    { Project: AuthoringProject option
      Diagnostics: AuthoringDiagnostic list }

[<RequireQualifiedAccess>]
module PathBoundary =
    let tryToLogical (workspaceRoot: string) (physicalPath: string) =
        let root =
            Path.GetFullPath(workspaceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))

        let physical = Path.GetFullPath(physicalPath)

        if physical.StartsWith(root, System.StringComparison.OrdinalIgnoreCase) then
            Some(Path.GetRelativePath(root, physical).Replace('\\', '/'))
        else
            None

[<RequireQualifiedAccess>]
module AuthoringProjectLoader =
    let openSingleFile (workspaceRoot: string) (entryFilePath: string) =
        let diagnostics = ref []

        if not (File.Exists entryFilePath) then
            diagnostics :=
                AuthoringDiagnostic.create EntryFileNotFound $"Entry file not found: `{entryFilePath}`." 1
                :: diagnostics.Value

            { Project = None; Diagnostics = List.rev diagnostics.Value }
        else
            let root = Path.GetFullPath workspaceRoot

            match PathBoundary.tryToLogical root entryFilePath with
            | None ->
                diagnostics :=
                    AuthoringDiagnostic.create
                        EntryOutsideWorkspace
                        $"Entry `{entryFilePath}` is outside workspace root `{root}`."
                        1
                    :: diagnostics.Value

                { Project = None; Diagnostics = List.rev diagnostics.Value }
            | Some logical ->
                let text = File.ReadAllText(entryFilePath)

                let document =
                    { Ref =
                        { Kind = LogicalFile
                          Path = logical }
                      Text = Some text
                      DisplayPath = entryFilePath }

                let project =
                    { WorkspaceRoot = root
                      Entry = logical
                      Documents = [ document ] }

                { Project = Some project; Diagnostics = [] }
