namespace AIGuiders.Platform.Modeling.CodeCenter.Tests

open System
open System.Diagnostics
open Xunit
open AIGuiders.Platform.Modeling.CodeCenter

module PerfConformanceTests =

    let perfFixture =
        String.Join(
            Environment.NewLine,
            [ "@dashboard perf"
              yield! [ for i in 1..250 -> $"    tab t{i} as \"Tab {i}\"" ]
              "end dashboard" ]
        )

    [<Fact>]
    let ``V15 mechanical perf point and region within SLA envelope`` () =
        let session0 = DocumentSession.Create("doc://mechanical-perf", perfFixture)
        Assert.True(session0.Text.Length >= 500)

        let pointTimes = ResizeArray()
        let mutable session = session0

        for i in 1..200 do
            let sw = Stopwatch.StartNew()

            session <-
                match
                    session.ApplyMechanicalEdit(
                        { Scope = Point
                          RemovedSpan = (session.Text.Length - 1, 0)
                          InsertedText = "x" }
                    )
                with
                | Ok s -> s
                | Error e -> Assert.Fail e; session0

            sw.Stop()
            pointTimes.Add sw.ElapsedMilliseconds

        let regionTimes = ResizeArray()

        for _ in 1..50 do
            let sw = Stopwatch.StartNew()

            session <-
                match
                    session.ApplyMechanicalEdit(
                        { Scope = Region
                          RemovedSpan = (0, min 4 session.Text.Length)
                          InsertedText = "z" }
                    )
                with
                | Ok s -> s
                | Error e -> Assert.Fail e; session

            sw.Stop()
            regionTimes.Add sw.ElapsedMilliseconds

        let documentReparses =
            session.RefreshScopes |> List.filter ((=) RefreshScope.Document) |> List.length

        let p95 values =
            let sorted = values |> Seq.sort |> Seq.toArray
            let idx = max 0 (int (float sorted.Length * 0.95) - 1)
            sorted.[min idx (sorted.Length - 1)]

        let pointP95 = p95 pointTimes
        let regionP95 = p95 regionTimes

        Assert.Equal(0, documentReparses)
        Assert.True(pointP95 <= 25L, $"Point p95 {pointP95}ms exceeds 25ms SLA")
        Assert.True(regionP95 <= 40L, $"Region p95 {regionP95}ms exceeds 40ms SLA")
