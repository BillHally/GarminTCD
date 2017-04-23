open System
open System.IO
open FSharp.Data
open FSharp.Charting
open OxyPlot

type Garmin = XmlProvider<Schema="TrainingCenterDatabasev2.xsd">

type Activity =
    {
        DateTime : DateTime
        Laps : TimeSpan[]
        Time : TimeSpan
    }

[<STAThread>]
[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    let directory = argv.[0]
    let n = if argv.Length > 1 then int argv.[1] else 5

    let results =
        DirectoryInfo(directory).GetFiles("*.tcx")
        |> Seq.map
            (
                fun x -> 
                    use stream = x.OpenRead()
                    let data = Garmin.Load(stream)

                    data.Activities.Value.Activities.[0]
            )
        |> Seq.sortBy (fun a -> a.Id)
        |> Seq.filter (fun x -> x.Sport = "Running" && x.Laps.Length > n)
        |> Seq.map
            (
                fun x ->
                    let laps =
                        x.Laps.[1..n]
                        |> Array.mapi (fun i lap -> lap.StartTime - x.Laps.[i].StartTime)

                    {
                        DateTime = x.Id
                        Laps = laps
                        Time = x.Laps.[n].StartTime - x.Laps.[0].StartTime
                    }
            )
        |> Array.ofSeq

    results|> Array.iter (fun x -> printfn "%A: %A: %A" x.DateTime x.Laps x.Time)

    results
    |> Array.map (fun x -> x.DateTime, x.Time)
    |> (
            fun xs ->
                Chart.Point
                    (
                        xs,
                        Color = OxyColors.Blue,
                        Title = sprintf "Running (%d km)" n,
                        XTitle = "Date",
                        YTitle = "Time",
                        MarkerSize = 2.0,
                        MarkerType = MarkerType.Circle
                    )
        )
    |> Chart.Show()

    0
