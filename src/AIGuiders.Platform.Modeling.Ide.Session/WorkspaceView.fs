namespace AIGuiders.Platform.Modeling.Ide.Session

/// <summary>§2.8b workspace projection — analyzer-facing view @ revision <c>r</c>.</summary>
type WorkspaceProjectView =
    { ProjectId: ProjectId
      ProjectPath: string
      LanguageId: string
      /// <summary>MSBuild @(Compile) order frozen @ revision (from ProjInfo at materialize).</summary>
      CompileFiles: string list }

type WorkspaceView =
    { Revision: SessionRevision
      AnchorPath: string
      Mode: FreezeMode
      RootProjectId: ProjectId
      Projects: WorkspaceProjectView list
      Contents: Map<string, string> }
