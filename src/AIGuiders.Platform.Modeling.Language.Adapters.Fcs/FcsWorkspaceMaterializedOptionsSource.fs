namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.IO

/// <summary>Reads FSharpProjectOptions materialized @ revision from <see cref="FcsCompilerServicesHost"/>.</summary>
type FcsWorkspaceMaterializedOptionsSource() =
    interface IFcsProjectOptionsSource with
        member _.TryLoad projectPath =
            match FcsCompilerServicesHost.tryGetOptions projectPath with
            | Some options -> Ok options
            | None ->
                Error
                    { Message =
                        $"F# compiler services are not materialized for '{Path.GetFullPath projectPath}'. "
                        + "Ensure federation CompilerServices (FTC → WorkspaceView → materialize) before LRC dispatch." }

        member _.Warm _ = ()

        member _.Invalidate anchorPath = FcsCompilerServicesHost.invalidate anchorPath
