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
        (docId: DocId)
        (before: DocumentSnapshot)
        (edit: StructuralEdit)
        : Result<SessionPatch * DocumentSnapshot * StructuralEdit option * InverseQuality, string>
        =
        let apply f inverse inverseQuality =
            match f before with
            | Error e -> Error e
            | Ok after ->
                let patch = textPatch docId before.Text after.Text
                Ok(patch, after, inverse, inverseQuality)

        match edit with
        | RenameMember(nodeId, newName) ->
            apply
                (fun s -> DocumentGraph.renameNode s nodeId newName)
                (Some(RenameMember(nodeId, (Map.find nodeId before.Nodes).Name)))
                InverseQuality.Exact

        | InsertBlock(anchorId, blockKind, body) ->
            apply
                (fun s -> DocumentGraph.insertBlock s anchorId blockKind body)
                None
                InverseQuality.Unspecified

        | MoveMember(nodeId, targetParentId, index) ->
            apply
                (fun s -> DocumentGraph.moveMember s nodeId targetParentId index)
                (Some(MoveMember(nodeId, targetParentId, index)))
                InverseQuality.Partial

        | Extract(nodeId, extractedName) ->
            apply
                (fun s -> DocumentGraph.extractMember s nodeId extractedName)
                (Some(Extract(nodeId, extractedName)))
                InverseQuality.Partial

    let replanStructural
        (docId: DocId)
        (snapshot: DocumentSnapshot)
        (edit: StructuralEdit)
        : Result<SessionPatch * DocumentSnapshot, string>
        =
        planStructural docId snapshot edit
        |> Result.map (fun (patch, after, _, _) -> patch, after)

    let applyPatch (docId: DocId) (snapshot: DocumentSnapshot) (patch: SessionPatch) =
        let replacement =
            patch.FileSystem.Replacements
            |> List.tryFind (fun r -> r.DocId = docId)

        match replacement with
        | None -> snapshot
        | Some r -> DocumentGraph.rebuildFromText r.New

    let applyMechanical (snapshot: DocumentSnapshot) (edit: MechanicalEdit) =
        let text = snapshot.Text
        let start, length = edit.RemovedSpan
        let endExclusive = min text.Length (start + length)

        let newText =
            if start < 0 || start > text.Length then
                text
            else
                text.Substring(0, start) + edit.InsertedText + text.Substring(endExclusive)

        DocumentGraph.rebuildFromText newText
