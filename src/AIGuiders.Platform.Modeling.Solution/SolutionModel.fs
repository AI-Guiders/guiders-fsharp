namespace AIGuiders.Platform.Modeling.Solution

/// <summary>Project entry in a solution. Path is solution-relative, forward-slash.</summary>
type SolutionProject =
    { Path: string
      Dependencies: string list }

/// <summary>Solution (virtual) folder containing project paths.</summary>
type SolutionFolder =
    { Name: string
      Projects: string list }

/// <summary>Solution model per the slnx spec (vs-solutionpersistence): projects + deps,
/// folders, build types/platforms, solution-level properties.</summary>
type SolutionModel =
    { Projects: SolutionProject list
      Folders: SolutionFolder list
      BuildTypes: string list
      Platforms: string list
      Properties: (string * string) list }

module SolutionModel =

    /// <summary>Empty solution (no projects, Debug/Release defaults).</summary>
    let empty : SolutionModel =
        { Projects = []
          Folders = []
          BuildTypes = [ "Debug"; "Release" ]
          Platforms = [ "Any CPU" ]
          Properties = [] }

    /// <summary>Dependencies of a project by its path.</summary>
    let dependenciesOf (model: SolutionModel) (projectPath: string) : string list =
        model.Projects
        |> List.tryFind (fun p -> p.Path = projectPath)
        |> Option.map (fun p -> p.Dependencies)
        |> Option.defaultValue []

    /// <summary>True when both projects exist and target depends on source (direct edge).</summary>
    let dependsOn (model: SolutionModel) (targetPath: string) (sourcePath: string) : bool =
        dependenciesOf model targetPath |> List.contains sourcePath