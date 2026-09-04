module AIGuiders.Platform.Modeling.Gdl.Command.Tests.ConformanceTests

open System
open System.Collections.Generic
open Xunit
open AIGuiders.Platform.Modeling.Gdl.Command

let private desc path aliases argTail =
    { Domain = "desk"
      Object = "files"
      Intent = "delete"
      CommandId = "cdp_files_delete"
      Path = path
      PathAliases = aliases
      Help = Some "help"
      Group = Some "grp"
      ArgTail = argTail
      ArgHint = None
      ArgPickerChoices = [||] :> IReadOnlyList<CommandPickerChoice>
      ArgConstructors = [||] :> IReadOnlyList<ArgConstructorBinding>
      Surfaces = [||] :> IReadOnlyList<string>
      Scope = [||] :> IReadOnlyList<string>
      RequiredCapabilities = [||] :> IReadOnlyList<string>
      Tier = None
      PluginId = None
      RequiresDestructiveConfirm = false }

[<Fact>]
let ``Parse: wire strings map to kinds`` () =
    Assert.Equal(CommandArgTailKind.None, CommandArgTailPolicy.parse "none")
    Assert.Equal(CommandArgTailKind.Required, CommandArgTailPolicy.parse "required")
    Assert.Equal(CommandArgTailKind.Optional, CommandArgTailPolicy.parse "optional")
    Assert.Equal(CommandArgTailKind.ImplicitSelection, CommandArgTailPolicy.parse "implicit:selection")
    Assert.Equal(CommandArgTailKind.ImplicitLineRange, CommandArgTailPolicy.parse "implicit:line_range")
    Assert.Equal(CommandArgTailKind.Picker, CommandArgTailPolicy.parse "picker:pickers/uri")
    Assert.Equal(CommandArgTailKind.Picker, CommandArgTailPolicy.parse "suggest:x")
    Assert.Equal(CommandArgTailKind.Picker, CommandArgTailPolicy.parse "picker+constructor:slot+a")

[<Fact>]
let ``Parse: empty null unknown and case-insensitive fall back correctly`` () =
    Assert.Equal(CommandArgTailKind.Optional, CommandArgTailPolicy.parse null)
    Assert.Equal(CommandArgTailKind.Optional, CommandArgTailPolicy.parse "")
    Assert.Equal(CommandArgTailKind.Optional, CommandArgTailPolicy.parse "  ")
    Assert.Equal(CommandArgTailKind.Optional, CommandArgTailPolicy.parse "wat")
    Assert.Equal(CommandArgTailKind.Required, CommandArgTailPolicy.parse " REQUIRED ")
    Assert.Equal(CommandArgTailKind.Picker, CommandArgTailPolicy.parse "PICKER:x")

[<Fact>]
let ``ShouldAutoRunOnCommit: matrix matches C# switch`` () =
    Assert.True (CommandArgTailPolicy.shouldAutoRunOnCommit CommandArgTailKind.None true false false)
    Assert.False (CommandArgTailPolicy.shouldAutoRunOnCommit CommandArgTailKind.None false false false)
    Assert.True (CommandArgTailPolicy.shouldAutoRunOnCommit CommandArgTailKind.Optional true false false)
    Assert.True (CommandArgTailPolicy.shouldAutoRunOnCommit CommandArgTailKind.Optional false true false)
    Assert.True (CommandArgTailPolicy.shouldAutoRunOnCommit CommandArgTailKind.Optional false false true)
    Assert.False (CommandArgTailPolicy.shouldAutoRunOnCommit CommandArgTailKind.Optional false false false)
    Assert.True (CommandArgTailPolicy.shouldAutoRunOnCommit CommandArgTailKind.Required false false true)
    Assert.False (CommandArgTailPolicy.shouldAutoRunOnCommit CommandArgTailKind.Required true false false)
    Assert.True (CommandArgTailPolicy.shouldAutoRunOnCommit CommandArgTailKind.Picker false true false)
    Assert.True (CommandArgTailPolicy.shouldAutoRunOnCommit CommandArgTailKind.ImplicitSelection true false false)
    Assert.True (CommandArgTailPolicy.shouldAutoRunOnCommit CommandArgTailKind.ImplicitLineRange false false true)

[<Fact>]
let ``InsertsTrailingSpaceOnCommit: only None`` () =
    Assert.True (CommandArgTailPolicy.insertsTrailingSpaceOnCommit CommandArgTailKind.None)
    Assert.False (CommandArgTailPolicy.insertsTrailingSpaceOnCommit CommandArgTailKind.Optional)
    Assert.False (CommandArgTailPolicy.insertsTrailingSpaceOnCommit CommandArgTailKind.Required)
    Assert.False (CommandArgTailPolicy.insertsTrailingSpaceOnCommit CommandArgTailKind.Picker)

