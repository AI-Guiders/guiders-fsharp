namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open FSharp.Compiler.CodeAnalysis

[<CLIMutable>]
type FcsProbeWire =
    { ProjectFile: string
      OtherOptions: string[]
      SourceFiles: string[] }

/// <summary>Pure probe wire → FCS options (File IO lives in Execution).</summary>
module FcsProbeWireMapping =
    let toFcsOptions (wire: FcsProbeWire) : Result<FSharpProjectOptions, FcsProjectOptionsLoadError> =
        let options =
            { ProjectId = None
              ProjectFileName = wire.ProjectFile
              SourceFiles = wire.SourceFiles
              OtherOptions = wire.OtherOptions
              ReferencedProjects = [||]
              IsIncompleteTypeCheckEnvironment = true
              UseScriptResolutionRules = false
              LoadTime = DateTime.UtcNow
              UnresolvedReferences = None
              OriginalLoadReferences = []
              Stamp = None }

        FcsProjectOptionsGuards.requireFrameworkReferences options
