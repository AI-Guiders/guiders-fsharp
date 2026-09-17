namespace AIGuiders.Platform.Modeling.Notations.Bracket

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

/// <summary>Kind: canon — bracket axes → RelationSpec witness (plan §3.2).</summary>
module BracketRelationWire =
    let private axisValue (wire: NormalizedBracketWire) (key: string) =
        wire.Axes
        |> Seq.tryFind (fun axis -> System.String.Equals(axis.Key, key, System.StringComparison.OrdinalIgnoreCase))
        |> Option.map (fun axis -> axis.Value.Trim())

    let private symbolFromAxes (file: LogicalPath) memberName scope =
        let container =
            match scope with
            | None -> []
            | Some s when System.String.IsNullOrWhiteSpace s -> []
            | Some s -> [ s ]

        CodeTarget.Symbol(
            DocumentRef.File file,
            { Container = container
              Name = memberName
              Arity = None }
        )

    let tryParseRelationSpec (wire: NormalizedBracketWire) : RelationSpec option =
        match axisValue wire "Kind" with
        | None -> None
        | Some kind ->
            match kind with
            | "CodeEdit" ->
                match axisValue wire "File", axisValue wire "Member" with
                | Some file, Some memberName ->
                    RelationSpec.CodeEdit(symbolFromAxes (LogicalPath.Create file) memberName (axisValue wire "Scope"))
                    |> Some
                | _ -> None
            | "Diag" ->
                match axisValue wire "DiagnosticId" with
                | Some id ->
                    match System.Int64.TryParse id with
                    | true, n -> RelationSpec.Diag(DiagnosticRef.mint(NumericId.ofCounter n)) |> Some
                    | _ -> None
                | None -> None
            | "Nav" ->
                match axisValue wire "File" with
                | Some path ->
                    let seed =
                        { Path = LogicalPath.Create path
                          Line = axisValue wire "Line" |> Option.bind (fun s -> System.Int32.TryParse(s) |> function true, v -> Some v | _ -> None)
                          Column = axisValue wire "Column" |> Option.bind (fun s -> System.Int32.TryParse(s) |> function true, v -> Some v | _ -> None)
                          Command = axisValue wire "Command"
                          Go = axisValue wire "Go"
                          Solution = axisValue wire "Solution" |> Option.map LogicalPath.Create }

                    RelationSpec.Nav seed |> Some
                | None -> None
            | _ -> None
