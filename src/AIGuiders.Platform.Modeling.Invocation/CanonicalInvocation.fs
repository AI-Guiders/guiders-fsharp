namespace AIGuiders.Platform.Modeling.Invocation

open AIGuiders.Platform.Modeling.Notations.Argument
open AIGuiders.Platform.Modeling.Notations.Command

/// <summary>Semantic command invocation after catalog resolve (one command, many surfaces).</summary>
type CanonicalInvocation =
    { CommandId: string
      Path: NormalizedCommandLine
      Args: NormalizedArguments }

module CanonicalInvocation =

    let create commandId path args =
        { CommandId = commandId
          Path = path
          Args = args }
