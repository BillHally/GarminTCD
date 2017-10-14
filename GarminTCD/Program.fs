open System
open System.IO
open FSharp.Data
open FSharp.Charting
open OxyPlot
open System.Windows

type Garmin = XmlProvider<Schema="TrainingCenterDatabasev2.xsd">

type Activity =
    {
        DateTime : DateTime
        Laps : TimeSpan[]
        Time : TimeSpan
    }

module Garmin =
    let getActivities sport directory =
        DirectoryInfo(directory).GetFiles("*.tcx")
        |> Seq.map
            (
                fun x -> 
                    use stream = x.OpenRead()
                    let data = Garmin.Load(stream)

                    data.Activities.Value.Activities.[0]
            )
        |> Seq.sortBy (fun a -> a.Id)
        |> Seq.filter (fun x -> x.Sport = sport)

[<STAThread>]
[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    let directory = argv.[0]
    let ns =
        if argv.Length > 1 then
            argv.[1].Split([| ' '; ',' |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.map int
        else
            [| 5 |]

    let activities = directory |> Garmin.getActivities "Running"

//    results |> Array.iter (fun x -> printfn "%A: %A: %A" x.DateTime x.Laps x.Time)
    ns
    |> Array.map
        (
            fun n ->
                activities
                |> Seq.filter (fun x -> x.Laps.Length > n)
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
                |> Array.map (fun x -> x.DateTime, x.Time)
                |> (
                        fun xs ->
                            Chart.Point
                                (
                                    xs,
//                                    Color      = OxyColors.Blue,
                                    Title      = sprintf "Running (%d km)" n,
                                    XTitle     = "Date",
                                    YTitle     = "Time",
                                    MarkerSize = 2.0,
                                    MarkerType = MarkerType.Circle
                                )
                    )
        )
    |> Chart.Combine
    |> Chart.SavePdf (sprintf "%s.%skm.pdf" (DateTime.Now.ToString("yyyyMMdd")) (String.Join(".", Array.map string ns)))

    0
