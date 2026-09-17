namespace AIGuiders.Platform.Modeling.Gdl.Authoring

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

/// Pure path boundary helpers (File IO @ Authoring.Core / Execution).
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
