namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.Collections.Concurrent
open System.IO
open FSharp.Compiler.CodeAnalysis

type FallbackFcsProjectOptionsSource(sources: IFcsProjectOptionsSource[]) =
    interface IFcsProjectOptionsSource with
        member _.TryLoad projectPath =
            let rec loop index lastError =
                if index >= sources.Length then
                    Error lastError
                else
                    match sources[index].TryLoad projectPath with
                    | Ok options -> Ok options
                    | Error err -> loop (index + 1) err

            loop 0 { Message = "No F# project options sources configured." }

        member _.Warm projectPath =
            for source in sources do
                source.Warm projectPath

        member _.Invalidate ?fsprojPath =
            for source in sources do
                source.Invalidate(?fsprojPath = fsprojPath)

type CachingFcsProjectOptionsSource(inner: IFcsProjectOptionsSource) =
    let cache =
        ConcurrentDictionary<string, Result<FSharpProjectOptions, FcsProjectOptionsLoadError>>(
            StringComparer.OrdinalIgnoreCase
        )

    let normalize path = Path.GetFullPath path

    interface IFcsProjectOptionsSource with
        member _.TryLoad fsprojPath =
            if String.IsNullOrWhiteSpace fsprojPath || not (File.Exists fsprojPath) then
                Error { Message = $"F# project file not found: '{fsprojPath}'." }
            else
                let key = normalize fsprojPath
                cache.GetOrAdd(key, fun _ -> inner.TryLoad key)

        member _.Warm fsprojPath =
            if not (String.IsNullOrWhiteSpace fsprojPath) && File.Exists fsprojPath then
                let key = normalize fsprojPath
                cache.GetOrAdd(key, fun _ -> inner.TryLoad key) |> ignore

        member _.Invalidate fsprojPath =
            match fsprojPath with
            | None -> cache.Clear()
            | Some path when not (String.IsNullOrWhiteSpace path) ->
                cache.TryRemove(normalize path) |> ignore
            | Some _ -> ()

module FcsProjectOptions =
    let createDefault () =
        let chain =
            FallbackFcsProjectOptionsSource(
                [| IonideInProcessFcsProjectOptionsSource() :> IFcsProjectOptionsSource
                   ProbeFcsProjectOptionsSource() :> IFcsProjectOptionsSource |]
            )

        CachingFcsProjectOptionsSource(chain) :> IFcsProjectOptionsSource

    let mutable Default = createDefault ()

    let tryGet (fsprojPath: string) =
        match Default.TryLoad fsprojPath with
        | Ok options -> Some options
        | Error _ -> None

    let warm (fsprojPath: string) = Default.Warm fsprojPath

    let invalidate () = Default.Invalidate()

    let invalidateProject (fsprojPath: string) = Default.Invalidate(fsprojPath)
