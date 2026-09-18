namespace AIGuiders.Platform.Modeling.CodeCenter

open System
open AIGuiders.Platform.Modeling.Core.Identity

type DocumentNode =
    { Id: NodeId
      Kind: string
      Name: string
      Start: int
      End: int
      Parent: NodeId option }

type SessionClassificationSpan =
    { Start: int
      Length: int
      Kind: string
      NodeId: NodeId option }

type DocumentLocation =
    { Start: int
      End: int
      Tier: string
      NodeId: NodeId option }

type SessionAnchor =
    { Offset: int
      TierHint: string option }

type FoldingRegion =
    { Start: int
      End: int
      Name: string }

type DocumentSnapshot =
    { Text: string
      Nodes: Map<NodeId, DocumentNode>
      TokenSpans: SessionClassificationSpan list
      FoldingRegions: FoldingRegion list }

type DocumentGraphRebuild = string -> DocumentSnapshot

module DocumentGraph =
    let private nextNodeId (nodes: Map<NodeId, DocumentNode>) =
        let maxId =
            nodes
            |> Map.toList
            |> List.fold (fun acc (_, n) -> max acc (NumericId.value (NodeId.carrier n.Id))) 0L

        NodeId.mint (NumericId.ofCounter (maxId + 1L))

    let hashSnapshot (snapshot: DocumentSnapshot) = snapshot.Text.GetHashCode()

    let findNodeAt (snapshot: DocumentSnapshot) (offset: int) =
        snapshot.Nodes
        |> Map.toList
        |> List.sortBy (fun (_, n) -> n.End - n.Start)
        |> List.tryFind (fun (_, n) -> n.Start <= offset && offset < n.End)
        |> Option.map snd

    let findNodeByName (snapshot: DocumentSnapshot) (name: string) =
        snapshot.Nodes
        |> Map.toList
        |> List.tryFind (fun (_, n) -> n.Name = name)
        |> Option.map snd

    /// Language-neutral snapshot shell — graph content comes from planet <c>DocumentGraphRebuild</c> only.
    let emptySnapshot (text: string) : DocumentSnapshot =
        { Text = text
          Nodes = Map.empty
          TokenSpans = []
          FoldingRegions = [] }

    let private shiftSpans (delta: int) (position: int) (nodes: Map<NodeId, DocumentNode>) =
        nodes
        |> Map.map (fun _ node ->
            if node.Start >= position then
                { node with
                    Start = node.Start + delta
                    End = node.End + delta }
            elif node.End > position then
                { node with End = node.End + delta }
            else
                node)

    let classificationSpans (snapshot: DocumentSnapshot) : SessionClassificationSpan list =
        snapshot.TokenSpans

    let renameNode (snapshot: DocumentSnapshot) (nodeId: NodeId) (newName: string) =
        match Map.tryFind nodeId snapshot.Nodes with
        | None -> Error $"node {nodeId} not found"
        | Some node ->
            let oldToken = node.Name
            let text = snapshot.Text

            let idx = text.IndexOf(oldToken, node.Start, node.End - node.Start, StringComparison.Ordinal)

            if idx < 0 then
                Error "rename token not found in span"
            else
                let delta = newName.Length - oldToken.Length

                let newText =
                    text.Substring(0, idx) + newName + text.Substring(idx + oldToken.Length)

                let nodes =
                    snapshot.Nodes
                    |> Map.add nodeId { node with Name = newName; End = node.End + delta }
                    |> shiftSpans delta (node.End)

                Ok { Text = newText; Nodes = nodes; TokenSpans = []; FoldingRegions = [] }

    let insertBlock (snapshot: DocumentSnapshot) (anchorId: NodeId) (blockKind: string) (body: string) =
        match Map.tryFind anchorId snapshot.Nodes with
        | None -> Error $"anchor {anchorId} not found"
        | Some anchor ->
            let insertion = Environment.NewLine + blockKind + " " + body + Environment.NewLine
            let insertAt = anchor.End
            let newText = snapshot.Text.Insert(insertAt, insertion)
            let delta = insertion.Length
            let newId = nextNodeId snapshot.Nodes

            let newNode =
                { Id = newId
                  Kind = blockKind
                  Name = body.Split(' ').[0]
                  Start = insertAt + Environment.NewLine.Length
                  End = insertAt + insertion.Length - Environment.NewLine.Length
                  Parent = Some anchorId }

            let nodes =
                snapshot.Nodes
                |> shiftSpans delta insertAt
                |> Map.add newId newNode

            Ok { Text = newText; Nodes = nodes; TokenSpans = []; FoldingRegions = [] }

    let moveMember (snapshot: DocumentSnapshot) (nodeId: NodeId) (_targetParentId: NodeId) (_index: int) =
        match Map.tryFind nodeId snapshot.Nodes with
        | None -> Error $"node {nodeId} not found"
        | Some node ->
            let line = Environment.NewLine + "    moved " + node.Name
            let newText = snapshot.Text.Insert(node.End, line)
            let delta = line.Length

            let nodes =
                snapshot.Nodes
                |> shiftSpans delta node.End
                |> Map.add nodeId { node with End = node.End + delta }

            Ok { Text = newText; Nodes = nodes; TokenSpans = []; FoldingRegions = [] }

    let extractMember (snapshot: DocumentSnapshot) (nodeId: NodeId) (extractedName: string) =
        match Map.tryFind nodeId snapshot.Nodes with
        | None -> Error $"node {nodeId} not found"
        | Some node ->
            let blockText = snapshot.Text.Substring(node.Start, node.End - node.Start)
            let wrapper =
                Environment.NewLine
                + "extracted "
                + extractedName
                + Environment.NewLine
                + blockText
                + Environment.NewLine
            let insertAt = snapshot.Text.Length
            let newText = snapshot.Text + wrapper
            let newId = nextNodeId snapshot.Nodes

            let newNode =
                { Id = newId
                  Kind = "extracted"
                  Name = extractedName
                  Start = insertAt + Environment.NewLine.Length
                  End = newText.Length
                  Parent = None }

            Ok
                { Text = newText
                  Nodes = snapshot.Nodes |> Map.add newId newNode
                  TokenSpans = []
                  FoldingRegions = [] }

    let tryResolve (snapshot: DocumentSnapshot) (anchor: SessionAnchor) =
        match findNodeAt snapshot anchor.Offset with
        | None -> None
        | Some node ->
            Some
                { Start = node.Start
                  End = node.End
                  Tier =
                    match anchor.TierHint with
                    | Some t -> t
                    | None -> "Syntax"
                  NodeId = Some node.Id }

    let nodeIdRoundTrip (snapshot: DocumentSnapshot) (location: DocumentLocation) =
        match location.NodeId with
        | None -> false
        | Some id ->
            match Map.tryFind id snapshot.Nodes with
            | None -> false
            | Some node -> node.Start = location.Start && node.End = location.End

    let formatNodeId (id: NodeId) =
        $"node:{NumericId.value (NodeId.carrier id)}"

    let tryFormatNodeIdWire (id: NodeId option) =
        id |> Option.map formatNodeId |> Option.toObj
