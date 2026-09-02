namespace AIGuiders.Platform.Modeling.Ide.Session

/// <summary>§2.11 style paths — vendor ≠ proven guarantees.</summary>
type StylePath =
    | Text
    | Vendor
    | Proven

type StyleApplyDecision =
    | AutoApplyAllowed
    | PreviewOnly of reason: string
    | Rejected of reason: string

module StyleConformance =
    /// ST3 + ST5: vendor/text without typecheck → reject auto-apply; proven requires Passed.
    let evaluateAutoApply (path: StylePath) (typecheck: TypecheckVerdict) : StyleApplyDecision =
        match path, typecheck with
        | Proven, Passed -> AutoApplyAllowed
        | Proven, NotRun -> Rejected "ST5: proven path requires typecheck before apply."
        | Proven, Failed _ -> Rejected "ST5: proven path rejected — typecheck failed."
        | Vendor, NotRun -> Rejected "ST5: vendor output without typecheck forbidden for auto-apply."
        | Vendor, Failed _ -> Rejected "ST5: vendor output typecheck failed."
        | Vendor, Passed -> PreviewOnly "ST3: vendor path is preview-only until proven gate."
        | Text, NotRun -> Rejected "ST5: text path requires typecheck before auto-apply."
        | Text, Failed _ -> Rejected "ST5: text path typecheck failed."
        | Text, Passed -> PreviewOnly "ST3: text path is preview-only by default."

type GoldenSession =
    { Name: string
      Graph: SolutionGraph
      Contents: Map<string, string>
      Phase: LifecyclePhase }

module GoldenSession =
    let create name graph contents phase =
        { Name = name
          Graph = graph
          Contents = contents
          Phase = phase }

module RefactorPlan =
    type RenameSymbol =
        { OldName: string
          NewName: string
          Files: string list }

    let planRename (contents: Map<string, string>) (spec: RenameSymbol) : SessionPatch =
        let replacements =
            spec.Files
            |> List.choose (fun path ->
                match Map.tryFind path contents with
                | None -> None
                | Some text when text.Contains spec.OldName ->
                    Some
                        { Path = path
                          Old = spec.OldName
                          New = spec.NewName }
                | Some _ -> None)

        { FileSystem = { Replacements = replacements; PathRenames = [] }
          Graph = GraphStructurePatch.empty }

module Conformance =
    let runRenameGolden
        (session: GoldenSession)
        (spec: RefactorPlan.RenameSymbol)
        (typecheckAfter: TypecheckVerdict)
        =
        let pre = HoarePrecondition.refactorDefault
        let post = HoarePostcondition.refactorRename spec.OldName spec.NewName
        let patch = RefactorPlan.planRename session.Contents spec

        HoareChecker.refactorPreserves
            pre
            session.Phase
            session.Graph
            session.Contents
            post
            patch
            typecheckAfter
