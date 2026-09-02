namespace AIGuiders.Platform.Modeling.Paths

open System
open System.IO
open TruePath

/// <summary>Maps between OS absolute paths and LogicalPath at workspace/repo boundaries.</summary>
[<RequireQualifiedAccess>]
module PathBoundary =
    let private toLogicalFallback (workspaceRoot: string) (absolutePath: string) =
        try
            let root =
                Path.GetFullPath(workspaceRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)

            let abs = Path.GetFullPath(absolutePath)

            if not (abs.StartsWith(root, StringComparison.OrdinalIgnoreCase)) then
                Nullable()
            else
                Nullable(LogicalPath.Create(abs.Substring(root.Length).TrimStart('\\', '/')))
        with _ ->
            Nullable()

    let private toPhysicalFallback (workspaceRoot: string) (logical: string) =
        try
            let segments = logical.Split('/', StringSplitOptions.RemoveEmptyEntries)
            Path.GetFullPath(Path.Combine(Array.append [| workspaceRoot.Trim() |] segments))
        with _ ->
            null

    [<CompiledName("ToLogical")>]
    let toLogical (workspaceRoot: string) (absolutePath: string) =
        if String.IsNullOrWhiteSpace workspaceRoot || String.IsNullOrWhiteSpace absolutePath then
            Nullable()
        else
            try
                let root = AbsolutePath.Create(workspaceRoot.Trim())
                let abs = AbsolutePath.Create(absolutePath.Trim())

                if not (abs.StartsWith root) then
                    Nullable()
                else
                    let rel = abs.RelativeTo root
                    Nullable(LogicalPath.Create(rel.ToString().Replace('\\', '/')))
            with :? ArgumentException ->
                toLogicalFallback workspaceRoot absolutePath

    [<CompiledName("ToPhysical")>]
    let toPhysical (workspaceRoot: string) (logical: LogicalPath) =
        if String.IsNullOrWhiteSpace workspaceRoot || logical.IsEmpty then
            null
        else
            try
                let root = AbsolutePath.Create(workspaceRoot.Trim())
                let combined = root / logical.Value
                combined.ToString()
            with :? ArgumentException ->
                toPhysicalFallback workspaceRoot logical.Value

    [<CompiledName("TryCanonicalPhysical")>]
    let tryCanonicalPhysical (path: string) =
        if String.IsNullOrWhiteSpace path then
            null
        else
            try
                AbsolutePath.Create(Path.GetFullPath(path.Trim())).ToString()
            with :? ArgumentException ->
                try
                    Path.GetFullPath(path.Trim())
                with _ ->
                    null