[<Fact>]
let ``ExtractPickerId: prefixes strip and slot split`` () =
    Assert.Equal(Some "abc", CommandArgTailPolicy.extractPickerId "picker:abc")
    Assert.Equal(Some "abc", CommandArgTailPolicy.extractPickerId "PICKER:abc")
    Assert.Equal(Some "x", CommandArgTailPolicy.extractPickerId "suggest:x")
    Assert.Equal(Some "slot", CommandArgTailPolicy.extractPickerId "picker+constructor:slot+ctor")
    Assert.Equal(None, CommandArgTailPolicy.extractPickerId "plain")
    Assert.Equal(None, CommandArgTailPolicy.extractPickerId "picker:")
    Assert.Equal(None, CommandArgTailPolicy.extractPickerId null)

[<Fact>]
let ``Composite picker+constructor: gate and constructor id extraction`` () =
    Assert.True (CommandArgTailPolicy.isCompositePickerConstructor "picker+constructor:slot+a+b")
    Assert.False (CommandArgTailPolicy.isCompositePickerConstructor "picker:x")
    Assert.False (CommandArgTailPolicy.isCompositePickerConstructor null)
    let ids = CommandArgTailPolicy.extractCompositeConstructorIds "picker+constructor:slot+ctor1+ctor2"
    Assert.Equal<string list>([ "ctor1"; "ctor2" ], List.ofSeq ids)
    Assert.Empty (CommandArgTailPolicy.extractCompositeConstructorIds "picker+constructor:onlyslot")
    Assert.Empty (CommandArgTailPolicy.extractCompositeConstructorIds "picker:x")

[<Fact>]
let ``IsStaticEnumPicker: enum-prefixed picker ids only`` () =
    Assert.True (CommandArgTailPolicy.isStaticEnumPicker "picker:enumLanguage")
    Assert.False (CommandArgTailPolicy.isStaticEnumPicker "picker:uri")
    Assert.False (CommandArgTailPolicy.isStaticEnumPicker "none")

[<Fact>]
let ``AllPaths: canonical plus non-empty aliases`` () =
    let d = desc "/desk/files/delete" [| "/desk/files/rm"; "  "; "" |] "optional"
    Assert.Equal<string list>([ "/desk/files/delete"; "/desk/files/rm" ], List.ofSeq (CommandDescriptor.allPaths d))

[<Fact>]
let ``FromDescriptor: canonical path role and field mapping`` () =
    let d = desc "/desk/files/delete" [||] "required"
    let row = CatalogRouteEntry.fromDescriptor d "/desk/files/delete"
    Assert.Equal(CatalogPathRole.Canonical, row.PathRole)
    Assert.Equal("cdp_files_delete", row.CommandId)
    Assert.Equal("help", row.Help)
    Assert.Equal(CommandArgTailKind.Required, row.ArgTailKind)
    Assert.Equal(Some "grp", row.Group)

[<Fact>]
let ``FromDescriptor: alias path role and help fallback`` () =
    let d = { desc "/desk/files/delete" [||] "optional" with Help = None }
    let row = CatalogRouteEntry.fromDescriptor d "/DESK/files/rm"
    Assert.Equal(CatalogPathRole.Alias, row.PathRole)
    Assert.Equal("", row.Help)

[<Fact>]
let ``SemanticFields: domain omitted only on alias rows with domain`` () =
    let d = desc "/desk/files/delete" [||] "optional"
    let canonical = CatalogRouteEntry.semanticFields (CatalogRouteEntry.fromDescriptor d "/desk/files/delete")
    Assert.False canonical.DomainOmittedInPath
    let alias = CatalogRouteEntry.semanticFields (CatalogRouteEntry.fromDescriptor d "/desk/files/rm")
    Assert.True alias.DomainOmittedInPath

[<Fact>]
let ``KindOf: descriptor slot parses its ArgTail wire`` () =
    Assert.Equal(CommandArgTailKind.Required, CommandArgTailPolicy.kindOf (desc "/p" [||] "required"))
    Assert.Equal(CommandArgTailKind.ImplicitSelection, CommandArgTailPolicy.kindOf (desc "/p" [||] "implicit:selection"))
    Assert.Equal(CommandArgTailKind.Optional, CommandArgTailPolicy.kindOf (desc "/p" [||] "wat"))