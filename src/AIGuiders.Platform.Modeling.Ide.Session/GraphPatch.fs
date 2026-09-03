namespace AIGuiders.Platform.Modeling.Ide.Session

type TextReplacement =
    { Path: string
      Old: string
      New: string }

type FileSystemPatch =
    { Replacements: TextReplacement list
      PathRenames: (string * string) list
      Writes: (string * string) list
      Deletes: string list }

module FileSystemPatch =
    let empty =
        { Replacements = []
          PathRenames = []
          Writes = []
          Deletes = [] }

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

    /// §5.2 scope for orchestrator invalidation after apply.
    let scope (patch: SessionPatch) : InvalidationScope =
        let fs = patch.FileSystem
        let g = patch.Graph

        let fileCrud =
            not (List.isEmpty fs.PathRenames)
            || not (List.isEmpty fs.Writes)
            || not (List.isEmpty fs.Deletes)
            || not (List.isEmpty g.FileOwnershipUpdates)

        if fileCrud then
            ProjectFileCrud
        elif not (List.isEmpty fs.Replacements) then
            FileChange
        else
            FileChange

    let apply (graph: SolutionGraph) (contents: Map<string, string>) (patch: SessionPatch) =
        let contentsAfterReplacements =
            (contents, patch.FileSystem.Replacements)
            ||> List.fold (fun acc repl ->
                match Map.tryFind repl.Path acc with
                | None -> acc
                | Some text -> Map.add repl.Path (text.Replace(repl.Old, repl.New)) acc)

        let contentsAfterWrites =
            (contentsAfterReplacements, patch.FileSystem.Writes)
            ||> List.fold (fun acc (path, text) -> Map.add path text acc)

        let contentsAfterRenames, ownershipAfterRenames =
            ((contentsAfterWrites, graph.FileOwnership), patch.FileSystem.PathRenames)
            ||> List.fold (fun (accContents, accOmega) (oldPath, newPath) ->
                match Map.tryFind oldPath accContents with
                | None -> accContents, accOmega
                | Some text ->
                    let owner =
                        match Map.tryFind oldPath accOmega with
                        | Some id -> id
                        | None -> failwith $"Path rename '{oldPath}' → '{newPath}' has no ω owner."

                    Map.remove oldPath accContents |> Map.add newPath text,
                    accOmega |> Map.remove oldPath |> Map.add newPath owner)

        let contents' =
            (contentsAfterRenames, patch.FileSystem.Deletes)
            ||> List.fold (fun acc path -> Map.remove path acc)

        let ownership' =
            (ownershipAfterRenames, patch.Graph.FileOwnershipUpdates)
            ||> List.fold (fun acc (path, owner) -> Map.add path owner acc)

        let graph' = { graph with FileOwnership = ownership' }
        graph', contents'
