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

module SerialLowLevel

open Result
open SerialNative
open System
open System.IO
open System.IO.Ports
open System.Runtime.InteropServices
open log4net

let logger = LogManager.GetLogger("SerialLowLevel");

type ReadWriteBuffering = 
    | NoBuffering
    | Buffered

// Low-level wrapper API around the native function calls
let getLastWin32Error<'a> :Result<'a, Exception> =
    try
        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error())
        Failure [new Exception()]
    with
    | ex -> Failure [ex]

// You probably don't want to buffer your writes to a serial device. I certainly don't.
let openPort name buffering =
    let fileOptionFlag =
        match buffering with
        | NoBuffering -> FILE_FLAG_NO_BUFFERING ||| FILE_FLAG_WRITE_THROUGH
        | Buffered -> 0

    let handle = CreateFile(name, FileAccess.ReadWrite, FileShare.None, IntPtr.Zero, FileMode.Open, fileOptionFlag, IntPtr.Zero)
    match handle.IsInvalid with
    | false -> Success handle
    | true -> getLastWin32Error

let getCommState handle =
    let mutable state = DCB()
    match GetCommState(handle, &state)  with
    | true -> Success state
    | false -> getLastWin32Error

let setCommState handle baudRate (parity:Parity) (stopBits:StopBits) (preserveState:bool) =
    let configure (state:DCB) =
        let mutable s = state
        s.BaudRate <- baudRate

        let encodeParity parity =
            match parity with
            | Parity.None -> byte 0
            | Parity.Odd -> byte 1
            | Parity.Even -> byte 2
            | Parity.Mark -> byte 3
            | Parity.Space -> byte 4
            | _ ->
                logger.Warn("encodeParity encountered unhandled case defaulting to Parity.None")
                byte 0

        s.Parity <- encodeParity parity

        let encodeStopBits stopBits =
            match stopBits with
            | StopBits.One -> byte 0
            | StopBits.OnePointFive -> byte 1
            | StopBits.Two -> byte 2
            | _ ->
                logger.Warn("encodeStopBits encountered unhandled case defaulting to StopBits.One")
                byte 0

        s.StopBits <- encodeStopBits stopBits
        s.ByteSize <- (8 |> byte)

        match SetCommState(handle, &s) with
        | true -> Success ()
        | false -> getLastWin32Error

    if preserveState
    then
        getCommState handle >>= configure
    else
        DCB() |> configure

let setTimeouts handle =
    let mutable timeouts = COMMTIMEOUTS(5)
    match SetCommTimeouts(handle, &timeouts) with
    | true -> Success ()
    | false -> getLastWin32Error

let read handle numBytes =
    let mutable buffer : byte array = Array.zeroCreate numBytes
    let mutable bytesRead = 0
    match ReadFile(handle, buffer, numBytes, &bytesRead, IntPtr.Zero) with
        | true -> buffer.[0..bytesRead-1] |> Success
        | false -> getLastWin32Error

let write handle data =
    let mutable bytesWritten = 0
    match WriteFile(handle, data, data.Length, &bytesWritten, IntPtr.Zero) with
        | true -> 
            if bytesWritten < data.Length
            then logger.Warn("WriteFile wrote " + bytesWritten.ToString() + 
                             " bytes instead of the requested " + data.Length.ToString())
            Success ()
        | false -> getLastWin32Error

let purgeComm handle =
    match PurgeComm(handle, 0) with
    | true -> Success ()
    | false -> getLastWin32Error
