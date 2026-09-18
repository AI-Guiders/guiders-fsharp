namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Documentation.Correspondence
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

/// <summary>Materialize CRS doc→code witnesses into graph <see cref="Relation"/> edges (plan Phase 2).</summary>
module CorrespondenceMaterialize =
    let tryMaterializeDocToCodeWitness (witness: DocToCodeWitness) : Relation option =
        match DocToCodeWitnessBridge.tryToRelationSpec witness with
        | None -> None
        | Some(RelationSpec.DocToCode(DocumentPlace.Fragment(docPath, _), CodeTarget.Symbol(codeDoc, symbol))) ->
            let relation =
                { From = GraphNodeRef.Document(DocumentRef.File docPath)
                  Type = CorrespondenceRelationGraph.kindToRelationType witness.Kind
                  To = GraphNodeRef.Semantic(codeDoc, symbol)
                  Scope = SessionG
                  Attributes = RelationAttributes.empty }

            match RelationGraph.validateRelation relation with
            | Ok () -> Some relation
            | Error _ -> None
        | _ -> None

    let buildUses (doc: DocumentRef) (fromSymbol: SymbolRef) (toSymbol: SymbolRef) (project: ProjectId) =
        { From = GraphNodeRef.Semantic(doc, fromSymbol)
          Type = RelationType.Uses
          To = GraphNodeRef.Semantic(doc, toSymbol)
          Scope = SemanticSubstrate project
          Attributes = RelationAttributes.empty }

    let private buildSemanticFromTypeNames
        (logicalPath: string)
        (fromTypeName: string)
        (toTypeName: string)
        (project: ProjectId)
        (relationType: RelationType)
        =
        let doc = DocumentRef.File(LogicalPath.Create logicalPath)

        let fromSymbol =
            { Container = []; Name = fromTypeName; Arity = None }

        let toSymbol =
            { Container = []; Name = toTypeName; Arity = None }

        { From = GraphNodeRef.Semantic(doc, fromSymbol)
          Type = relationType
          To = GraphNodeRef.Semantic(doc, toSymbol)
          Scope = SemanticSubstrate project
          Attributes = RelationAttributes.empty }

    let buildUsesFromTypeNames (logicalPath: string) (fromTypeName: string) (toTypeName: string) (project: ProjectId) =
        buildSemanticFromTypeNames logicalPath fromTypeName toTypeName project RelationType.Uses

    let buildTypeUsesFromTypeNames (logicalPath: string) (fromTypeName: string) (toTypeName: string) (project: ProjectId) =
        buildSemanticFromTypeNames logicalPath fromTypeName toTypeName project RelationType.TypeUses

    let buildExtendsFromTypeNames (logicalPath: string) (fromTypeName: string) (toTypeName: string) (project: ProjectId) =
        buildSemanticFromTypeNames logicalPath fromTypeName toTypeName project RelationType.Extends

    let buildImplementsInterfaceFromTypeNames
        (logicalPath: string)
        (fromTypeName: string)
        (toTypeName: string)
        (project: ProjectId)
        =
        buildSemanticFromTypeNames logicalPath fromTypeName toTypeName project RelationType.ImplementsInterface
