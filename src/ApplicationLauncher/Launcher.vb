Imports System.Runtime.InteropServices
Imports System.Runtime.ConstrainedExecution
Imports System.ComponentModel
Imports System.Security.Principal

<ComClass(Launcher.ClassId, Launcher.InterfaceId, Launcher.EventsId)> Public Class Launcher

#Region "COM GUIDs"
    ' These  GUIDs provide the COM identity for this class 
    ' and its COM interfaces. If you change them, existing 
    ' clients will no longer be able to access the class.
    Public Const ClassId As String = "74f8543f-3f45-4425-9516-223ae0909716"
    Public Const InterfaceId As String = "44615411-10b8-44f3-8bc8-ab573e4abca8"
    Public Const EventsId As String = "77185345-459c-48b6-90f2-5c52d3291b67"
#End Region

    Public Sub log(ByVal msg As String)
        Dim appName As String = "ApplicationLauncher"
        Dim eventData As EventSourceCreationData
        eventData = New EventSourceCreationData(appName, "Application")
        If Not EventLog.SourceExists(appName) Then
            EventLog.CreateEventSource(eventData)
        End If
        Dim eLog As New EventLog()
        eLog.Source = appName
        eLog.WriteEntry(msg, EventLogEntryType.Error)
    End Sub

    Public Function runprocess(ByVal executable As String, Optional ByVal singleinstance As Boolean = False) As Integer
        Dim running As Process() = Process.GetProcessesByName(executable.Substring(executable.LastIndexOf("\") + 1).Substring(0, executable.Substring(executable.LastIndexOf("\") + 1).LastIndexOf(".")))
        If running.Length = 0 Or Not singleinstance Then
            Try
                Return CreateProcess.createProcess(executable)
            Catch ex As Exception
                log(ex.Message)
                Return -1
            End Try
        End If
        Return 0
    End Function

    Public Sub New()
        MyBase.New()
    End Sub
End Class

Public Class CreateProcess
    Private Declare Auto Function CreateProcessAsUser Lib "advapi32" (ByVal hToken As IntPtr, ByVal lpApplicationName As String, ByVal lpCommandLine As String, ByRef lpProcessAttributes As SECURITY_ATTRIBUTES, ByRef lpThreadAttributes As SECURITY_ATTRIBUTES, ByVal bInheritHandles As Boolean, ByVal dwCreationFlags As Integer, ByVal lpEnvironment As IntPtr, ByVal lpCurrentDirectory As String, ByRef lpStartupInfo As STARTUPINFO, ByRef lpProcessInformation As PROCESS_INFORMATION) As Boolean
    Private Declare Auto Function DuplicateTokenEx Lib "advapi32.dll" (ByVal ExistingTokenHandle As IntPtr, ByVal dwDesiredAccess As UInt32, ByRef lpThreadAttributes As SECURITY_ATTRIBUTES, ByVal ImpersonationLevel As Integer, ByVal TokenType As Integer, ByRef DuplicateTokenHandle As System.IntPtr) As Boolean
    <DllImport("kernel32.dll", SetLastError:=True)> Private Shared Function CloseHandle(ByVal hHandle As IntPtr) As Boolean
    End Function
    Private Declare Auto Function RevertToSelf Lib "advapi32.dll" () As Long
    Private Declare Function OpenProcessToken Lib "advapi32.dll" (ByVal ProcessHandle As IntPtr, ByVal DesiredAccess As Integer, ByRef TokenHandle As IntPtr) As Boolean
    <DllImport("advapi32.dll", SetLastError:=True)> Private Shared Function LogonUser(ByVal lpszUsername As [String], ByVal lpszDomain As [String], ByVal lpszPassword As [String], ByVal dwLogonType As Integer, ByVal dwLogonProvider As Integer, ByRef phToken As IntPtr) As Boolean
    End Function
    <DllImport("user32.dll", SetLastError:=True)> Private Shared Function OpenInputDesktop(ByVal dwFlags As UInteger, ByVal fInherit As Boolean, ByVal dwDesiredAccess As UInteger) As IntPtr
    End Function
    <DllImport("user32.dll", SetLastError:=True)> Private Shared Function CloseDesktop(ByVal hDesktop As IntPtr) As Boolean
    End Function
    <DllImport("user32.dll", SetLastError:=True)> Private Shared Function SetThreadDesktop(ByVal hDesktop As IntPtr) As Boolean
    End Function

    <ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)> _
    <DllImport("user32", CharSet:=CharSet.Unicode, SetLastError:=True)> Private Shared Function GetProcessWindowStation() As IntPtr
    End Function

    <ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)>
    <DllImport("user32", CharSet:=CharSet.Unicode, SetLastError:=True)> Private Shared Function CloseWindowStation(ByVal hWinsta As IntPtr) As Boolean
    End Function
    <DllImport("user32.dll", SetLastError:=True)> Private Shared Function GetThreadDesktop(<MarshalAs(UnmanagedType.I4)> ByVal dwThreadId As UInteger) As IntPtr
    End Function
    <DllImport("kernel32.dll")> Private Shared Function GetCurrentThreadId() As UInteger
    End Function

    <DllImport("user32.dll", SetLastError:=True)> Private Shared Function SetProcessWindowStation(ByVal hWinSta As IntPtr) As Boolean
    End Function


    <StructLayout(LayoutKind.Sequential)>
    Private Structure STARTUPINFO
        Public cb As Int32
        Public lpReserved As String
        Public lpDesktop As String
        Public lpTitle As String
        Public dwX As Int32
        Public dwY As Int32
        Public dwXSize As Int32
        Public dwXCountChars As Int32
        Public dwYCountChars As Int32
        Public dwFillAttribute As Int32
        Public dwFlags As Int32
        Public wShowWindow As Int16
        Public cbReserved2 As Int16
        Public lpReserved2 As IntPtr
        Public hStdInput As IntPtr
        Public hStdOutput As IntPtr
        Public hStdError As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure SECURITY_ATTRIBUTES
        Public Length As Int32
        Public lpSecurityDescriptor As IntPtr
        Public bInheritHandle As Boolean
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure PROCESS_INFORMATION
        Public hProcess As IntPtr
        Public hThread As IntPtr
        Public dwProcessID As Int32
        Public dwThreadID As Int32
    End Structure

    Private Enum SECURITY_IMPERSONATION_LEVEL
        SecurityAnonymous
        SecurityIdentification
        SecurityImpersonation
        SecurityDelegation
    End Enum

    Private Enum TOKEN_TYPE
        TokenPrimary = 1
        TokenImpersonation
    End Enum

    <Flags()> Private Enum TokenPrivilege As UInteger
        STANDARD_RIGHTS_REQUIRED = &HF0000
        STANDARD_RIGHTS_READ = &H20000
        TOKEN_ASSIGN_PRIMARY = &H1
        TOKEN_DUPLICATE = &H2
        TOKEN_IMPERSONATE = &H4
        TOKEN_QUERY = &H8
        TOKEN_QUERY_SOURCE = &H10
        TOKEN_ADJUST_PRIVILEGES = &H20
        TOKEN_ADJUST_GROUPS = &H40
        TOKEN_ADJUST_DEFAULT = &H80
        TOKEN_ADJUST_SESSIONID = &H100
        TOKEN_READ = (STANDARD_RIGHTS_READ Or TOKEN_QUERY)
        TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED Or TOKEN_ASSIGN_PRIMARY Or TOKEN_DUPLICATE Or TOKEN_IMPERSONATE Or TOKEN_QUERY Or TOKEN_QUERY_SOURCE Or TOKEN_ADJUST_PRIVILEGES Or TOKEN_ADJUST_GROUPS Or TOKEN_ADJUST_DEFAULT Or TOKEN_ADJUST_SESSIONID)
    End Enum

    Private Const DESKTOP_CREATEMENU As UInteger = &H4
    Private Const DESKTOP_CREATEWINDOW As UInteger = &H2
    Private Const DESKTOP_ENUMERATE As UInteger = &H40
    Private Const DESKTOP_HOOKCONTROL As UInteger = &H8
    Private Const DESKTOP_JOURNALPLAYBACK As UInteger = &H20
    Private Const DESKTOP_JOURNALRECORD As UInteger = &H10
    Private Const DESKTOP_READOBJECTS As UInteger = &H1
    Private Const DESKTOP_SWITCHDESKTOP As UInteger = &H100
    Private Const DESKTOP_WRITEOBJECTS As UInteger = &H80
    Private Const GENERIC_ALL_ACCESS As UInteger = &H10000000

    Private Shared Function ImpersonateDesktopUser(ByVal proc As Process)
        Dim _userTokenHandle As IntPtr
        Dim hToken As IntPtr = IntPtr.Zero
        RevertToSelf()
        If OpenProcessToken(proc.Handle, TokenPrivilege.TOKEN_ALL_ACCESS, hToken) <> 0 Then
            Try
                Dim sa As SECURITY_ATTRIBUTES = New SECURITY_ATTRIBUTES()
                sa.Length = Marshal.SizeOf(sa)
                Dim result As Boolean = DuplicateTokenEx(hToken, GENERIC_ALL_ACCESS, sa, CType(SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, Integer), CType(TOKEN_TYPE.TokenPrimary, Integer), _userTokenHandle)
                If IntPtr.Zero = _userTokenHandle Then
                    Dim ex As Win32Exception = New Win32Exception(Marshal.GetLastWin32Error())
                    Throw New ApplicationException(String.Format("Can't duplicate the token for {0}:\n{1}", proc.ProcessName, ex.Message), ex)
                End If
            Catch ex As Exception
                EventLog.WriteEntry("ApplicationLauncher", ex.Message, EventLogEntryType.Error, 1, 1)
            Finally
                CloseHandle(hToken)
            End Try
        Else
            Dim s As String = String.Format("OpenProcess Failed {0}, privilege not held", Marshal.GetLastWin32Error())
            Throw New Exception(s)
        End If
        Return _userTokenHandle
    End Function

    Public Shared Function createProcess(ByVal app As String) As Integer
        Dim _userTokenHandle As IntPtr = IntPtr.Zero
        Try
            _userTokenHandle = ImpersonateDesktopUser(Process.GetProcessesByName("explorer")(0))
        Catch ex As Exception
            If _userTokenHandle = IntPtr.Zero Then
                _userTokenHandle = ImpersonateDesktopUser(Process.GetCurrentProcess())
            End If
        End Try
        Dim pi As PROCESS_INFORMATION = New PROCESS_INFORMATION()
        Try
            Dim sa As SECURITY_ATTRIBUTES = New SECURITY_ATTRIBUTES()
            sa.Length = Marshal.SizeOf(sa)
            Dim si As STARTUPINFO = New STARTUPINFO()
            si.cb = Marshal.SizeOf(si)
            si.lpDesktop = String.Empty
            If Not app Is Nothing And app.Length = 0 Then
                app = Nothing
            End If
            If Not CreateProcessAsUser(_userTokenHandle, app, Nothing, sa, sa, False, 0, IntPtr.Zero, "C:\", si, pi) Then
                Dim lasterror As Integer = Marshal.GetLastWin32Error()
                Dim ex As Win32Exception = New Win32Exception(lasterror)
                Dim message As String = String.Format("CreateProcessAsUser Error: {0}", ex.Message)
                Throw New ApplicationException(message, ex)
            End If
        Catch ex As Exception
            EventLog.WriteEntry("ApplicationLauncher", ex.Message, EventLogEntryType.Error, 1, 1)
            Throw
        Finally
            If pi.hProcess <> IntPtr.Zero Then
                CloseHandle(pi.hProcess)
            End If
            If pi.hThread <> IntPtr.Zero Then
                CloseHandle(pi.hThread)
            End If
        End Try
        Return pi.dwProcessID
    End Function
End Class
