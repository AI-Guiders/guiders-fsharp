namespace AIGuiders.Platform.Modeling.Ide.Session

type TextReplacement =
    { Path: string
      Old: string
      New: string }

type FileSystemPatch =
    { Replacements: TextReplacement list
      PathRenames: (string * string) list }

module FileSystemPatch =
    let empty = { Replacements = []; PathRenames = [] }

type GraphStructurePatch =
    { FileOwnershipUpdates: (string * ProjectId) list }

module GraphStructurePatch =
    let empty = { FileOwnershipUpdates = [] }

type SessionPatch =
    { FileSystem: FileSystemPatch
      Graph: GraphStructurePatch }

module SessionPatch =
    let empty =
        { FileSystem = FileSystemPatch.empty
          Graph = GraphStructurePatch.empty }

    let apply (graph: SolutionGraph) (contents: Map<string, string>) (patch: SessionPatch) =
        let contents' =
            (contents, patch.FileSystem.Replacements)
            ||> List.fold (fun acc repl ->
                match Map.tryFind repl.Path acc with
                | None -> acc
                | Some text ->
                    Map.add repl.Path (text.Replace(repl.Old, repl.New)) acc)

        let ownership' =
            (graph.FileOwnership, patch.Graph.FileOwnershipUpdates)
            ||> List.fold (fun acc (path, owner) -> Map.add path owner acc)

        let graph' = { graph with FileOwnership = ownership' }
        graph', contents'
