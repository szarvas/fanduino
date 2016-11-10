(*  
    This file contains F# bindings to the native Win32 API functions

    The native function signatures were copied from the Win32 header files
    to help with finding mistakes
*)

module SerialNative

open System.Runtime.InteropServices

// Flags from WinBase.h
let FILE_FLAG_NO_BUFFERING = 0x20000000
let FILE_FLAG_WRITE_THROUGH = 0x80000000

(* Declaration from `fileapi.h`

WINBASEAPI
HANDLE
WINAPI
CreateFileW(
    _In_ LPCWSTR lpFileName,
    _In_ DWORD dwDesiredAccess,
    _In_ DWORD dwShareMode,
    _In_opt_ LPSECURITY_ATTRIBUTES lpSecurityAttributes,
    _In_ DWORD dwCreationDisposition,
    _In_ DWORD dwFlagsAndAttributes,
    _In_opt_ HANDLE hTemplateFile
    );
*)

(* Declaration from `minwinbase.h`

typedef struct _SECURITY_ATTRIBUTES {
    DWORD nLength;
    LPVOID lpSecurityDescriptor;
    BOOL bInheritHandle;
} SECURITY_ATTRIBUTES, *PSECURITY_ATTRIBUTES, *LPSECURITY_ATTRIBUTES;
*)
[<DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)>]
extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFile(
    [<MarshalAs(UnmanagedType.LPWStr)>] string               lpFileName,
    [<MarshalAs(UnmanagedType.U4)>]     System.IO.FileAccess dwDesiredAccess,
    [<MarshalAs(UnmanagedType.U4)>]     System.IO.FileShare  dwShareMode,
    System.IntPtr lpSecurityAttributes, // this will be NULL meaning that no child process can inherit the
                                        // handle created by CreateFile
    [<MarshalAs(UnmanagedType.U4)>]     System.IO.FileMode   dwCreationDisposition,
    [<MarshalAs(UnmanagedType.U4)>]     int                  dwFlagsAndAttributes,
    System.IntPtr hTemplateFile
);

(* Declaration from `handleapi.h`

WINBASEAPI
BOOL
WINAPI
CloseHandle(
    _In_ HANDLE hObject
    );
*)
[<DllImport("kernel32", SetLastError = true)>]
[<MarshalAs(UnmanagedType.Bool)>] 
extern System.Boolean CloseHandle(
    System.IntPtr hObject
);

(* Declaration from `WinBase.h`

typedef struct _DCB {
    DWORD DCBlength;      /* sizeof(DCB)                     */
    DWORD BaudRate;       /* Baudrate at which running       */
    DWORD fBinary: 1;     /* Binary Mode (skip EOF check)    */
    DWORD fParity: 1;     /* Enable parity checking          */
    DWORD fOutxCtsFlow:1; /* CTS handshaking on output       */
    DWORD fOutxDsrFlow:1; /* DSR handshaking on output       */
    DWORD fDtrControl:2;  /* DTR Flow control                */
    DWORD fDsrSensitivity:1; /* DSR Sensitivity              */
    DWORD fTXContinueOnXoff: 1; /* Continue TX when Xoff sent */
    DWORD fOutX: 1;       /* Enable output X-ON/X-OFF        */
    DWORD fInX: 1;        /* Enable input X-ON/X-OFF         */
    DWORD fErrorChar: 1;  /* Enable Err Replacement          */
    DWORD fNull: 1;       /* Enable Null stripping           */
    DWORD fRtsControl:2;  /* Rts Flow control                */
    DWORD fAbortOnError:1; /* Abort all reads and writes on Error */
    DWORD fDummy2:17;     /* Reserved                        */
    WORD wReserved;       /* Not currently used              */
    WORD XonLim;          /* Transmit X-ON threshold         */
    WORD XoffLim;         /* Transmit X-OFF threshold        */
    BYTE ByteSize;        /* Number of bits/byte, 4-8        */
    BYTE Parity;          /* 0-4=None,Odd,Even,Mark,Space    */
    BYTE StopBits;        /* 0,1,2 = 1, 1.5, 2               */
    char XonChar;         /* Tx and Rx X-ON character        */
    char XoffChar;        /* Tx and Rx X-OFF character       */
    char ErrorChar;       /* Error replacement char          */
    char EofChar;         /* End of Input character          */
    char EvtChar;         /* Received Event character        */
    WORD wReserved1;      /* Fill for now.                   */
} DCB, *LPDCB;
*)

