namespace AIGuiders.Platform.Modeling.Build

/// <summary>FTC request — freeze a revision tree (Freeze Tree Composition, Света 2026-09-07).
/// Nullability mirrors C# `string?`.</summary>
[<CLIMutable>]
type FreezeRequest =
    { EntryPath: string
      FocusPath: string | null }

/// <summary>FTC outcome — frozen tree the consumer owns: worktree at HEAD + WIP overlay.
/// The consumer (MSBuild/tests) is «царь и бог» in WorkRoot; bin/obj contention on the
/// shared tree disappears by construction. Release = discard by PlanId.
/// Parity with C# WorktreePlanRunner.FrozenTree (Execution binding).</summary>
[<CLIMutable>]
type FrozenTree =
    { Ok: bool
      Error: string | null
      PlanId: string
      GitRoot: string
      WorkRoot: string
      PlanScope: string
      BaseTreeSha: string
      OverlayPathCount: int }

[<RequireQualifiedAccess>]
module Freeze =

    /// Pure validation: entry path required, focus optional. null = valid.
    let validate (req: FreezeRequest) : string | null =
        if isNull req.EntryPath || req.EntryPath.Trim().Length = 0 then
            "entry_path required"
        else null

    /// Pulse line for the desk — where the frozen copy lives.
    let pulse (tree: FrozenTree) : string =
        if tree.Ok then
            $"frozen · {tree.PlanId} · overlay={tree.OverlayPathCount} · base={tree.BaseTreeSha[..7]}"
        else
            $"frozen failed · {tree.Error}"
