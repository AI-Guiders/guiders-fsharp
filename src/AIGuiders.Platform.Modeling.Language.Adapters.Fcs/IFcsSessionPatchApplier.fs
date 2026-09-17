namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open AIGuiders.Platform.Modeling.Ide.Session

/// Applies federation <c>SessionPatch</c> with disk read/write — Execution binds @ startup (plan §7).
type IFcsSessionPatchApplier =
    abstract TryApply:
        anchorPath: string * patch: SessionPatch * sourceOverrides: Map<string, string> -> Result<unit, string>
