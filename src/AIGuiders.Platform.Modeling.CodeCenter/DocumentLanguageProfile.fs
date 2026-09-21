namespace AIGuiders.Platform.Modeling.CodeCenter

type SurfaceFamily =
    | BlockText = 0
    | MdBlockAst = 1
    | XmlTree = 2
    | HtmlTree = 3
    | YamlMapping = 4
    | TomlMapping = 5
    | SqlScript = 6
    | PlainLines = 7
    | CodeAst = 8

type ProfileRef =
    { ProfileId: string
      Flavour: string option }

type StructuralPlanOutcome =
    { Snapshot: DocumentSnapshot
      Inverse: StructuralEdit option
      InverseQuality: InverseQuality }

/// Planet-owned language semantics for document graph rebuild and structural edits.
type IDocumentLanguageProfile =
    abstract ProfileRef: ProfileRef
    abstract Surface: SurfaceFamily
    abstract Rebuild: string -> DocumentSnapshot
    abstract PlanStructural: DocumentSnapshot -> StructuralEdit -> Result<StructuralPlanOutcome, string>
    abstract AvailableProjections: unit -> ProjectionDescriptor list

module StructuralPlanGraph =
    let plan (before: DocumentSnapshot) (edit: StructuralEdit) : Result<StructuralPlanOutcome, string> =
        let map inverse inverseQuality f =
            match f before with
            | Error e -> Error e
            | Ok after ->
                Ok
                    { Snapshot = after
                      Inverse = inverse
                      InverseQuality = inverseQuality }

        match edit with
        | RenameMember(nodeId, newName) ->
            map
                (Some(RenameMember(nodeId, (Map.find nodeId before.Nodes).Name)))
                InverseQuality.Exact
                (fun s -> DocumentGraph.renameNode s nodeId newName)

        | InsertBlock(anchorId, sourceLine) ->
            map None InverseQuality.Unspecified (fun s -> DocumentGraph.insertBlock s anchorId sourceLine)

        | MoveMember(nodeId, targetParentId, index) ->
            map
                (Some(MoveMember(nodeId, targetParentId, index)))
                InverseQuality.Partial
                (fun s -> DocumentGraph.moveMember s nodeId targetParentId index)

        | Extract(nodeId, extractedName) ->
            map
                (Some(Extract(nodeId, extractedName)))
                InverseQuality.Partial
                (fun s -> DocumentGraph.extractMember s nodeId extractedName)

type NeutralDocumentLanguageProfile() =
    interface IDocumentLanguageProfile with
        member _.ProfileRef = { ProfileId = "neutral.plain"; Flavour = None }
        member _.Surface = SurfaceFamily.PlainLines
        member _.Rebuild text = DocumentGraph.emptySnapshot text
        member _.PlanStructural snapshot edit = StructuralPlanGraph.plan snapshot edit

        member _.AvailableProjections () = ProjectionDescriptor.defaultAvailable ()

type RebuildLanguageProfile(rebuild: DocumentGraphRebuild, profileId: string, surface: SurfaceFamily) =
    interface IDocumentLanguageProfile with
        member _.ProfileRef = { ProfileId = profileId; Flavour = None }
        member _.Surface = surface
        member _.Rebuild text = rebuild text
        member _.PlanStructural snapshot edit = StructuralPlanGraph.plan snapshot edit

        member _.AvailableProjections () = ProjectionDescriptor.defaultAvailable ()

module RebuildLanguageProfile =
    let create rebuild =
        RebuildLanguageProfile(rebuild, "rebuild.adhoc", SurfaceFamily.PlainLines) :> IDocumentLanguageProfile
