module AIGuiders.Platform.Modeling.Solution.Tests.SolutionConformanceTests

open Xunit
open AIGuiders.Platform.Modeling.Solution

let private sampleXml =
    """<Solution>
  <Configurations>
    <BuildTypes>
      <BuildType Name="Debug" />
      <BuildType Name="Release" />
    </BuildTypes>
    <Platforms>
      <Platform Name="Any CPU" />
    </Platforms>
  </Configurations>
  <Project Path="src/Foo/Foo.csproj" />
  <Project Path="src/Bar/Bar.fsproj">
    <ProjectDependency Path="src/Foo/Foo.csproj" />
  </Project>
  <Folder Name="tests">
    <Project Path="tests/Bar.Tests/Bar.Tests.fsproj" />
  </Folder>
  <Properties Name="Solution Properties">
    <Property Name="HideSlnAndSolutionItems" Value="true" />
  </Properties>
</Solution>"""

[<Fact>]
let ``Parse: projects, dependencies, folders, configs, properties`` () =
    match SolutionNotation.parse sampleXml with
    | Ok model ->
        Assert.Equal(2, List.length model.Projects)
        Assert.Equal("src/Foo/Foo.csproj", model.Projects.[0].Path)
        Assert.Equal<string list>([ "src/Foo/Foo.csproj" ], model.Projects.[1].Dependencies)
        Assert.Equal(1, List.length model.Folders)
        Assert.Equal<string list>([ "tests/Bar.Tests/Bar.Tests.fsproj" ], model.Folders.[0].Projects)
        Assert.Equal<string list>([ "Debug"; "Release" ], model.BuildTypes)
        Assert.Equal<string list>([ "Any CPU" ], model.Platforms)
        Assert.Equal<(string * string) list>([ ("HideSlnAndSolutionItems", "true") ], model.Properties)
    | Error e -> Assert.Fail e

[<Fact>]
let ``Parse: malformed XML is an Error not an exception`` () =
    Assert.True (match SolutionNotation.parse "<Solution><Project" with Error _ -> true | Ok _ -> false)

[<Fact>]
let ``Author then parse: roundtrip keeps projects and dependencies`` () =
    let model =
        { SolutionModel.empty with
            Projects =
                [ { Path = "src/Foo/Foo.csproj"; Dependencies = [] }
                  { Path = "src/Bar/Bar.fsproj"; Dependencies = [ "src/Foo/Foo.csproj" ] } ] }

    let xml = SolutionNotation.author model
    match SolutionNotation.parse xml with
    | Ok round ->
        Assert.Equal<SolutionProject list>(model.Projects, round.Projects)
        Assert.Equal<string list>(model.BuildTypes, round.BuildTypes)
        Assert.Equal<string list>(model.Platforms, round.Platforms)
    | Error e -> Assert.Fail e

[<Fact>]
let ``Author: backslash paths normalized to forward slashes`` () =
    let model =
        { SolutionModel.empty with Projects = [ { Path = "src\\Foo\\Foo.csproj"; Dependencies = [] } ] }

    let xml = SolutionNotation.author model
    Assert.Contains("src/Foo/Foo.csproj", xml)
    match SolutionNotation.parse xml with
    | Ok round -> Assert.Equal("src/Foo/Foo.csproj", round.Projects.[0].Path)
    | Error e -> Assert.Fail e

[<Fact>]
let ``DependsOn: direct dependency edge`` () =
    let model =
        { SolutionModel.empty with
            Projects =
                [ { Path = "a.csproj"; Dependencies = [] }
                  { Path = "b.csproj"; Dependencies = [ "a.csproj" ] } ] }

    Assert.True (SolutionModel.dependsOn model "b.csproj" "a.csproj")
    Assert.False (SolutionModel.dependsOn model "a.csproj" "b.csproj")