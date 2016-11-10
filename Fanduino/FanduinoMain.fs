(*
Copyright 2016 Attila Szarvas

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*)

module Fanduino.Main

open System.IO.Ports
open Maybe
open Serial
open Fanduino.TempMonitor
open Fanduino.Config
open log4net

let logger = LogManager.GetLogger("FanduinoMain");

// Calculates the linear interpolation of duty cycle between nearest neighbours
// Outside the curve's boundaries, returns the leftmost or rightmost value
let dutyCycle (curve:(int*int) list) (temp:int) =
    let (t0, d0) = try List.find (fun (t, d) -> t <= temp) curve with
                    _ -> match curve.IsEmpty with
                            | true -> (temp, 0)
                            | false -> 
                                let (t, d) = curve.Head
                                (temp, d)

    let (t1, d1) = try List.find (fun (t, d) -> t >= temp) curve with
                    _ -> match curve.IsEmpty with
                            | true -> (temp, 0)
                            | false -> 
                                let (t, d) = curve.Item(curve.Length-1)
                                (temp, d)

    if (t1 - t0) = 0
    then d0
    else d0 + (d1 - d0) * (temp - t0) / (t1 - t0)

// Calculates the duty cycle for every fan
let calculateDc (config:FanConfig list) (temps:Temps) =
    let cpuDc = match temps.cpu with
                | Some cpuTemp -> List.map (fun c -> dutyCycle c.cpu cpuTemp) config
                | None -> [ for i in 1 .. config.Length -> 0]

    let gpuDc = match temps.gpu with
                | Some gpuTemp -> List.map (fun c -> dutyCycle c.gpu gpuTemp) config
                | None -> [ for i in 1 .. config.Length -> 0]

    List.zip cpuDc gpuDc |> List.map (fun (a, b) -> max a b)

let logic (serialAgent:IDisposableAgent<string>) (config:FanConfig list) (t:Temps) =
    let dcVector = calculateDc config t
    let serialCmd = dcVector |> List.map (sprintf "%i ") |> List.fold (+) "" |> sprintf "c 1 %s"
    serialCmd |> serialAgent.Post

// Aggregates and prints data interesting to end users i.e. cpu and gpu temperatures,
// fan speeds and duty cycles reported by the Arduino
type InfoPrinterState = { temps:Temps; arduinoState:string }
type TempsOrString =
    | Temps of Temps
    | Arduino of string

let infoPrinter (target:string -> unit) =
    let createDisplayMsg state =
        let cpuMsg = match state.temps.cpu with
                        | Some t -> "CPU: " + t.ToString()
                        | None -> ""

        let gpuMsg = match state.temps.gpu with
                        | Some t -> " GPU: " + t.ToString()
                        | None -> ""

        let createArduinoMsg (msg:string) =
            let splitLength =  msg.Split(',').Length
            let fanSpeeds = (msg.Split(',').[0..splitLength/2-1])
            let dutyCycles = (msg.Split(',').[splitLength/2..splitLength-1])
            Array.zip fanSpeeds dutyCycles |> Seq.map (fun (speed, cycle) ->
                speed + " [" + cycle + "]  "
                )
            |> Seq.fold (+) ""

        let arduinoMsg = match state.arduinoState.Split(',').Length % 2 with
                            | 1 -> state.arduinoState
                            | _ -> createArduinoMsg (state.arduinoState)

        cpuMsg + gpuMsg + "\n" + arduinoMsg

    MailboxProcessor<TempsOrString>.Start(fun inbox ->
        let rec messageLoop state = async {
            let! msg = inbox.Receive()

            let newState = 
                match msg with
                | Temps temps -> { temps = temps; arduinoState = state.arduinoState }
                | Arduino s -> { temps = state.temps; arduinoState = s }

            target (createDisplayMsg newState)

            return! messageLoop newState
            }

        messageLoop { temps = { cpu = None; gpu = None }; arduinoState = "" }
        )

// This is the entry point for the program
let program (hook:string -> unit) =
    // The Arduino program expects 4 duty cycle values, but the config may contain
    // less. So we add as many FanConfigs as needed for 4.
    let config =
        if fanConfig.Length <= 4
        then
            [fanConfig; [ for i in 1 .. (4 - fanConfig.Length) -> {cpu=List.empty; gpu=List.empty} ]]
            |> List.concat
        else
            logger.Fatal("The configuration contains more than the supported maximum of 4 fan profiles")
            raise (System.Exception("The configuration contains more than the supported maximum of 4 fan profiles"))

    let ip = infoPrinter hook

    let framing = 
        Some (fun msg -> ip.Post (Arduino msg)) |> Util.framing "\r\n"
        |> Util.stringToByteAdapter

    let agent = 
        serialAgent Config.port 19200 Parity.None StopBits.One SerialLowLevel.NoBuffering (Some framing.Post)
        |> Util.recreateOnCrash 1000
        |> Util.byteToStringAdapter        

    let tempReader = 
        (fun t ->
            logic agent config t
            ip.Post (Temps t)
            )
        |> readTemps 2000

    { new System.IDisposable with
        member this.Dispose () = 
            tempReader.Dispose ()
            agent.Dispose ()
        }
