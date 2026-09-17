namespace AIGuiders.Platform.Modeling.Ide.Session

/// <summary>TypeSystem profile schema — Modeling owns sorts/laws; Execution registers AdapterSlot emitters (plan §2.3).</summary>
type ProfileRelationId = ProfileRelationId of string

type ProfileRelationTypeDef =
    { Id: ProfileRelationId
      Dom: NodeSort
      Cod: NodeSort }

module TypeSystemProfile =
    let private def id dom cod = { Id = ProfileRelationId id; Dom = dom; Cod = cod }

    /// <summary>C# / Roslyn reference profile shipped with federation Phase 1.</summary>
    let csharpRoslynProfile : ProfileRelationTypeDef list =
        [ def "implements-interface" NodeSort.SemanticSymbol NodeSort.SemanticSymbol
          def "extends" NodeSort.SemanticSymbol NodeSort.SemanticSymbol
          def "instantiates" NodeSort.SemanticSymbol NodeSort.SemanticSymbol ]

    let tryFind (id: string) =
        csharpRoslynProfile
        |> List.tryFind (fun row ->
            match row.Id with
            | ProfileRelationId wire -> System.String.Equals(wire, id, System.StringComparison.OrdinalIgnoreCase))
