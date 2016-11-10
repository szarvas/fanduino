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

module Fanduino.TempMonitor

open System.Threading
open OpenHardwareMonitor.Hardware
open Maybe

type Temps = { cpu:int option; gpu:int option }

let computer =
    let mutable computer = new Computer()
    computer.CPUEnabled <- true
    computer.GPUEnabled <- true
    computer.Open() |> ignore
    computer

#nowarn "40"
let readTemps (interval:int) (target:Temps -> unit) = 
    let cts = new CancellationTokenSource()
    let computer = computer
    cts.Token.Register(fun () -> computer.Close()) |> ignore
    
    let updateHw (hw:IHardware) =
        hw.Update()
        for sh in hw.SubHardware do
            sh.Update()

    let cpuTemp () =
        maybe {
            let! cpu = 
                try Array.find (fun (hw:IHardware) -> hw.HardwareType = HardwareType.CPU) (computer.Hardware) |> Some with
                    _ -> None
            do updateHw cpu
            let! sensor = 
                try Array.find (fun (s:ISensor) -> (s.SensorType = SensorType.Temperature) && (s.Name = "CPU Package")) cpu.Sensors |> Some with
                    _ -> None

            return! 
                if sensor.Value.HasValue then sensor.Value.Value |> int |> Some else None
        }

    let gpuTemp () =
        maybe {
            let! gpu = 
                try Array.find (fun (hw:IHardware) -> (hw.HardwareType = HardwareType.GpuAti || hw.HardwareType = HardwareType.GpuNvidia)) (computer.Hardware) |> Some with
                    _ -> None
            do updateHw gpu
            let! sensor = 
                try Array.find (fun (s:ISensor) -> (s.SensorType = SensorType.Temperature) && (s.Name = "GPU Core")) gpu.Sensors |> Some with
                    _ -> None

            return! 
                if sensor.Value.HasValue then sensor.Value.Value |> int |> Some else None
        }

    let rec loop = async {
        do! Async.Sleep(interval)
        do target { cpu = cpuTemp (); gpu = gpuTemp () }
        return! loop
        }

    Async.Start (loop, cts.Token)
    
    { new System.IDisposable with
        member this.Dispose () = cts.Cancel()
        }
