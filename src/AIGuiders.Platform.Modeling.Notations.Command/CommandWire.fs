namespace AIGuiders.Platform.Modeling.Notations.Command

open System
open System.Collections.Generic

/// <summary>Pre-catalog slash/console wire: tokenized path + tail before longest-prefix resolve.</summary>
[<CLIMutable>]
type SlashWireBody =
    { Tokens: IReadOnlyList<string>
      EndsWithSpaceAfterTokens: bool }

    member this.JoinedTokens = String.Join(' ', this.Tokens)
