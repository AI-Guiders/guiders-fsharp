namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open FSharp.Compiler.CodeAnalysis

[<CLIMutable>]
type FcsProjectOptionsWire =
    { ProjectFile: string
      OtherOptions: string[]
      SourceFiles: string[] }

module FcsProjectOptionsWire =
    let toFcsOptions (checker: FSharpChecker) (wire: FcsProjectOptionsWire) =
        let baseOptions =
            checker.GetProjectOptionsFromCommandLineArgs(wire.ProjectFile, wire.OtherOptions)

        { baseOptions with
            SourceFiles = wire.SourceFiles }
