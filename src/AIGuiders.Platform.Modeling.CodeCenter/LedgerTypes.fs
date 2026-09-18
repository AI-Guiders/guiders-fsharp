namespace AIGuiders.Platform.Modeling.CodeCenter

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

type ReplayMode =
    | DeltaRequired
    | RePlannable
    | DeltaOrReplan

type CommitPolicy =
    | OnApply
    | OnSaveBatch
    | EphemeralOnly

type RePlannableThetaRow =
    { ThetaKind: string
      ThetaClass: TransformClass
      ReplayMode: ReplayMode
      StoresPhi: bool
      CommitPolicy: CommitPolicy }

/// φᵣ pin — snapshot identity @ revision (hash, not full clone).
type FrozenSnapshotRef =
    { Revision: int
      ContentHash: int }

type DocumentTheta =
    | Structural of StructuralEdit
    | Mechanical of MechanicalEdit

type LedgerEntryDoc =
    { Revision: int
      Scope: InvalidationScope
      Theta: DocumentTheta
      ThetaClass: TransformClass
      Delta: SessionPatch option
      PhiRef: FrozenSnapshotRef option
      Anchor: RelationSpec option
      GitPin: GitPin
      Inverse: StructuralEdit option
      InverseQuality: InverseQuality }
