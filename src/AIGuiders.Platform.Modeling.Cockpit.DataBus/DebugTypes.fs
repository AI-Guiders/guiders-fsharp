namespace AIGuiders.Platform.Modeling.Cockpit.DataBus

open System

[<CLIMutable>]
type DebugBreakpointSnapshot =
    { File: string
      Line: int
      Condition: string }

[<CLIMutable>]
type DebugVariableRow =
    { Name: string
      Value: string
      Type: string
      VariablesReference: int
      NamedVariables: Nullable<int>
      IndexedVariables: Nullable<int> }

[<CLIMutable>]
type DebugVariableRootScope =
    { ScopeName: string
      Roots: DebugVariableRow[] }

[<CLIMutable>]
type DebugSessionSnapshot =
    { HasActiveSession: bool
      IsExecutionStopped: bool
      StoppedFile: string
      StoppedLine: int
      ExceptionText: string
      Breakpoints: DebugBreakpointSnapshot[]
      StackFrames: (string * string * int)[]
      VariableRootScopes: DebugVariableRootScope[]
      VariablesFrameIndex: int }

    static member Empty =
        { HasActiveSession = false
          IsExecutionStopped = false
          StoppedFile = ""
          StoppedLine = 0
          ExceptionText = ""
          Breakpoints = Array.empty
          StackFrames = Array.empty
          VariableRootScopes = Array.empty
          VariablesFrameIndex = 0 }

[<CLIMutable>]
type DebugStateChanged = { Snapshot: DebugSessionSnapshot }
