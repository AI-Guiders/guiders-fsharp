namespace AIGuiders.Platform.Modeling.Notations.Bracket

open System
open System.Collections.Generic
open System.Runtime.InteropServices

type BracketAxisShape =
    | KeyValue = 0
    | Opaque = 1

[<RequireQualifiedAccess>]
module BracketAxisValueClasses =
    let Opaque = "opaque"
    let CommandPath = "command.path"
    let Kv = "notation.kv"
    let LineRange = "line.range"
    let NestedBracket = "bracket.nested"

[<CLIMutable>]
type BracketNotationProfile =
    { Id: string
      StartTerminal: string
      EndTerminal: string
      ListSeparator: char
      KvSign: char
      AxisShape: BracketAxisShape
      StripOuterTerminals: bool
      RespectBracketDepthOnListSplit: bool
      NestedAxisKeys: IReadOnlyList<string> }

and [<CLIMutable>] NormalizedBracketWire =
    { ProfileId: string
      Axes: IReadOnlyList<BracketAxis>
      Raw: string }

and BracketAxis
    (
        key: string,
        sign: char,
        value: string,
        [<Optional; DefaultParameterValue(null)>] ?valueWireClass: string,
        [<Optional; DefaultParameterValue(null)>] ?nested: NormalizedBracketWire
    ) =
    member val Key = key with get, set
    member val Sign = sign with get, set
    member val Value = value with get, set
    member val ValueWireClass = defaultArg valueWireClass BracketAxisValueClasses.Opaque with get, set
    member val Nested = defaultArg nested (Unchecked.defaultof<NormalizedBracketWire>) with get, set

[<CLIMutable>]
type BracketAxisValuePlan =
    { ByAxisKey: IReadOnlyDictionary<string, string>
      DefaultValueKvSign: char }

[<CLIMutable>]
type BracketAxisAliasMap = { Aliases: IReadOnlyDictionary<string, string> }

[<RequireQualifiedAccess>]
module BracketProfiles =
    let CdpSquareKeyValue =
        { Id = "bracket.cdp-square-kv"
          StartTerminal = "["
          EndTerminal = "]"
          ListSeparator = ';'
          KvSign = ':'
          AxisShape = BracketAxisShape.KeyValue
          StripOuterTerminals = true
          RespectBracketDepthOnListSplit = true
          NestedAxisKeys = [| "Anchor" |] :> IReadOnlyList<_> }

    let SquareKeyValue = CdpSquareKeyValue

    let AngleOpaque =
        { Id = "bracket.angle-opaque"
          StartTerminal = "<"
          EndTerminal = ">"
          ListSeparator = ';'
          KvSign = ':'
          AxisShape = BracketAxisShape.Opaque
          StripOuterTerminals = true
          RespectBracketDepthOnListSplit = true
          NestedAxisKeys = null }

    let DocSymbol =
        { Id = "bracket.doc-symbol"
          StartTerminal = "["
          EndTerminal = "]"
          ListSeparator = ';'
          KvSign = ':'
          AxisShape = BracketAxisShape.KeyValue
          StripOuterTerminals = true
          RespectBracketDepthOnListSplit = true
          NestedAxisKeys = null }

[<RequireQualifiedAccess>]
module BracketAxisValuePlans =
    let private readOnlyDict (pairs: (string * string) list) =
        Dictionary<string, string>(dict pairs) :> IReadOnlyDictionary<string, string>

    let CdpCode =
        { ByAxisKey =
            readOnlyDict
                [ "F", BracketAxisValueClasses.CommandPath
                  "File", BracketAxisValueClasses.CommandPath
                  "M", BracketAxisValueClasses.Opaque
                  "Member", BracketAxisValueClasses.Opaque
                  "L", BracketAxisValueClasses.LineRange
                  "Line", BracketAxisValueClasses.LineRange
                  "S", BracketAxisValueClasses.Kv
                  "Scope", BracketAxisValueClasses.Kv
                  "K", BracketAxisValueClasses.Kv
                  "Kind", BracketAxisValueClasses.Kv
                  "T", BracketAxisValueClasses.Opaque
                  "Text", BracketAxisValueClasses.Opaque
                  "X", BracketAxisValueClasses.CommandPath
                  "Element", BracketAxisValueClasses.CommandPath
                  "A", BracketAxisValueClasses.Opaque
                  "Attribute", BracketAxisValueClasses.Opaque
                  "Anchor", BracketAxisValueClasses.NestedBracket
                  "Command", BracketAxisValueClasses.Opaque
                  "Go", BracketAxisValueClasses.Opaque
                  "Family", BracketAxisValueClasses.Opaque ]
          DefaultValueKvSign = ':' }

    let ForgeFrgCompound =
        { ByAxisKey = readOnlyDict [ "FRG", BracketAxisValueClasses.CommandPath ]
          DefaultValueKvSign = ':' }

    let DocSymbol =
        { ByAxisKey =
            readOnlyDict
                [ "Family", BracketAxisValueClasses.Opaque
                  "Package", BracketAxisValueClasses.Opaque
                  "Type", BracketAxisValueClasses.Opaque
                  "Member", BracketAxisValueClasses.Opaque
                  "CatalogField", BracketAxisValueClasses.Opaque
                  "Reader", BracketAxisValueClasses.Opaque ]
          DefaultValueKvSign = ':' }
