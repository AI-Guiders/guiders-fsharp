namespace AIGuiders.Platform.Modeling.Cockpit.DataBus

type DebugBreakpointSnapshot =
    { File: string
      Line: int
      Condition: string option }

type DebugVariableRow =
    { Name: string
      Value: string
      Type: string option
      VariablesReference: int
      NamedVariables: int option
      IndexedVariables: int option }

type DebugVariableRootScope =
    { ScopeName: string
      Roots: DebugVariableRow list }

type DebugSessionSnapshot =
    { HasActiveSession: bool
      IsExecutionStopped: bool
      StoppedFile: string option
      StoppedLine: int
      ExceptionText: string option
      Breakpoints: DebugBreakpointSnapshot list
      StackFrames: (string * string option * int) list
      VariableRootScopes: DebugVariableRootScope list
      VariablesFrameIndex: int }

    static member Empty =
        { HasActiveSession = false
          IsExecutionStopped = false
          StoppedFile = None
          StoppedLine = 0
          ExceptionText = None
          Breakpoints = []
          StackFrames = []
          VariableRootScopes = []
          VariablesFrameIndex = 0 }

type DebugStateChanged = { Snapshot: DebugSessionSnapshot }