#nowarn "9"
[<type: StructLayout(LayoutKind.Sequential)>]
type DCB =
    struct
        val mutable DCBlength          :System.UInt32   (* sizeof(DCB)                     *)
        [<MarshalAs(UnmanagedType.U4)>]
        val mutable BaudRate           :int             (* Baudrate at which running       *)
    
        (* All the following is packed into 32 bits
        fBinary            :System.UInt32   (* Binary Mode (skip EOF check)    *)
        fParity            :System.UInt32   (* Enable parity checking          *)
        fOutxCtsFlow       :System.UInt32   (* CTS handshaking on output       *)
        fOutxDsrFlow       :System.UInt32   (* DSR handshaking on output       *)
        fDtrControl        :System.UInt32   (* DTR Flow control                *)
        fDsrSensitivity    :System.UInt32   (* DSR Sensitivity                 *)
        fTXContinueOnXoff  :System.UInt32   (* Continue TX when Xoff sent      *)
        fOutX              :System.UInt32   (* Enable output X-ON/X-OFF        *)
        fInX               :System.UInt32   (* Enable input X-ON/X-OFF         *)
        fErrorChar         :System.UInt32   (* Enable Err Replacement          *)
        fNull              :System.UInt32   (* Enable Null stripping           *)
        fRtsControl        :System.UInt32   (* Rts Flow control                *)
        fAbortOnError      :System.UInt32   (* Abort all reads and writes on Error *)
        fDummy2            :System.UInt32   (* Reserved                        *)
        *)

        val mutable LoadsAVariables    :System.UInt32
        val mutable wReserved          :System.UInt16   (* Not currently used              *)
        val mutable XonLim             :System.UInt16   (* Transmit X-ON threshold         *)
        val mutable XoffLim            :System.UInt16   (* Transmit X-OFF threshold        *)
        val mutable ByteSize           :System.Byte     (* Number of bits/byte, 4-8        *)
        [<MarshalAs(UnmanagedType.U1)>]
        val mutable Parity             :byte            (* 0-4=None,Odd,Even,Mark,Space    *)
        [<MarshalAs(UnmanagedType.U1)>]
        val mutable StopBits           :byte            (* 0,1,2 = 1, 1.5, 2               *)
        val mutable XonChar            :System.Char     (* Tx and Rx X-ON character        *)
        val mutable XoffChar           :System.Char     (* Tx and Rx X-OFF character       *)
        val mutable ErrorChar          :System.Char     (* Error replacement char          *)
        val mutable EofChar            :System.Char     (* End of Input character          *)
        val mutable EvtChar            :System.Char     (* Received Event character        *)
        val mutable wReserved1         :System.UInt16   (* Fill for now.                   *)
    end

(* Declaration from `WinBase.h`

WINBASEAPI
BOOL
WINAPI
GetCommState(
    _In_  HANDLE hFile,
    _Out_ LPDCB lpDCB
    );
*)
[<DllImport("kernel32")>]
[<MarshalAs(UnmanagedType.Bool)>] 
extern System.Boolean GetCommState(
    Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
    DCB& lpDCB
);

(* Declaration from `WinBase.h`

WINBASEAPI
BOOL
WINAPI
SetCommState(
    _In_ HANDLE hFile,
    _In_ LPDCB lpDCB
    );
*)
[<DllImport("kernel32")>]
[<MarshalAs(UnmanagedType.Bool)>] 
extern System.Boolean SetCommState(
    Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
    DCB& lpDCB
);

