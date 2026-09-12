namespace AIGuiders.Platform.Modeling.Gdl.Correspondence

open System
open System.Text.RegularExpressions
open AIGuiders.Platform.Modeling.Paths

/// <summary>
/// Correspondence wire shapes — CRS SSOT (GUIDERS-FSHARP-ADR-0003 §4.8, ADR-0005/0006).
/// Pure model: records, const-modules, bracket-wire grammar. No IO, no workspace scan —
/// those are Execution consumers (Execution.Documentation.Correspondence.*).
/// Records are <see cref="CLIMutable"/> 1:1 with the transitional C# shapes — one shape,
/// one owner (this package); C# migrates to these types, never forks.
/// </summary>
module Schema =
    [<Literal>]
    let V0 = "correspondence/v0"

module Provenance =
    [<Literal>]
    let Bracket = "bracket"
    [<Literal>]
    let DocBody = "doc_body"
    [<Literal>]
    let WorkspaceToml = "workspace_toml"

module Kind =
    [<Literal>]
    let Documents = "documents"
    [<Literal>]
    let Implements = "implements"
    [<Literal>]
    let Related = "related"
    [<Literal>]
    let Constrains = "constrains"

    /// <summary>ADR/axiom normatively constrains code or graph scope (GUIDERS-FSHARP-ADR-0006).</summary>
    [<Literal>]
    let Normates = "normates"

    /// <summary>ADR facts block satisfied by golden session or CI evidence.</summary>
    [<Literal>]
    let VerifiedBy = "verified_by"

module AdrLifecycleTag =
    [<Literal>]
    let Proposed = "proposed"
    [<Literal>]
    let Accepted = "accepted"
    [<Literal>]
    let Implemented = "implemented"
    [<Literal>]
    let Superseded = "superseded"
    [<Literal>]
    let Deprecated = "deprecated"

[<CLIMutable>]
type AdrReference =
    { Id: string
      Fragment: string option }

[<CLIMutable>]
type ForwardDoc =
    { Path: string
      Title: string
      Abs: string option
      Kind: string option }

[<CLIMutable>]
type ReverseAnchor =
    { DocPath: string
      DocTitle: string
      Provenance: string
      Kind: string
      File: string
      LineStart: int option
      LineEnd: int option
      MemberKey: string option
      Wire: string
      DocLineHint: int option
      Excerpt: string option }

[<CLIMutable>]
type ExplicitCodeAnchor =
    { DocPath: string
      File: string
      LineStart: int option
      LineEnd: int option
      MemberKey: string option
      Provenance: string
      Kind: string
      DefaultKind: string }

[<CLIMutable>]
type CorrespondenceResult =
    { WorkspaceRoot: string
      FileRel: string option
      FeatureLine: string option
      FeatureDocs: string array
      AdrLine: string
      ForwardDocs: ForwardDoc array
      ReverseAnchors: ReverseAnchor array
      ActiveLayers: string array
      TomlPath: string }

[<CLIMutable>]
type ForwardMapResult =
    { FeatureLine: string option
      FeatureDocs: string array
      AdrLine: string
      DocPaths: string list
      ForwardDocs: ForwardDoc array }

/// <summary>
/// Bracket wire grammar: `[F:path; M:member]` / `[F:path; L:start; L2:end]`.
/// Pure functions (no IO). Keys F/M/L/L2/S/K are the cross-planet anchor vocabulary.
/// </summary>
[<RequireQualifiedAccess>]
module BracketWire =
    let private keySep =
        Regex(@"(?<=\S)\s+(?=[FMLSK]:)", RegexOptions.CultureInvariant)

    /// <summary>Build a bracket wire. Member wins over line span (C# parity).</summary>
    let build (file: string) (lineStart: int option) (lineEnd: int option) (memberKey: string option) : string =
        let parts = ResizeArray<string>()
        parts.Add($"F:{LogicalPathOps.normalize file}")
        match memberKey with
        | Some m when not (String.IsNullOrWhiteSpace m) -> parts.Add($"M:{m}")
        | _ ->
            match lineStart with
            | Some ls ->
                parts.Add($"L:{ls}")
                match lineEnd with
                | Some le when le <> ls -> parts.Add($"L2:{le}")
                | _ -> ()
            | None -> ()
        "[" + String.Join("; ", parts) + "]"

    /// <summary>
    /// Parse a bracket wire. Normalizes separators (whitespace before F:/M:/L:/L2:/S:/K:)
    /// and the path via LogicalPathOps. Returns Some(file, lineStart, lineEnd, memberKey)
    /// only when a file key is present.
    /// </summary>
    let tryParseBracket (bracket: string) : (string * int option * int option * string option) option =
        if String.IsNullOrWhiteSpace bracket then
            None
        else
            let raw = keySep.Replace(bracket.Trim().Trim('[', ']'), "; ")
            let mutable file = ""
            let mutable lineStart = None
            let mutable lineEnd = None
            let mutable memberKey = None

            for part in
                raw.Split(';', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries) do
                if part.StartsWith("F:", StringComparison.OrdinalIgnoreCase) then
                    file <- LogicalPathOps.normalize part[2..]
                elif part.StartsWith("M:", StringComparison.OrdinalIgnoreCase) then
                    memberKey <- Some(part[2..].Trim())
                elif part.StartsWith("L:", StringComparison.OrdinalIgnoreCase) then
                    match Int32.TryParse(part[2..].Trim()) with
                    | true, ln ->
                        lineStart <- Some ln
                        if lineEnd.IsNone then lineEnd <- Some ln
                    | _ -> ()

            if file.Length > 0 then
                Some(file, lineStart, lineEnd, memberKey)
            else
                None