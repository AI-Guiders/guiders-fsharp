namespace AIGuiders.Platform.Modeling.CodeCenter

open AIGuiders.Platform.Modeling.Ide.Session

module RePlannableThetaRegistry =
    let private rows: RePlannableThetaRow list =
        [ { ThetaKind = "RenameMember"
            ThetaClass = TransformClass.Refactor "RenameMember"
            ReplayMode = RePlannable
            StoresPhi = true
            CommitPolicy = OnApply }
          { ThetaKind = "InsertBlock"
            ThetaClass = TransformClass.Refactor "InsertBlock"
            ReplayMode = DeltaRequired
            StoresPhi = true
            CommitPolicy = OnApply }
          { ThetaKind = "MoveMember"
            ThetaClass = TransformClass.Refactor "MoveMember"
            ReplayMode = DeltaOrReplan
            StoresPhi = true
            CommitPolicy = OnApply }
          { ThetaKind = "Extract"
            ThetaClass = TransformClass.Refactor "Extract"
            ReplayMode = DeltaOrReplan
            StoresPhi = true
            CommitPolicy = OnApply }
          { ThetaKind = "MechanicalPoint"
            ThetaClass = TransformClass.Other "MechanicalPoint"
            ReplayMode = DeltaRequired
            StoresPhi = false
            CommitPolicy = EphemeralOnly }
          { ThetaKind = "MechanicalRegion"
            ThetaClass = TransformClass.Other "MechanicalRegion"
            ReplayMode = DeltaRequired
            StoresPhi = false
            CommitPolicy = EphemeralOnly }
          { ThetaKind = "MechanicalDocument"
            ThetaClass = TransformClass.Other "MechanicalDocument"
            ReplayMode = DeltaRequired
            StoresPhi = false
            CommitPolicy = EphemeralOnly } ]

    let all = rows

    let lookup (thetaKind: string) =
        rows |> List.tryFind (fun r -> r.ThetaKind = thetaKind)

    let require (thetaKind: string) =
        match lookup thetaKind with
        | Some row -> row
        | None -> invalidArg "thetaKind" $"unknown theta kind '{thetaKind}'"

    let satisfiesEntry (entry: LedgerEntryDoc) =
        let kind =
            match entry.Theta with
            | Structural edit -> StructuralEdit.kind edit
            | Mechanical edit -> MechanicalEdit.kind edit

        match lookup kind with
        | None -> false
        | Some row ->
            row.ThetaClass = entry.ThetaClass
            && (not row.StoresPhi || entry.PhiRef.IsSome)
            && (row.ReplayMode <> DeltaRequired || entry.Delta.IsSome)
            && match row.CommitPolicy, entry.Scope with
               | OnApply, _ -> true
               | OnSaveBatch, _ -> true
               | EphemeralOnly, InvalidationScope.FileChange -> true
               | EphemeralOnly, _ -> false
