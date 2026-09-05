namespace AIGuiders.Platform.Modeling.Ide.Session

open Xunit

type RegistryStubProvider(name: string, fingerprint: string) =
    interface ISolutionInfoProvider with
        member _.Name = name
        member _.Fingerprint() = fingerprint
        member _.Entries() = []
        member _.Relations() = []

type SolutionProviderRegistryTests() =

    [<Fact>]
    member _.``Register, names, create, createAll, capabilities``() =
        let unique = System.Guid.NewGuid().ToString "N"
        let n1 = "stub-a-" + unique
        let n2 = "stub-b-" + unique

        SolutionProviderRegistry.register
            n1
            (fun anchor -> RegistryStubProvider(n1, anchor + "|a") :> ISolutionInfoProvider)

        SolutionProviderRegistry.register
            n2
            (fun anchor -> RegistryStubProvider(n2, anchor + "|b") :> ISolutionInfoProvider)

        let names = SolutionProviderRegistry.names ()
        Assert.Contains(n1, names)
        Assert.Contains(n2, names)

        match SolutionProviderRegistry.create n1 "anchor" with
        | Some p -> Assert.Equal("anchor|a", p.Fingerprint ())
        | None -> failwith "expected provider"

        Assert.True((SolutionProviderRegistry.create ("stub-missing-" + unique) "x").IsNone)

        let caps = SolutionProviderRegistry.capabilities "anchor" |> Map.ofList
        Assert.True(Map.containsKey n1 caps)
        Assert.True(Map.containsKey n2 caps)

    [<Fact>]
    member _.``Re-register overwrites (idempotent plugin catalog)``() =
        let name = "stub-overwrite-" + System.Guid.NewGuid().ToString "N"

        SolutionProviderRegistry.register name (fun _ -> RegistryStubProvider(name, "v1") :> ISolutionInfoProvider)
        SolutionProviderRegistry.register name (fun _ -> RegistryStubProvider(name, "v2") :> ISolutionInfoProvider)

        match SolutionProviderRegistry.create name "x" with
        | Some p -> Assert.Equal("v2", p.Fingerprint ())
        | None -> failwith "expected provider"
