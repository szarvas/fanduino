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

module Fanduino.Config

open FSharp.Configuration
open log4net
open System.Diagnostics
open System.IO

let logger = LogManager.GetLogger("FanduinoMain");

type ConfigType = YamlConfig<"Config.yaml">

let yamlConfig =
    try
        let c = ConfigType()
        // Workaround for https://github.com/fsprojects/FSharp.Configuration/issues/66
        let cfgFile = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Config.yaml"
        c.Load(cfgFile)
        c
    with
        _ -> 
            logger.Fatal("Malformed config file (Config.yaml)")
            raise (System.Exception("Malformed config file (Config.yaml)"))

let startMinimized = yamlConfig.StartMinimized = "Yes"
let port = yamlConfig.Port

type FanConfig = { cpu:(int*int) list; gpu:(int*int) list }

let fanConfig =
    yamlConfig.FanConfig |> Seq.map (fun e -> 
        let cpuList = 
            try
                e.cpu |> Seq.map (fun e ->
                    (e.Item(0), e.Item(1))
                    )
                |> Seq.toList
            with
            _ -> List.empty

        let gpuList = 
            try
                e.gpu |> Seq.map (fun e ->
                    (e.Item(0), e.Item(1))
                    )
                |> Seq.toList
            with
            _ -> List.empty

        { cpu = cpuList; gpu = gpuList }
        )
    |> Seq.toList