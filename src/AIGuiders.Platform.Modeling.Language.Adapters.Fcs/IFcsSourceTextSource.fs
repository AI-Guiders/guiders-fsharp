namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

/// Disk/source text lookup — Execution binds @ startup (plan §7; Modeling never calls File.*).
type IFcsSourceTextSource =
    abstract TryRead: path: string -> string option