(* Declaration from `WinBase.h`

typedef struct _COMMTIMEOUTS {
    DWORD ReadIntervalTimeout;          /* Maximum time between read chars. */
    DWORD ReadTotalTimeoutMultiplier;   /* Multiplier of characters.        */
    DWORD ReadTotalTimeoutConstant;     /* Constant in milliseconds.        */
    DWORD WriteTotalTimeoutMultiplier;  /* Multiplier of characters.        */
    DWORD WriteTotalTimeoutConstant;    /* Constant in milliseconds.        */
} COMMTIMEOUTS,*LPCOMMTIMEOUTS;
*)
#nowarn "9"
[<type: StructLayout(LayoutKind.Sequential)>]
type COMMTIMEOUTS =
    struct
        [<MarshalAs(UnmanagedType.U4)>] val ReadIntervalTimeout          :int
        [<MarshalAs(UnmanagedType.U4)>] val ReadTotalTimeoutMultiplier   :int
        [<MarshalAs(UnmanagedType.U4)>] val ReadTotalTimeoutConstant     :int
        [<MarshalAs(UnmanagedType.U4)>] val WriteTotalTimeoutMultiplier  :int
        [<MarshalAs(UnmanagedType.U4)>] val WriteTotalTimeoutConstant    :int
        new (p:int) = { ReadIntervalTimeout = p; ReadTotalTimeoutMultiplier = p; ReadTotalTimeoutConstant = p; WriteTotalTimeoutMultiplier = p; WriteTotalTimeoutConstant = p }
    end

(* Declaration from `WinBase.h`

WINBASEAPI
BOOL
WINAPI
SetCommTimeouts(
    _In_ HANDLE hFile,
    _In_ LPCOMMTIMEOUTS lpCommTimeouts
    );
*)
[<DllImport("kernel32")>]
[<MarshalAs(UnmanagedType.Bool)>] 
extern System.Boolean SetCommTimeouts(
    Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
    COMMTIMEOUTS& lpCommTimeouts
);

(* Declaration from `fileapi.h`

WINBASEAPI
_Must_inspect_result_
BOOL
WINAPI
ReadFile(
    _In_ HANDLE hFile,
    _Out_writes_bytes_to_opt_(nNumberOfBytesToRead, *lpNumberOfBytesRead) __out_data_source(FILE) LPVOID lpBuffer,
    _In_ DWORD nNumberOfBytesToRead,
    _Out_opt_ LPDWORD lpNumberOfBytesRead,
    _Inout_opt_ LPOVERLAPPED lpOverlapped
    );
*)
[<DllImport("kernel32")>]
[<MarshalAs(UnmanagedType.Bool)>] 
extern System.Boolean ReadFile(
    Microsoft.Win32.SafeHandles.SafeFileHandle handle,
    byte[] lpBuffer,
    [<MarshalAs(UnmanagedType.U4)>] int nNumberOfBytesToRead,
    int& lpNumberOfBytesRead,
    System.IntPtr lpOverlapped // we will be using non-overlapped IO
);

(* Declaration from `fileapi.h`

WINBASEAPI
BOOL
WINAPI
WriteFile(
    _In_ HANDLE hFile,
    _In_reads_bytes_opt_(nNumberOfBytesToWrite) LPCVOID lpBuffer,
    _In_ DWORD nNumberOfBytesToWrite,
    _Out_opt_ LPDWORD lpNumberOfBytesWritten,
    _Inout_opt_ LPOVERLAPPED lpOverlapped
    );
*)
[<DllImport("kernel32")>]
[<MarshalAs(UnmanagedType.Bool)>] 
extern System.Boolean WriteFile(
    Microsoft.Win32.SafeHandles.SafeFileHandle handle,
    byte[] lpBuffer,
    [<MarshalAs(UnmanagedType.U4)>] int nNumberOfBytesToWrite,
    int& lpNumberOfBytesWritten,
    System.IntPtr lpOverlapped // we will be using non-overlapped IO
);

(* Declaration from `WinBase.h`

WINBASEAPI
BOOL
WINAPI
PurgeComm(
    _In_ HANDLE hFile,
    _In_ DWORD dwFlags
    );
*)
[<DllImport("kernel32")>]
[<MarshalAs(UnmanagedType.Bool)>] 
extern System.Boolean PurgeComm(
    Microsoft.Win32.SafeHandles.SafeFileHandle handle,
    [<MarshalAs(UnmanagedType.U4)>] int dwFlags
);
