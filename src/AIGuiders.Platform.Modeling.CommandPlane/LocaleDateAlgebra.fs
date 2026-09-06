namespace AIGuiders.Platform.Modeling.CommandPlane

/// <summary>Date field in locale input order (parity: LocaleDateField).</summary>
type LocaleDateField =
    | Day = 0
    | Month = 1
    | Year = 2

/// <summary>How complete a typed date is (parity: LocaleDateCompleteness).</summary>
type LocaleDateCompleteness =
    | Empty = 0
    | Partial = 1
    | MonthYear = 2
    | CompleteDate = 3
    | CompleteRange = 4

/// <summary>Parsed locale date parts (parity: LocaleDateParts).</summary>
[<CLIMutable>]
type LocaleDateParts =
    { Day: int
      Month: int
      Year: int
      RangeEnd: LocaleDateParts }

module LocaleInputProfile =

    type Profile =
        { PrimarySeparator: char
          Separators: char list
          FieldOrder: LocaleDateField list
          ShortDatePattern: string }

    /// Derive the input profile from a culture short-date pattern (pure).
    let fromPattern (shortDatePattern: string) : Profile =
        let fieldOrder = ResizeArray<LocaleDateField>()
        let chars = shortDatePattern.ToCharArray()
        let mutable i = 0
        while i < chars.Length do
            match chars[i] with
            | 'd' | 'D' ->
                if i + 1 < chars.Length && (chars[i + 1] = 'd' || chars[i + 1] = 'D') then i <- i + 1
                fieldOrder.Add(LocaleDateField.Day)
            | 'M' ->
                if i + 1 < chars.Length && chars[i + 1] = 'M' then i <- i + 1
                fieldOrder.Add(LocaleDateField.Month)
            | 'y' | 'Y' ->
                if i + 1 < chars.Length && (chars[i + 1] = 'y' || chars[i + 1] = 'Y') then i <- i + 1
                fieldOrder.Add(LocaleDateField.Year)
            | _ -> ()
            i <- i + 1

        let fields =
            if fieldOrder.Count = 0 then
                [ LocaleDateField.Day; LocaleDateField.Month; LocaleDateField.Year ]
            else
                List.ofSeq fieldOrder

        let separators =
            shortDatePattern
            |> Seq.filter (fun c -> not (System.Char.IsLetter c))
            |> Seq.append "/.-"
            |> Seq.distinct
            |> List.ofSeq

        let primary = if separators.IsEmpty then '/' else List.head separators
        { PrimarySeparator = primary
          Separators = separators
          FieldOrder = fields
          ShortDatePattern = shortDatePattern }

module LocaleDateAlgebra =

    /// Pure completeness check from token count + parsed values (no culture I/O).
    let completeness (tokenCount: int) (hasDay: bool) (hasMonth: bool) (hasYear: bool) : LocaleDateCompleteness =
        if hasDay && hasMonth && hasYear then LocaleDateCompleteness.CompleteDate
        elif hasMonth && hasYear && not hasDay then LocaleDateCompleteness.MonthYear
        elif tokenCount > 0 then LocaleDateCompleteness.Partial
        else LocaleDateCompleteness.Empty