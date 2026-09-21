namespace AIGuiders.Platform.Modeling.CodeCenter

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

type DocumentSessionState =
    { G0: DocumentSnapshot
      Current: DocumentSnapshot
      Revision: int
      LambdaCommitted: LedgerEntryDoc list
      EphemeralMechanical: MechanicalEdit list
      RefreshScopes: RefreshScope list
      PartialParse: bool
      Profile: IDocumentLanguageProfile
      Completions: DocumentCompletions
      StructuralCompletions: DocumentStructuralCompletions }

type DocumentSession private (documentId: string, docId: DocId, gitPin: GitPin, state: DocumentSessionState) =

    member _.DocumentId = documentId
    member _.DocId = docId
    member _.Revision = state.Revision
    member _.Text = state.Current.Text
    member _.CommittedCount = state.LambdaCommitted.Length
    member _.EphemeralMechanicalCount = state.EphemeralMechanical.Length
    member _.RefreshScopes = state.RefreshScopes
    member _.PartialParse = state.PartialParse
    member _.LambdaCommitted = state.LambdaCommitted

    member private _.State = state

    member private _.WithState newState = DocumentSession(documentId, docId, gitPin, newState)

    member _.LanguageProfile = state.Profile

    member private _.Refresh snapshot = state.Profile.Rebuild snapshot.Text

    member _.GetClassificationSpans() =
        DocumentGraph.classificationSpans state.Current :> System.Collections.Generic.IReadOnlyList<_>

    member _.GetFoldingRegions() =
        state.Current.FoldingRegions :> System.Collections.Generic.IReadOnlyList<_>

    member _.GetDocumentNodes() =
        DocumentGraph.listNodes state.Current :> System.Collections.Generic.IReadOnlyList<_>

    member _.TryResolveNode(nodeId: NodeId) =
        DocumentGraph.tryResolveNode state.Current nodeId

    member _.TryResolve(anchor: SessionAnchor) = DocumentGraph.tryResolve state.Current anchor

    member _.GetCompletions(anchor: SessionAnchor) = state.Completions state.Current anchor

    member _.GetStructuralCompletions(anchor: SessionAnchor) =
        state.StructuralCompletions state.Current anchor

    member _.ProjectText() = state.Current.Text

    /// Session-advertised projections; host intersects with installed plugin catalog.
    member _.AvailableProjections() =
        state.Profile.AvailableProjections ()
        :> System.Collections.Generic.IReadOnlyList<_>

    member _.ApplyMechanicalEdit(edit: MechanicalEdit) : Result<DocumentSession, string> =
        let after = StructuralPlan.applyMechanical state.Profile.Rebuild state.Current edit
        let scope = RefreshScope.ofEditScope edit.Scope

        let newState =
            { state with
                Current = after
                Revision = state.Revision + 1
                EphemeralMechanical = state.EphemeralMechanical @ [ edit ]
                RefreshScopes = state.RefreshScopes @ [ scope ] }

        Ok(DocumentSession(documentId, docId, gitPin, newState))

    member _.CommitMechanicalBatch() : Result<DocumentSession * LedgerEntryDoc, string> =
        if List.isEmpty state.EphemeralMechanical then
            Error "no ephemeral mechanical edits to commit"
        else
            let before = state.G0

            let after =
                state.EphemeralMechanical
                |> List.fold (fun snap edit -> StructuralPlan.applyMechanical state.Profile.Rebuild snap edit) before

            let headEdit = List.head state.EphemeralMechanical
            let row = RePlannableThetaRegistry.require (MechanicalEdit.kind headEdit)

            let combinedPatch =
                if before.Text = after.Text then
                    SessionPatch.empty
                else
                    { SessionPatch.empty with
                        FileSystem =
                            { FileSystemPatch.empty with
                                Replacements = [ { DocId = docId; Old = before.Text; New = after.Text } ] } }

            let entry =
                DocumentSession.createLedgerEntry
                    (state.Revision + 1)
                    docId
                    gitPin
                    (DocumentTheta.Mechanical headEdit)
                    row
                    (Some combinedPatch)
                    before
                    None
                    InverseQuality.Unspecified

            let newState =
                { state with
                    Current = after
                    Revision = state.Revision + 1
                    LambdaCommitted = state.LambdaCommitted @ [ entry ]
                    EphemeralMechanical = [] }

            Ok(DocumentSession(documentId, docId, gitPin, newState), entry)

    member _.ApplyStructural(edit: StructuralEdit) =
        let row = RePlannableThetaRegistry.require (StructuralEdit.kind edit)

        match StructuralPlan.planStructural state.Profile docId state.Current edit with
        | Error e -> Error e
        | Ok(patch, after, inverse, inverseQuality) ->
            let after = DocumentSession(documentId, docId, gitPin, state).Refresh after

            let entry =
                DocumentSession.createLedgerEntry
                    (state.Revision + 1)
                    docId
                    gitPin
                    (DocumentTheta.Structural edit)
                    row
                    (Some patch)
                    state.Current
                    inverse
                    inverseQuality

            let newState =
                { state with
                    Current = after
                    Revision = state.Revision + 1
                    LambdaCommitted = state.LambdaCommitted @ [ entry ]
                    RefreshScopes = state.RefreshScopes @ [ RefreshScope.Document ] }

            Ok(DocumentSession(documentId, docId, gitPin, newState), entry)

    member _.ApplyFromRelationSpec(spec: RelationSpec) =
        match spec with
        | RelationSpec.CodeEdit(CodeTarget.TreeNode(_, nodeId)) ->
            DocumentSession(documentId, docId, gitPin, state).ApplyStructural(RenameMember(nodeId, "renamed"))
        | _ -> Error "RelationSpec stub supports CodeEdit TreeNode only (D34)"

    member _.ReplayToRevision(target: int) =
        if target < 0 then
            Error "target revision must be >= 0"
        elif target > state.LambdaCommitted.Length then
            Error $"target revision {target} exceeds committed count {state.LambdaCommitted.Length}"
        else
            let replayed =
                DocumentSession.replayCommitted docId state.Profile state.G0 state.LambdaCommitted target

            let newState =
                { state with
                    Current = replayed
                    Revision = target
                    LambdaCommitted = state.LambdaCommitted |> List.truncate target
                    EphemeralMechanical = [] }

            Ok(DocumentSession(documentId, docId, gitPin, newState))

    member _.TryUndo() =
        if state.LambdaCommitted.IsEmpty then
            Error "nothing to undo"
        else
            DocumentSession(documentId, docId, gitPin, state).ReplayToRevision(state.LambdaCommitted.Length - 1)

    member _.SyncFromText(newText: string) =
        let rebuilt = state.Profile.Rebuild newText
        let hadGraph = not state.Current.Nodes.IsEmpty

        let graphDegraded =
            hadGraph
            && (rebuilt.Nodes.IsEmpty || rebuilt.Nodes.Count < state.Current.Nodes.Count)

        let partial =
            (rebuilt.Nodes.IsEmpty && not (System.String.IsNullOrWhiteSpace newText)) || graphDegraded

        let current =
            if partial && hadGraph then
                { rebuilt with
                    Nodes = state.Current.Nodes
                    TokenSpans = state.Current.TokenSpans
                    FoldingRegions = state.Current.FoldingRegions }
            else
                rebuilt

        let newState =
            { state with
                Current = current
                Revision = state.Revision + 1
                PartialParse = partial
                RefreshScopes = state.RefreshScopes @ [ RefreshScope.Document ] }

        DocumentSession(documentId, docId, gitPin, newState)

    static member Create(documentId: string, initialText: string, profile: IDocumentLanguageProfile, ?gitPin: GitPin) =
        let g0 = profile.Rebuild initialText
        let pin = defaultArg gitPin { GitPin.Commit = None }

        DocumentSession(
            documentId,
            DocId.mint (NumericId.ofCounter 1L),
            pin,
            { G0 = g0
              Current = g0
              Revision = 0
              LambdaCommitted = []
              EphemeralMechanical = []
              RefreshScopes = []
              PartialParse = false
              Profile = profile
              Completions = Completion.empty
              StructuralCompletions = Completion.emptyStructural }
        )

    static member CreateWithProviders
        (
            documentId: string,
            initialText: string,
            profile: IDocumentLanguageProfile,
            completions: DocumentCompletions,
            structuralCompletions: DocumentStructuralCompletions,
            ?gitPin: GitPin
        ) =
        let g0 = profile.Rebuild initialText
        let pin = defaultArg gitPin { GitPin.Commit = None }

        DocumentSession(
            documentId,
            DocId.mint (NumericId.ofCounter 1L),
            pin,
            { G0 = g0
              Current = g0
              Revision = 0
              LambdaCommitted = []
              EphemeralMechanical = []
              RefreshScopes = []
              PartialParse = false
              Profile = profile
              Completions = completions
              StructuralCompletions = structuralCompletions }
        )

    static member private makePhiRef revision snapshot =
        { Revision = revision
          ContentHash = DocumentGraph.hashSnapshot snapshot }

    static member private createLedgerEntry
        revision
        _docId
        gitPin
        theta
        (row: RePlannableThetaRow)
        delta
        beforeSnapshot
        inverse
        inverseQuality
        =
        let phiRef =
            if row.StoresPhi then
                Some(DocumentSession.makePhiRef (revision - 1) beforeSnapshot)
            else
                None

        { Revision = revision
          Scope = InvalidationScope.FileChange
          Theta = theta
          ThetaClass = row.ThetaClass
          Delta = delta
          PhiRef = phiRef
          Anchor = None
          GitPin = gitPin
          Inverse = inverse
          InverseQuality = inverseQuality }

    static member private replayEntry
        (docId: DocId)
        (profile: IDocumentLanguageProfile)
        (snapshot: DocumentSnapshot)
        (entry: LedgerEntryDoc)
        =
        let row =
            match entry.Theta with
            | Structural edit -> RePlannableThetaRegistry.require (StructuralEdit.kind edit)
            | Mechanical edit -> RePlannableThetaRegistry.require (MechanicalEdit.kind edit)

        match row.ReplayMode, entry.Delta with
        | DeltaRequired, None -> Error $"entry {entry.Revision} missing delta"
        | RePlannable, None ->
            match entry.Theta with
            | Structural edit ->
                StructuralPlan.replanStructural profile docId snapshot edit
                |> Result.map (fun (_, after) -> profile.Rebuild after.Text)
            | Mechanical _ -> Error $"entry {entry.Revision} mechanical requires delta"
        | _, Some patch -> Ok(StructuralPlan.applyPatch profile.Rebuild docId snapshot patch)
        | DeltaOrReplan, None ->
            match entry.Theta with
            | Structural edit ->
                StructuralPlan.replanStructural profile docId snapshot edit
                |> Result.map (fun (_, after) -> profile.Rebuild after.Text)
            | Mechanical _ -> Error $"entry {entry.Revision} missing delta or theta"

    static member private replayCommitted
        (docId: DocId)
        (profile: IDocumentLanguageProfile)
        (g0: DocumentSnapshot)
        (entries: LedgerEntryDoc list)
        target
        =
        let rec loop snapshot remaining =
            match remaining with
            | [] -> snapshot
            | entry :: rest ->
                match DocumentSession.replayEntry docId profile snapshot entry with
                | Ok next -> loop next rest
                | Error _ -> snapshot

        let take = entries |> List.truncate target
        loop g0 take

module DocumentSession =
    let create documentId initialText rebuild =
        DocumentSession.Create(documentId, initialText, RebuildLanguageProfile.create rebuild)

    let createNeutral documentId initialText =
        DocumentSession.Create(documentId, initialText, NeutralDocumentLanguageProfile())

    let createWithProfile documentId initialText profile =
        DocumentSession.Create(documentId, initialText, profile)

    let createWithRebuild documentId initialText rebuild =
        DocumentSession.Create(documentId, initialText, RebuildLanguageProfile.create rebuild)

    let createWithProviders documentId initialText profile completions structuralCompletions =
        DocumentSession.CreateWithProviders(documentId, initialText, profile, completions, structuralCompletions)
