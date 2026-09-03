namespace AIGuiders.Platform.Modeling.Ide.Session

/// <summary>Result of a semantic / typecheck gate (port-provided in production).</summary>
type TypecheckVerdict =
    | Passed
    | Failed of errors: string list
    | NotRun

type TypecheckRequirement =
    | TypesRequired
    | TypesOptional
    | TypesNotRequired

type ObsPredicate =
    | RenamePreserves of oldName: string * newName: string
    | TypeMoved of typeName: string * sourcePath: string * targetPath: string

type ObsRequirement =
    | ObsMustPreserve of ObsPredicate list
    | ObsIgnored

type HoarePrecondition =
    { MinPhase: LifecyclePhase
      RequireGraphValid: bool }

module HoarePrecondition =
    let refactorDefault =
        { MinPhase = DesignTime
          RequireGraphValid = true }

type HoarePostcondition =
    { Types: TypecheckRequirement
      Obs: ObsRequirement }

module HoarePostcondition =
    let refactorRename oldName newName =
        { Types = TypesRequired
          Obs = ObsMustPreserve [ RenamePreserves(oldName, newName) ] }

    let refactorMoveType typeName sourcePath targetPath =
        { Types = TypesRequired
          Obs = ObsMustPreserve [ TypeMoved(typeName, sourcePath, targetPath) ] }

    let styleProven =
        { Types = TypesRequired
          Obs = ObsIgnored }

type SatResult =
    | Satisfied
    | Violated of violations: string list

module ObsChecker =
    let private checkRename (oldName: string) (newName: string) (contents: Map<string, string>) =
        let violations = ResizeArray()

        for kv in contents do
            let path, text = kv.Key, kv.Value

            if text.Contains oldName then
                violations.Add($"RenamePreserves: '{oldName}' still present in '{path}'.")

            if not (text.Contains newName) && text.Contains oldName then
                ()

        if violations.Count = 0 then
            let anyNew =
                contents |> Map.exists (fun _ text -> text.Contains newName)

            if not anyNew then
                violations.Add($"RenamePreserves: '{newName}' not found in any file.")

        if violations.Count = 0 then
            Satisfied
        else
            Violated(violations |> Seq.toList)

    let private typeDeclarationMarker (typeName: string) = $"type {typeName}"

    let private checkTypeMoved (typeName: string) (sourcePath: string) (targetPath: string) (contents: Map<string, string>) =
        let marker = typeDeclarationMarker typeName
        let violations = ResizeArray()

        match Map.tryFind sourcePath contents with
        | Some text when text.Contains marker ->
            violations.Add($"TypeMoved: '{marker}' still present in source '{sourcePath}'.")
        | _ -> ()

        match Map.tryFind targetPath contents with
        | None -> violations.Add($"TypeMoved: target '{targetPath}' missing from contents.")
        | Some text when not (text.Contains marker) ->
            violations.Add($"TypeMoved: '{marker}' not found in target '{targetPath}'.")
        | _ -> ()

        if violations.Count = 0 then Satisfied else Violated(violations |> Seq.toList)

    let check (requirement: ObsRequirement) (contents: Map<string, string>) =
        match requirement with
        | ObsIgnored -> Satisfied
        | ObsMustPreserve predicates ->
            let violations =
                predicates
                |> List.collect (function
                    | RenamePreserves(old, newName) ->
                        match checkRename old newName contents with
                        | Satisfied -> []
                        | Violated vs -> vs
                    | TypeMoved(typeName, sourcePath, targetPath) ->
                        match checkTypeMoved typeName sourcePath targetPath contents with
                        | Satisfied -> []
                        | Violated vs -> vs)

            if List.isEmpty violations then Satisfied else Violated violations

module HoareChecker =
    let private satTypes (requirement: TypecheckRequirement) (verdict: TypecheckVerdict) =
        match requirement, verdict with
        | TypesNotRequired, _ -> Satisfied
        | TypesOptional, _ -> Satisfied
        | TypesRequired, Passed -> Satisfied
        | TypesRequired, NotRun -> Violated [ "Q_types: typecheck required but not run (ST5)." ]
        | TypesRequired, Failed errs -> Violated([ "Q_types: typecheck failed." ] @ errs)

    let satGraph (graph: SolutionGraph) =
        let result = GraphValidation.validate graph

        if result.IsValid then
            Satisfied
        else
            Violated(result.Issues |> List.map (fun i -> $"Q_wf: {i.Message}"))

    let satPre (pre: HoarePrecondition) (sessionPhase: LifecyclePhase) (graph: SolutionGraph) =
        let violations = ResizeArray()

        if not (LifecyclePhase.canAdvanceTo sessionPhase pre.MinPhase) then
            violations.Add(
                $"P: session phase {sessionPhase} below required minimum {pre.MinPhase}."
            )

        if pre.RequireGraphValid then
            match satGraph graph with
            | Satisfied -> ()
            | Violated vs -> vs |> List.iter violations.Add

        if violations.Count = 0 then Satisfied else Violated(violations |> Seq.toList)

    let sat
        (post: HoarePostcondition)
        (graph: SolutionGraph)
        (contents: Map<string, string>)
        (typecheck: TypecheckVerdict)
        =
        let parts =
            [ satGraph graph
              satTypes post.Types typecheck
              ObsChecker.check post.Obs contents ]

        let violations =
            parts
            |> List.choose (function
                | Satisfied -> None
                | Violated vs -> Some vs)
            |> List.concat

        if List.isEmpty violations then Satisfied else Violated violations

    /// RF6: Sat(P,G,Q_wf) ∧ apply ⇒ Sat(P,G',Q).
    let refactorPreserves
        (pre: HoarePrecondition)
        (sessionPhase: LifecyclePhase)
        (graph: SolutionGraph)
        (contents: Map<string, string>)
        (post: HoarePostcondition)
        (patch: SessionPatch)
        (typecheckAfter: TypecheckVerdict)
        =
        match satPre pre sessionPhase graph with
        | Violated _ as v -> v
        | Satisfied ->
            match satGraph graph with
            | Violated _ as v -> v
            | Satisfied ->
                let graph', contents' = SessionPatch.apply graph contents patch

                let parts =
                    [ satGraph graph'
                      satTypes post.Types typecheckAfter
                      ObsChecker.check post.Obs contents' ]

                let violations =
                    parts
                    |> List.choose (function
                        | Satisfied -> None
                        | Violated vs -> Some vs)
                    |> List.concat

                if List.isEmpty violations then Satisfied else Violated violations
