namespace AIGuiders.Platform.Modeling.Paths

open System
open System.IO

[<RequireQualifiedAccess>]
module LogicalPathOps =
    let normalize (raw: string) =
        if String.IsNullOrWhiteSpace raw then
            ""
        else
            let mutable normalized = raw.Trim().Replace('\\', '/')

            while normalized.Contains("//", StringComparison.Ordinal) do
                normalized <- normalized.Replace("//", "/", StringComparison.Ordinal)

            normalized.TrimStart('.').TrimStart('/').TrimEnd('/')

[<RequireQualifiedAccess>]
module LogicalPathMatching =
    let matches (candidatePath: string) (anchorRel: string) (anchorFileName: string) =
        let c = LogicalPathOps.normalize candidatePath

        if String.Equals(c, anchorRel, StringComparison.OrdinalIgnoreCase) then
            true
        elif c.EndsWith("/" + anchorRel, StringComparison.OrdinalIgnoreCase) then
            true
        else
            String.Equals(Path.GetFileName c, anchorFileName, StringComparison.OrdinalIgnoreCase)
            && (anchorRel.EndsWith("/" + anchorFileName, StringComparison.OrdinalIgnoreCase)
                || String.Equals(anchorRel, anchorFileName, StringComparison.OrdinalIgnoreCase))

/// <summary>Repo/workspace-relative path with forward-slash canonical form (GUIDERS-ADR-0050).</summary>
[<Struct; CustomEquality; NoComparison>]
type LogicalPath =
    { Value: string }

    static member Normalize(raw: string) = LogicalPathOps.normalize raw

    static member Empty = { Value = "" }

    static member Create(value: string) = { Value = LogicalPathOps.normalize value }

    member this.IsEmpty = String.IsNullOrEmpty this.Value

    static member Parse(raw: string) = LogicalPath.Create raw

    static member TryParse(raw: string) =
        if isNull raw then
            None
        else
            Some(LogicalPath.Create raw)

    member this.AsDocPath() =
        let trimmed = this.Value.TrimStart('/')
        if trimmed = this.Value then this else LogicalPath.Create trimmed

    member this.Combine(segment: string) =
        if String.IsNullOrWhiteSpace segment then
            this
        else
            let left = this.Value.TrimEnd('/')
            let right = LogicalPathOps.normalize(segment).TrimStart('/')

            if String.IsNullOrEmpty left then
                LogicalPath.Create right
            elif String.IsNullOrEmpty right then
                this
            else
                LogicalPath.Create($"{left}/{right}")

    member this.StartsWith(prefix: LogicalPath) = this.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)

    member this.StartsWith(prefix: LogicalPath, comparison: StringComparison) =
        prefix.IsEmpty || this.Value.StartsWith(prefix.Value, comparison)

    member this.MatchesAnchor(anchor: LogicalPath, anchorFileName: string) =
        LogicalPathMatching.matches this.Value anchor.Value anchorFileName

    override this.ToString() = this.Value

    interface IEquatable<LogicalPath> with
        member this.Equals(other) =
            String.Equals(this.Value, other.Value, StringComparison.Ordinal)

    override this.Equals(obj) =
        match obj with
        | :? LogicalPath as other -> (this :> IEquatable<LogicalPath>).Equals(other)
        | _ -> false

    override this.GetHashCode() =
        StringComparer.Ordinal.GetHashCode(this.Value)
