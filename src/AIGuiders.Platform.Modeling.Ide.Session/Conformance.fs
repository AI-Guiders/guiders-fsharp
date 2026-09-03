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

module Conformance =
    let private runRefactorGolden
        (session: GoldenSession)
        (patch: SessionPatch)
        (post: HoarePostcondition)
        (typecheckAfter: TypecheckVerdict)
        =
        let pre = HoarePrecondition.refactorDefault

        HoareChecker.refactorPreserves
            pre
            session.Phase
            session.Graph
            session.Contents
            post
            patch
            typecheckAfter

    let runRenameGolden
        (session: GoldenSession)
        (spec: RefactorPlan.RenameSymbol)
        (typecheckAfter: TypecheckVerdict)
        =
        let patch = RefactorPlan.planRename session.Contents spec
        let post = HoarePostcondition.refactorRename spec.OldName spec.NewName
        runRefactorGolden session patch post typecheckAfter

    let runMoveTypeGolden
        (session: GoldenSession)
        (spec: RefactorPlan.MoveTypeToFile)
        (typecheckAfter: TypecheckVerdict)
        =
        let patch = RefactorPlan.planMoveTypeToFile spec

        let post =
            HoarePostcondition.refactorMoveType spec.TypeName spec.SourcePath spec.TargetPath

        runRefactorGolden session patch post typecheckAfter
