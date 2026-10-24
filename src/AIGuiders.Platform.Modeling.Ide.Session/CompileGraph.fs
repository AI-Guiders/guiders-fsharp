namespace AIGuiders.Platform.Modeling.Ide.Session

open System
open System.Text
open AIGuiders.Platform.Modeling.Core.Identity

/// <summary>
/// Compile graph Γ_π = (U_π, E_π, emit) — math §7.4.
/// U_π — compilation units; E_π — emit dependencies (artifact of u is an input of v);
/// emit is the driver port's job (BS3) — this module computes <c>U'</c>, it does not compile.
/// </summary>
type CompilationUnitId = string

/// <summary>Granularity policy of U_π (§7.4): semantic node (method, type member) or file/module.</summary>
type CompileGranularity =
    | FileOrModule
    | SemanticNode

/// <summary>E_π edge (§7.4): artifact of <c>From</c> is an input of <c>To</c>; <c>To</c> is a descendant of <c>From</c>.</summary>
type EmitDependency =
    { From: CompilationUnitId
      To: CompilationUnitId }

/// <summary>Unit topology of a project. <c>OrderedUnits</c> — F# compile order: each unit sees everything above it, so descendants(u) = all units after u (E_π is the chain, stored implicitly). <c>ExplicitEdges</c> — set-shaped projects (C#): E_π given explicitly.</summary>
type UnitTopology =
    | OrderedUnits of CompilationUnitId list
    | ExplicitEdges of EmitDependency list

/// <summary>Γ_π for one project π (§7.4).</summary>
type CompileGraph =
    { Project: ProjectId
      Topology: UnitTopology
      Granularity: CompileGranularity }

/// <summary>Δ for incremental build (§7.4): coarse file diff, or sniper semantic delta refine(δ) (SN1). FileStale fallback is a caller policy — express it as FilesChanged.</summary>
type CompileDelta =
    | FilesChanged of CompilationUnitId list
    | SemanticDelta of CompilationUnitId list

[<RequireQualifiedAccess>]
module CompileGraph =

    let units (g: CompileGraph) : CompilationUnitId list =
        match g.Topology with
        | OrderedUnits xs -> xs
        | ExplicitEdges _ -> []

    let private explicitDescendants (edges: EmitDependency list) (roots: CompilationUnitId list) =
        let byFrom = edges |> List.groupBy (fun e -> e.From) |> Map.ofList
        let rec visit seen queue =
            match queue with
            | [] -> seen
            | u :: rest when Set.contains u seen -> visit seen rest
            | u :: rest ->
                let next =
                    match Map.tryFind u byFrom with
                    | None -> []
                    | Some es -> es |> List.map (fun e -> e.To)
                visit (Set.add u seen) (rest @ next)
        visit Set.empty roots

    /// <summary>descendants_{E_π}(roots), including the roots themselves (§7.4). Ordered topology: F# chain — everything after a touched unit.</summary>
    let descendants (g: CompileGraph) (roots: CompilationUnitId list) : CompilationUnitId Set =
        match g.Topology with
        | OrderedUnits xs ->
            let touched = Set.ofList roots
            let takeAfter = xs |> List.skipWhile (fun u -> not (Set.contains u touched))
            Set.ofList takeAfter
        | ExplicitEdges edges -> explicitDescendants edges roots

    /// <summary>
    /// U' = Δ_U ∪ descendants_{E_π}(Δ_U) — the minimal rebuild set (IB1 for FilesChanged, SN1 for SemanticDelta).
    /// Pure calculation; no full rebuild unless the closure says so.
    /// </summary>
    let minimalRebuild (g: CompileGraph) (delta: CompileDelta) : CompilationUnitId Set =
        let roots =
            match delta with
            | FilesChanged us -> us
            | SemanticDelta us -> us
        if roots.IsEmpty then Set.empty else descendants g (List.ofSeq roots)

/// <summary>Artifact cache H: hash(inputs(u)) ⇀ artifacts, keyed to frozen inputs @ φ_r (§7.4). Revision of the freeze that produced the artifact.</summary>
type CachedArtifact =
    { Unit: CompilationUnitId
      Key: string
      Revision: SessionRevision
      /// <summary>Opaque artifact references (IL, pdb, …) — driver-owned (IC3).</summary>
      Artifacts: string list }

type ArtifactCache = Map<CompilationUnitId, CachedArtifact>

/// <summary>Emit plan for Build(π)@φ_r': recompile U', reuse the rest from H, or no emit at all when U' = ∅ (SN2, SN4).</summary>
type EmitPlan =
    | NoEmit
    | Emit of recompile: CompilationUnitId list * reuse: CompilationUnitId list

[<RequireQualifiedAccess>]
module ArtifactCache =

    /// <summary>hash(inputs(u)): frozen unit contents + the invocation inputs that shape its IL (project key). Deterministic sha256 — same inputs, same key (§7.4 H).</summary>
    let unitKey (projectId: ProjectId) (unit: CompilationUnitId) (unitContents: string) (invocationKey: string) : string =
        use sha = System.Security.Cryptography.SHA256.Create()
        let payload =
            String.Concat(
                ProjectId.value projectId, "\u0000",
                unit, "\u0000",
                invocationKey, "\u0000",
                unitContents
            )
        sha.ComputeHash(Encoding.UTF8.GetBytes payload)
        |> Array.map (fun b -> b.ToString("x2"))
        |> String.concat ""

    let empty : ArtifactCache = Map.empty

    let put (entry: CachedArtifact) (cache: ArtifactCache) : ArtifactCache = Map.add entry.Unit entry cache

    let tryGet (unit: CompilationUnitId) (cache: ArtifactCache) : CachedArtifact option = Map.tryFind unit cache

/// <summary>Emit planning over Γ_π + H (§7.4): what to recompile, what to reuse, whether to emit at all.</summary>
[<RequireQualifiedAccess>]
module EmitPlanner =

    /// <summary>
    /// Plan Build(π)@φ_r' against a cache filled at φ_r (IB1, SN2, SN4):
    /// recompile = U'; reuse = units with a valid cache entry outside U'; U' = ∅ ⇒ NoEmit.
    /// Stale cache entries inside U' are the recompile set itself — the driver replaces them on success.
    /// </summary>
    let plan (graph: CompileGraph) (delta: CompileDelta) (cache: ArtifactCache) : EmitPlan =
        let u' = CompileGraph.minimalRebuild graph delta
        if u'.IsEmpty then NoEmit
        else
            let recompile = CompileGraph.units graph |> List.filter (fun u -> Set.contains u u')
            let reuse =
                CompileGraph.units graph
                |> List.filter (fun u -> not (Set.contains u u') && Map.containsKey u cache)
            Emit(recompile, reuse)
