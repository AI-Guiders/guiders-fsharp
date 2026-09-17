namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

type WorkspaceProjectView =
    { ProjectId: ProjectId
      ProjectPath: string
      LanguageId: string
      CompileFiles: string list }

type WorkspaceView =
    { Revision: SessionRevision
      Anchor: LogicalPath
      Mode: FreezeMode
      RootProjectId: ProjectId
      Projects: WorkspaceProjectView list
      Documents: Map<DocId, DocumentText> }
