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

module Serial

open SerialLowLevel
open Result
open System
open System.Threading
open log4net

let logger = LogManager.GetLogger("Serial");

type IDisposableAgent<'a> =
    inherit System.IDisposable
    abstract member Post: 'a -> unit

type AgentEvent =
    | Crash
    | Disposing

type SerialAgentCmd =
    | Write of byte array

(* 
   When an Exception is encountered during operation the encapsuled agent crashes itself
   and signals this to death watch, which can recreate the handler.

   However when we want to shut down the handler for good, we call the Dispose function,
   which will signal termination to death watch, and should not be recreated.
*)
#nowarn "40"
let serialAgent portName baudRate parity stopBits buffering (target:(byte[] -> unit) option) (deathWatch:(AgentEvent -> unit) option) =
    let cts = new CancellationTokenSource()
    
    let handle = 
        result {
            let! handle = openPort portName buffering
            do! setCommState handle baudRate parity stopBits false
            do! setTimeouts handle
            return handle
            }

    let cancel () =
        cts.Cancel ()
        result {
            let! handle = handle
            do purgeComm(handle) |> ignore
            do handle.Dispose()
            }
        |> ignore

    let crash () =
        cancel ()
        deathWatch |> Option.iter (fun w -> w Crash)

    let agent = 
        MailboxProcessor<SerialAgentCmd>.Start(
            fun inbox ->
                let rec messageLoop = async {
                    if inbox.CurrentQueueLength > 0
                    then
                        let! msg = inbox.Receive()
                        do match msg with
                            | Write msg ->
                                match handle >>= (fun handle -> write handle msg) with
                                | Success _ -> ()
                                | Failure errs ->
                                    logger.Error(errs)
                                    crash ()
                    else
                        let msg = handle >>= (fun handle -> read handle 64)
                        do match msg with
                            | Success msg ->
                                if msg.Length > 0 then target |> Option.iter (fun target -> target msg) else ()
                            | Failure errs ->
                                logger.Error(errs)
                                crash ()

                    return! messageLoop
                    }
                messageLoop
            , cancellationToken = cts.Token
            )

    { new IDisposableAgent<byte array> with
        member this.Dispose () = 
            cancel ()
            deathWatch |> Option.iter (fun w -> w Disposing)
        member this.Post data = 
            (Write data) |> agent.Post
        }

module Util =
    let adapter<'a, 'b> (convert: 'b -> 'a option) (agent:MailboxProcessor<'a>) = MailboxProcessor<'b>.Start(fun inbox ->
        let rec messageLoop = async {
            let! msg = inbox.Receive()
            let convertedMsg = convert msg
            do match convertedMsg with
                        | Some msg -> agent.Post msg
                        | _ -> ()
            return! messageLoop
            }
        messageLoop
        )

    let framing (delimiter:string) (target:(string -> unit) option) = MailboxProcessor.Start(fun inbox ->
        let rec messageLoop buffer = async {
            let! msg = inbox.Receive()
            let tokens = (String.concat "" [buffer;msg]).Split([|delimiter|], System.StringSplitOptions.None)
            for i in 0 .. tokens.Length - 2 do
                target |> Option.iter (fun target -> target tokens.[i])
            let newBuffer = tokens.[tokens.Length - 1]
            return! messageLoop newBuffer
        }
        messageLoop ""
        )

    let stringToByteAdapter =
        adapter (fun (byteMsg) -> byteMsg |> System.Text.ASCIIEncoding.ASCII.GetString |> Some)

    let byteToStringAdapter (agent:IDisposableAgent<byte array>) = {
        new IDisposableAgent<string> with
            member this.Dispose () = agent.Dispose ()
            member this.Post data = agent.Post (System.Text.ASCIIEncoding.ASCII.GetBytes(data))
        }

    type RecreateOnCrashCmd<'a> = 
    | Crash
    | Disposing
    | Msg of 'a

    (*
       Creates an instance of an IDisposableAgent and recreates it in the event of a Crash
       Relays messages to the agent and calls its Dispose function upon disposing and
       stops recreating it.
    *)
    let recreateOnCrash<'a> interval (watchable:((AgentEvent -> unit) option) -> IDisposableAgent<'a>) =
        let cts = new CancellationTokenSource()
        let agent = 
            MailboxProcessor.Start(
                fun inbox ->
                    let callback event =
                        match event with
                        | AgentEvent.Crash -> inbox.Post Crash
                        | AgentEvent.Disposing -> cts.Cancel ()

                    let rec messageLoop (watched:IDisposableAgent<'a>) = async {
                        let! msg = inbox.Receive ()
                        return! 
                            match msg with
                            | Msg m -> 
                                watched.Post m
                                messageLoop watched
                            | Crash ->
                                logger.Info("recreateOnCrash: recreating agent")
                                async {
                                    // Limiting the rate of retrying the connection
                                    do! Async.Sleep interval
                                    return! Some callback |> watchable |> messageLoop
                                    }
                            | Disposing ->
                                watched.Dispose ()
                                cts.Cancel ()
                                messageLoop watched
                    }
                    Some callback |> watchable |> messageLoop
                , cts.Token
                )

        { new IDisposableAgent<'a> with
            member this.Dispose () = agent.Post Disposing
            member this.Post data = agent.Post (Msg data)
            }