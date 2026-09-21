namespace AIGuiders.Platform.Modeling.CodeCenter

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Ide.Session

module StructuralPlan =
    let private textPatch (docId: DocId) (oldText: string) (newText: string) =
        if oldText = newText then
            SessionPatch.empty
        else
            { SessionPatch.empty with
                FileSystem =
                    { FileSystemPatch.empty with
                        Replacements = [ { DocId = docId; Old = oldText; New = newText } ] } }

    let planStructural
        (profile: IDocumentLanguageProfile)
        (docId: DocId)
        (before: DocumentSnapshot)
        (edit: StructuralEdit)
        : Result<SessionPatch * DocumentSnapshot * StructuralEdit option * InverseQuality, string>
        =
        match profile.PlanStructural before edit with
        | Error e -> Error e
        | Ok outcome ->
            let patch = textPatch docId before.Text outcome.Snapshot.Text
            Ok(patch, outcome.Snapshot, outcome.Inverse, outcome.InverseQuality)

    let replanStructural
        (profile: IDocumentLanguageProfile)
        (docId: DocId)
        (snapshot: DocumentSnapshot)
        (edit: StructuralEdit)
        : Result<SessionPatch * DocumentSnapshot, string>
        =
        planStructural profile docId snapshot edit
        |> Result.map (fun (patch, after, _, _) -> patch, after)

    let applyPatch (rebuild: DocumentGraphRebuild) (docId: DocId) (snapshot: DocumentSnapshot) (patch: SessionPatch) =
        let replacement =
            patch.FileSystem.Replacements
            |> List.tryFind (fun r -> r.DocId = docId)

        match replacement with
        | None -> snapshot
        | Some r -> rebuild r.New

    let applyMechanical (rebuild: DocumentGraphRebuild) (snapshot: DocumentSnapshot) (edit: MechanicalEdit) =
        match edit.Scope with
        | EditScope.Point
        | EditScope.Region when not snapshot.Nodes.IsEmpty ->
            DocumentGraph.applyMechanicalPatch snapshot edit
        | _ ->
            let text = snapshot.Text
            let start, length = edit.RemovedSpan
            let endExclusive = min text.Length (start + length)

            let newText =
                if start < 0 || start > text.Length then
                    text
                else
                    text.Substring(0, start) + edit.InsertedText + text.Substring(endExclusive)

            rebuild newText
