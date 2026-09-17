namespace AIGuiders.Platform.Modeling.Core.Identity

open System
open System.Globalization
open System.Numerics
open AIGuiders.Platform.Modeling.Paths

/// Wire ingress — raw shapes only at parse boundaries.
type CarrierWire =
    | Numeric of bigint
    | Uuid of Guid
    | Text of string
    | Path of LogicalPath

[<Struct; StructuralEquality; StructuralComparison>]
type NumericId = NumericId of value: int64

module NumericId =
    let ofCounter (value: int64) = NumericId value
    let value (NumericId v) = v

[<Struct; StructuralEquality; StructuralComparison>]
type GuidId = GuidId of value: Guid

module GuidId =
    let create (value: Guid) = GuidId value
    let value (GuidId v) = v

[<Struct; StructuralEquality; StructuralComparison>]
type Sha256 = Sha256 of bytes: byte[]

module Sha256 =
    let create (bytes: byte[]) = Sha256 bytes
    let bytes (Sha256 b) = b

[<Struct; StructuralEquality; StructuralComparison>]
type Identity<'entity, 'carrier when 'carrier : comparison> = Identity of carrier: 'carrier

module Identity =
    let mint carrier = Identity carrier
    let carrier (Identity c) = c

type Document = struct end
type Diagnostic = struct end
type SyntaxNode = struct end
type Artifact = struct end
type GitCommit = struct end

type DocId = Identity<Document, NumericId>
type DiagnosticRef = Identity<Diagnostic, NumericId>
type NodeId = Identity<SyntaxNode, NumericId>
type ArtifactRef = Identity<Artifact, GuidId>
type CommitRef = Identity<GitCommit, Sha256>

module DocId =
    let mint = Identity.mint
    let carrier = Identity.carrier

module DiagnosticRef =
    let mint = Identity.mint
    let carrier = Identity.carrier

module NodeId =
    let mint = Identity.mint
    let carrier = Identity.carrier

module ArtifactRef =
    let mint = Identity.mint
    let carrier = Identity.carrier

module CommitRef =
    let mint = Identity.mint
    let carrier = Identity.carrier

[<Struct; CustomEquality; NoComparison>]
type ProjectId =
    | ProjectId of path: LogicalPath

    override this.Equals o =
        match o with
        | :? ProjectId as other ->
            let (ProjectId left) = this
            let (ProjectId right) = other
            String.Equals(left.Value, right.Value, StringComparison.OrdinalIgnoreCase)
        | _ -> false

    override this.GetHashCode() =
        let (ProjectId p) = this
        StringComparer.OrdinalIgnoreCase.GetHashCode(p.Value)

module ProjectId =
    let create path = ProjectId path
    let path (ProjectId p) = p

type GitPin = { Commit: CommitRef option }

module CarrierWire =
    let private tryToInt64 (value: bigint) =
        if value < bigint Int64.MinValue || value > bigint Int64.MaxValue then
            Error "numeric wire value out of int64 range"
        else
            Ok(NumericId.ofCounter (int64 value))

    let parseNumeric wire =
        match wire with
        | CarrierWire.Numeric n -> tryToInt64 n
        | _ -> Error "expected numeric carrier wire"

    let parseUuid wire =
        match wire with
        | CarrierWire.Uuid g -> Ok(GuidId.create g)
        | _ -> Error "expected uuid carrier wire"

    let parsePath wire =
        match wire with
        | CarrierWire.Path p -> Ok p
        | _ -> Error "expected path carrier wire"

module ParseCommit =
    let private parseHexBytes (hex: string) =
        if String.IsNullOrWhiteSpace hex then
            Error "commit hash is empty"
        else
            let trimmed = hex.Trim()

            if trimmed.Length <> 64 then
                Error "commit hash must be 64 hex characters (sha256)"
            elif trimmed |> Seq.exists (fun c -> not (Uri.IsHexDigit c)) then
                Error "commit hash contains non-hex characters"
            else
                let bytes = Array.zeroCreate<byte> 32

                for i in 0 .. 31 do
                    bytes.[i] <-
                        System.Byte.Parse(trimmed.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)

                Ok(Sha256.create bytes)

    let parse (hex: string) = parseHexBytes hex |> Result.map CommitRef.mint
