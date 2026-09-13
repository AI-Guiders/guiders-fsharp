namespace AIGuiders.Platform.Modeling.Gdl.Correspondence

open System
open System.Collections.Generic
open System.Text.RegularExpressions

/// <summary>
/// SSOT catalog for Hoare obligations and well-formedness ids declared in ADR facts (SAT-003).
/// GUIDERS-FSHARP-ADR-0007 pilot entries: OB-H1..H3, WF-OB1..OB2.
/// </summary>
[<CLIMutable>]
type HoareCatalogEntry =
    { Id: string
      AdrSource: string }

[<CLIMutable>]
type WellFormednessCatalogEntry =
    { Id: string
      AdrSource: string }

[<RequireQualifiedAccess>]
module HoareObligationCatalog =
    let private adr0007 = "GUIDERS-FSHARP-ADR-0007"

    let private hoareEntries =
        [| { Id = "OB-H1"; AdrSource = adr0007 }
           { Id = "OB-H2"; AdrSource = adr0007 }
           { Id = "OB-H3"; AdrSource = adr0007 } |]

    let private wfEntries =
        [| { Id = "WF-OB1"; AdrSource = adr0007 }
           { Id = "WF-OB2"; AdrSource = adr0007 } |]

    let private hoareIds =
        HashSet<string>(hoareEntries |> Array.map (fun e -> e.Id), StringComparer.OrdinalIgnoreCase)

    let private wfIds =
        HashSet<string>(wfEntries |> Array.map (fun e -> e.Id), StringComparer.OrdinalIgnoreCase)

    let private hoareShapeRegex =
        Regex(@"^OB-H\d+$", RegexOptions.Compiled ||| RegexOptions.CultureInvariant)

    let private wfShapeRegex =
        Regex(@"^WF-OB\d+$", RegexOptions.Compiled ||| RegexOptions.CultureInvariant)

    let isRegistered (obligationId: string) =
        not (String.IsNullOrWhiteSpace obligationId)
        && hoareIds.Contains(obligationId.Trim())

    let isRegisteredWf (wfId: string) =
        not (String.IsNullOrWhiteSpace wfId)
        && wfIds.Contains(wfId.Trim())

    let registeredIds () =
        hoareEntries
        |> Array.map (fun e -> e.Id)
        |> Array.sortBy (fun id -> id.ToUpperInvariant())

    let registeredWfIds () =
        wfEntries
        |> Array.map (fun e -> e.Id)
        |> Array.sortBy (fun id -> id.ToUpperInvariant())

    /// <summary>v1: registered catalog id with known obligation shape.</summary>
    let validateObligation (obligationId: string) =
        if String.IsNullOrWhiteSpace obligationId then
            false
        else
            let trimmed = obligationId.Trim()
            isRegistered trimmed && hoareShapeRegex.IsMatch trimmed

    /// <summary>v1: registered catalog id with known well-formedness shape.</summary>
    let validateWellFormedness (wfId: string) =
        if String.IsNullOrWhiteSpace wfId then
            false
        else
            let trimmed = wfId.Trim()
            isRegisteredWf trimmed && wfShapeRegex.IsMatch trimmed

    let entries () = hoareEntries

    let wellFormednessEntries () = wfEntries
