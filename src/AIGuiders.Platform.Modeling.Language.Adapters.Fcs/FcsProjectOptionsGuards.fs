namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open FSharp.Compiler.CodeAnalysis

module FcsProjectOptionsGuards =
  let hasFrameworkReference (options: FSharpProjectOptions) =
    options.OtherOptions
    |> Array.exists (fun o ->
      o.StartsWith("-r:", StringComparison.Ordinal)
      && o.IndexOf("System.Runtime", StringComparison.OrdinalIgnoreCase) >= 0)

  let requireFrameworkReferences options =
    if hasFrameworkReference options then
      Ok options
    else
      Error
        { Message =
            "Project options are missing framework references (System.Runtime); retry with SdkAssets fallback." }
