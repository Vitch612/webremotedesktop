Imports System.Runtime.InteropServices
Imports System.Runtime.ConstrainedExecution
Imports System.ComponentModel
Imports System.Security.Principal
'Imports System.Diagnostics
'Imports System.Drawing
'Imports System.Windows.Forms
'Imports System.Drawing.Imaging
'Imports System.Drawing.Drawing2D
'Imports System.IO
'Imports System.Threading
'Imports System.Text
'Imports System.Web


<ComClass(Launcher.ClassId, Launcher.InterfaceId, Launcher.EventsId)> Public Class Launcher

#Region "COM GUIDs"
    ' These  GUIDs provide the COM identity for this class 
    ' and its COM interfaces. If you change them, existing 
    ' clients will no longer be able to access the class.
    Public Const ClassId As String = "74f8543f-3f45-4425-9516-223ae0909716"
    Public Const InterfaceId As String = "44615411-10b8-44f3-8bc8-ab573e4abca8"
    Public Const EventsId As String = "77185345-459c-48b6-90f2-5c52d3291b67"
#End Region

    Private Sub log(ByVal msg As String)
        Dim appName As String = "DesktopInteract"
        Dim eventData As EventSourceCreationData
        eventData = New EventSourceCreationData(appName, "Application")
        If Not EventLog.SourceExists(appName) Then
            EventLog.CreateEventSource(eventData)
        End If
        Dim eLog As New EventLog()
        eLog.Source = appName
        eLog.WriteEntry(msg, EventLogEntryType.Error)
    End Sub

    'Private Sub SaveScaledImage(ByVal path As String, ByVal OldImage As Image, ByVal newWidth As Integer, ByVal newHeight As Integer, ByVal quality As Long)
    '    Dim originalWidth As Integer = OldImage.Width
    '    Dim originalHeight As Integer = OldImage.Height
    '    Dim percentWidth As Single = newWidth / originalWidth
    '    Dim percentHeight As Single = newHeight / originalHeight
    '    Dim percent As Single = If(percentHeight < percentWidth, percentHeight, percentWidth)
    '    newWidth = CInt(originalWidth * percent)
    '    newHeight = CInt(originalHeight * percent)
    '    Dim newImage As Image = New Bitmap(newWidth, newHeight)
    '    Using graphicsHandle As Graphics = Graphics.FromImage(newImage)
    '        graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBilinear
    '        graphicsHandle.DrawImage(OldImage, 0, 0, newWidth, newHeight)
    '    End Using
    '    Dim myEncoder As Imaging.Encoder
    '    Dim myEncoderParameter As EncoderParameter
    '    Dim myEncoderParameters As EncoderParameters
    '    myEncoder = Imaging.Encoder.Quality
    '    myEncoderParameters = New EncoderParameters(1)
    '    myEncoderParameter = New EncoderParameter(myEncoder, CType(quality, Int32))
    '    myEncoderParameters.Param(0) = myEncoderParameter
    '    Try
    '        newImage.Save(path, GetEncoderInfo(ImageFormat.Jpeg), myEncoderParameters)
    '        newImage.Dispose()
    '    Catch ex As Exception
    '    End Try
    'End Sub

    'Public Sub sendText(ByVal text As String)
    '    CreateProcess.impersonate()
    '    ScreenCapture.User32.sendkeys(text)
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Sub sendKey(ByVal vk As Byte)
    '    CreateProcess.impersonate()
    '    ScreenCapture.User32.sendkey(vk)
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Function getscreen_resolution() As Integer()
    '    CreateProcess.impersonate()
    '    Dim intX As Integer = Screen.PrimaryScreen.Bounds.Width
    '    Dim intY As Integer = Screen.PrimaryScreen.Bounds.Height
    '    Dim retval(1) As Integer
    '    retval(0) = intX
    '    retval(1) = intY
    '    CreateProcess.releaseimpersonation()
    '    Return retval
    'End Function

    'Public Sub sendkeybevent(ByVal key)
    '    CreateProcess.impersonate()
    '    ScreenCapture.User32.keybd_event(CType(key, Byte), 0, 0, 0)
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Sub mute()
    '    CreateProcess.impersonate()
    '    ScreenCapture.User32.keybd_event(CType(Keys.VolumeMute, Byte), 0, 0, 0)
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Sub volumeup()
    '    CreateProcess.impersonate()
    '    ScreenCapture.User32.keybd_event(CType(Keys.VolumeUp, Byte), 0, 0, 0)
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Sub volumedown()
    '    CreateProcess.impersonate()
    '    ScreenCapture.User32.keybd_event(CType(Keys.VolumeDown, Byte), 0, 0, 0)
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Function getmousepos() As Integer()
    '    CreateProcess.impersonate()
    '    Dim p As Point
    '    Dim R As Long = ScreenCapture.User32.GetCursorPos(p)
    '    Dim retval(1) As Integer
    '    retval(0) = p.X
    '    retval(1) = p.Y
    '    CreateProcess.releaseimpersonation()
    '    Return retval
    'End Function


    'Public Sub mousemove(ByVal x, ByVal y)
    '    CreateProcess.impersonate()
    '    Dim p As System.Drawing.Point = New System.Drawing.Point(x, y)
    '    ScreenCapture.User32.MoveMouse(p)
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Sub mouseclickl(ByVal x, ByVal y)
    '    CreateProcess.impersonate()
    '    mousemove(x, y)
    '    ScreenCapture.User32.SendClick()
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Sub mousedblclickl(ByVal x, ByVal y)
    '    CreateProcess.impersonate()
    '    mousemove(x, y)
    '    ScreenCapture.User32.SendClick()
    '    ScreenCapture.User32.SendClick()
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Sub mouseclickr(ByVal x, ByVal y)
    '    CreateProcess.impersonate()
    '    mousemove(x, y)
    '    ScreenCapture.User32.SendRightClick()
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Sub mousedblclickr(ByVal x, ByVal y)
    '    CreateProcess.impersonate()
    '    mousemove(x, y)
    '    ScreenCapture.User32.SendRightClick()
    '    ScreenCapture.User32.SendRightClick()
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Sub mousedownat(ByVal x, ByVal y)
    '    CreateProcess.impersonate()
    '    mousemove(x, y)
    '    ScreenCapture.User32.SendDown()
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Sub mouseupat(ByVal x, ByVal y)
    '    CreateProcess.impersonate()
    '    mousemove(x, y)
    '    ScreenCapture.User32.SendUp()
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Sub mousedown()
    '    CreateProcess.impersonate()
    '    ScreenCapture.User32.SendDown()
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Sub mouseup()
    '    CreateProcess.impersonate()
    '    ScreenCapture.User32.SendUp()
    '    CreateProcess.releaseimpersonation()
    'End Sub

    'Public Function grab(ByVal location As String, ByVal width As Integer, ByVal height As Integer, ByVal quality As Long) As Boolean
    '    CreateProcess.impersonate()
    '    Dim fullpath As String = "screenshot.jpeg"
    '    Dim path As String = "."
    '    If location.Length > 0 Then
    '        path = location
    '    End If
    '    If Directory.Exists(path) Then
    '        If path.Substring(path.Length - 1).Equals("\\") Or path.Substring(path.Length - 1).Equals("/") Then
    '            fullpath = path & fullpath
    '        Else
    '            fullpath = path & "/" & fullpath
    '        End If
    '    Else
    '        fullpath = path
    '    End If
    '    Dim img As Image
    '    Try
    '        Dim scap As ScreenCapture = New ScreenCapture()
    '        img = scap.CaptureScreen()
    '        SaveScaledImage(fullpath, img, width, height, quality)
    '        img.Dispose()
    '    Catch ex As Exception
    '        CreateProcess.releaseimpersonation()
    '        Return False
    '    End Try
    '    CreateProcess.releaseimpersonation()
    '    Return True
    'End Function

    'Private Shared Function GetEncoderInfo(ByVal format As ImageFormat) As ImageCodecInfo
    '    Dim j As Integer
    '    Dim encoders() As ImageCodecInfo
    '    encoders = ImageCodecInfo.GetImageEncoders()
    '    j = 0
    '    While j < encoders.Length
    '        If encoders(j).FormatID = format.Guid Then
    '            Return encoders(j)
    '        End If
    '        j += 1
    '    End While
    '    Return Nothing
    'End Function

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

    Public Sub test()
        CreateProcess.test()
    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
        'CreateProcess.closedesktop()
    End Sub

    Public Sub New()
        MyBase.New()
        'CreateProcess.opendesktop()
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
            Finally
                CloseHandle(hToken)
            End Try
        Else
            Dim s As String = String.Format("OpenProcess Failed {0}, privilege not held", Marshal.GetLastWin32Error())
            Throw New Exception(s)
        End If
        Return _userTokenHandle
    End Function

    Shared _desktop As IntPtr = IntPtr.Zero
    Shared _userToken As IntPtr = IntPtr.Zero
    Shared _impersonatedUser As WindowsImpersonationContext

    Public Shared Sub test()
        Dim windowhandle As IntPtr = IntPtr.Zero
        windowhandle = GetProcessWindowStation()
        Dim tid As UInteger = GetCurrentThreadId()
        Dim tdsk As IntPtr = IntPtr.Zero
        tdsk = GetThreadDesktop(tid)
        EventLog.WriteEntry("DesktopInteract", "window: " & windowhandle.ToString & " tid: " & tid & " dsk: " & tdsk.ToString(), EventLogEntryType.Information, 1, 1)
    End Sub


    Public Shared Sub impersonate()
        Return
        Try
            _userToken = ImpersonateDesktopUser(Process.GetProcessesByName("explorer")(0))
            Dim newId As New WindowsIdentity(_userToken)
            _impersonatedUser = newId.Impersonate()
            opendesktop()
            If Not SetThreadDesktop(_desktop) Then
                EventLog.WriteEntry("DesktopInteract", Marshal.GetLastWin32Error(), EventLogEntryType.Warning, 1, 1)
            End If
        Catch ex As Exception
            EventLog.WriteEntry("DesktopInteract", ex.Message, EventLogEntryType.Error, 1, 1)
        End Try
    End Sub

    Public Shared Sub releaseimpersonation()
        Return
        CloseHandle(_userToken)
        _impersonatedUser.Dispose()
        CloseDesktop()
    End Sub


    Public Shared Function opendesktop() As Boolean
        Dim GENALL As UInteger = DESKTOP_CREATEMENU Or DESKTOP_CREATEWINDOW Or DESKTOP_ENUMERATE Or DESKTOP_HOOKCONTROL Or DESKTOP_JOURNALPLAYBACK Or DESKTOP_JOURNALRECORD Or DESKTOP_READOBJECTS Or DESKTOP_SWITCHDESKTOP Or DESKTOP_WRITEOBJECTS
        _desktop = OpenInputDesktop(0, False, GENALL)
        If _desktop = IntPtr.Zero Then
            EventLog.WriteEntry("DesktopInteract", Marshal.GetLastWin32Error(), EventLogEntryType.Warning, 1, 1)
            Return False
        End If
        Return True
    End Function

    Public Shared Sub closedesktop()
        If _desktop <> IntPtr.Zero Then
            CloseDesktop(_desktop)
        End If
    End Sub

    Public Shared Function createProcess(ByVal app As String) As Integer
        Dim _userTokenHandle As IntPtr = IntPtr.Zero
        Try
            _userTokenHandle = ImpersonateDesktopUser(Process.GetProcessesByName("explorer")(0))
        Catch ex As Exception
            'Dim GENALL As UInteger = DESKTOP_CREATEMENU Or DESKTOP_CREATEWINDOW Or DESKTOP_ENUMERATE Or DESKTOP_HOOKCONTROL Or DESKTOP_JOURNALPLAYBACK Or DESKTOP_JOURNALRECORD Or DESKTOP_READOBJECTS Or DESKTOP_SWITCHDESKTOP Or DESKTOP_WRITEOBJECTS
            'EventLog.WriteEntry("DesktopInteract", GENALL, EventLogEntryType.Information, 1, 1)
            'LogonUser("Alain", Nothing, Nothing, 2, 0, _userTokenHandle)
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
            EventLog.WriteEntry("DesktopInteract", ex.Message, EventLogEntryType.Error, 1, 1)
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

'Public Class ScreenCapture
'    Public Function CaptureScreen() As Image
'        Return CaptureWindow(User32.GetDesktopWindow())
'    End Function

'    Public Function CaptureWindow(ByVal handle As IntPtr) As Image
'        Dim hdcSrc As IntPtr = User32.GetWindowDC(handle)
'        Dim windowRect As New User32.RECT()
'        User32.GetWindowRect(handle, windowRect)
'        Dim width As Integer = windowRect.right - windowRect.left
'        Dim height As Integer = windowRect.bottom - windowRect.top
'        Dim hdcDest As IntPtr = GDI32.CreateCompatibleDC(hdcSrc)
'        Dim hBitmap As IntPtr = GDI32.CreateCompatibleBitmap(hdcSrc, width, height)
'        Dim hOld As IntPtr = GDI32.SelectObject(hdcDest, hBitmap)
'        GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY)
'        GDI32.SelectObject(hdcDest, hOld)
'        GDI32.DeleteDC(hdcDest)
'        User32.ReleaseDC(handle, hdcSrc)
'        Dim img As Image = Image.FromHbitmap(hBitmap)
'        GDI32.DeleteObject(hBitmap)
'        Return img
'    End Function

'    Public Sub CaptureWindowToFile(ByVal handle As IntPtr, ByVal filename As String, ByVal format As ImageFormat)
'        Dim img As Image = CaptureWindow(handle)
'        img.Save(filename, format)
'    End Sub

'    Public Sub CaptureScreenToFile(ByVal filename As String, ByVal format As ImageFormat)
'        Dim img As Image = CaptureScreen()
'        img.Save(filename, format)
'    End Sub

'    Private Class GDI32
'        Public Shared SRCCOPY As Integer = &HCC0020
'        Public Declare Function BitBlt Lib "gdi32.dll" (ByVal hObject As IntPtr, ByVal nXDest As Integer, ByVal nYDest As Integer, ByVal nWidth As Integer, ByVal nHeight As Integer, ByVal hObjectSource As IntPtr, ByVal nXSrc As Integer, ByVal nYSrc As Integer, ByVal dwRop As Integer) As Boolean
'        Public Declare Function CreateCompatibleBitmap Lib "gdi32.dll" (ByVal hDC As IntPtr, ByVal nWidth As Integer, ByVal nHeight As Integer) As IntPtr
'        Public Declare Function CreateCompatibleDC Lib "gdi32.dll" (ByVal hDC As IntPtr) As IntPtr
'        Public Declare Function DeleteDC Lib "gdi32.dll" (ByVal hDC As IntPtr) As Boolean
'        Public Declare Function DeleteObject Lib "gdi32.dll" (ByVal hObject As IntPtr) As Boolean
'        Public Declare Function SelectObject Lib "gdi32.dll" (ByVal hDC As IntPtr, ByVal hObject As IntPtr) As IntPtr
'    End Class

'    Public Class User32
'        <StructLayout(LayoutKind.Sequential)> Public Structure RECT
'            Public left As Integer
'            Public top As Integer
'            Public right As Integer
'            Public bottom As Integer
'        End Structure
'        Public Declare Function GetDesktopWindow Lib "user32.dll" () As IntPtr
'        Public Declare Function GetWindowDC Lib "user32.dll" (ByVal hWnd As IntPtr) As IntPtr
'        Public Declare Function ReleaseDC Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal hDC As IntPtr) As IntPtr
'        Public Declare Function GetWindowRect Lib "user32.dll" (ByVal hWnd As IntPtr, ByRef rect As RECT) As IntPtr
'        Public Declare Sub mouse_event Lib "user32.dll" (ByVal dwFlags As UInt32, ByVal dx As UInt32, ByVal dy As UInt32, ByVal dwData As UInt32, ByVal dwExtraInfo As IntPtr)
'        Public Declare Auto Function GetCursorPos Lib "User32.dll" (ByRef lpPoint As Point) As Long
'        Public Declare Sub keybd_event Lib "user32.dll" (ByVal bVk As Byte, ByVal bScan As Byte, ByVal dwFlags As UInteger, ByVal dwExtraInfo As Integer)
'        Public Declare Function GetMessageExtraInfo Lib "user32.dll" () As IntPtr
'        Public Declare Function GetForegroundWindow Lib "user32" () As IntPtr
'        Public Declare Auto Function GetWindowThreadProcessId Lib "user32.dll" (ByVal hwnd As IntPtr, ByRef lpdwProcessId As Integer) As Integer
'        Public Declare Function GetKeyboardLayout Lib "user32.dll" (ByVal idThread As Integer) As IntPtr
'        <DllImport("user32.dll")> Private Shared Function VkKeyScanEx(ByVal ch As Char, ByVal dwhkl As IntPtr) As Short
'        End Function
'        <DllImport("user32.dll", CharSet:=CharSet.Unicode)> Private Shared Function VkKeyScanW(ByVal ch As Char) As Short
'        End Function
'        Public Declare Function MapVirtualKeyW Lib "user32.dll" (ByVal uCode As UInteger, ByVal uMapType As UInteger) As UInteger
'        Public Declare Function MapVirtualKeyExW Lib "user32.dll" (ByVal uCode As UInteger, ByVal uMapType As UInteger, ByVal dwhkl As IntPtr) As UInteger

'        Public Const KEYEVENTF_KEYUP As UInt32 = &H2

'        Public Shared Sub sendkey(ByVal vk As Byte)
'            Dim _fwid As IntPtr = GetForegroundWindow()
'            Dim _dwtid As Integer = GetWindowThreadProcessId(_fwid, Nothing)
'            Dim _hkl As IntPtr = GetKeyboardLayout(_dwtid)
'            Dim bScan As Byte = MapVirtualKeyExW(vk, 4, _hkl)
'            If (bScan) <> 0 Then
'                keybd_event(vk, bScan, 0, IntPtr.Zero)
'                keybd_event(vk, bScan, KEYEVENTF_KEYUP, IntPtr.Zero)
'            End If
'        End Sub

'        Public Shared Sub sendkeys(ByVal s As String)
'            Dim _fwid As IntPtr = GetForegroundWindow()
'            Dim _dwtid As Integer = GetWindowThreadProcessId(_fwid, Nothing)
'            Dim _hkl As IntPtr = GetKeyboardLayout(_dwtid)
'            For Each c As Char In s
'                Dim exvkcode As Short = VkKeyScanEx(c, _hkl)
'                Dim bVk As Byte = exvkcode And &HFF
'                Dim dwFlags As UInteger = (exvkcode And &HFF00) >> 8
'                Dim bScan As Byte = MapVirtualKeyExW(bVk, 4, _hkl)
'                If (bScan) <> 0 Then
'                    If dwFlags = 1 Then
'                        keybd_event(16, 42, 0, IntPtr.Zero)
'                    End If
'                    keybd_event(bVk, bScan, 0, IntPtr.Zero)
'                    keybd_event(bVk, bScan, KEYEVENTF_KEYUP, IntPtr.Zero)
'                    If dwFlags = 1 Then
'                        keybd_event(16, 42, KEYEVENTF_KEYUP, IntPtr.Zero)
'                    End If
'                End If
'            Next
'        End Sub

'        Public Shared Sub SendClick()
'            SendDown()
'            SendUp()
'        End Sub

'        Public Shared Sub SendRightClick()
'            SendDownRight()
'            SendUpRight()
'        End Sub


'        Public Shared Sub SendUp()
'            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, New System.IntPtr())
'        End Sub

'        Public Shared Sub SendDown()
'            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, New System.IntPtr())
'        End Sub

'        Public Shared Sub SendUpRight()
'            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, New System.IntPtr())
'        End Sub

'        Public Shared Sub SendDownRight()
'            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, New System.IntPtr())
'        End Sub

'        Public Shared Sub MoveMouse(ByVal p As System.Drawing.Point)
'            System.Windows.Forms.Cursor.Position = New System.Drawing.Point(CType(p.X, Integer), CType(p.Y, Integer))
'        End Sub

'        Private Shared APPCOMMAND_VOLUME_MUTE As Integer = &HE80000
'        Private Shared APPCOMMAND_VOLUME_UP As Integer = &HEA0000
'        Private Shared APPCOMMAND_VOLUME_DOWN As Integer = &HE90000
'        Private Shared WM_APPCOMMAND As Integer = &HE319

'        Private Shared MOUSEEVENTF_LEFTDOWN As UInt32 = &HE0002
'        Private Shared MOUSEEVENTF_LEFTUP As UInt32 = &HE0004
'        Private Shared MOUSEEVENTF_RIGHTDOWN As UInt32 = &HE08
'        Private Shared MOUSEEVENTF_RIGHTUP As UInt32 = &HE10
'        Private Shared MOUSEEVENTF_MIDDLEDOWN As UInt32 = &HE0020
'        Private Shared MOUSEEVENTF_MIDDLEUP As UInt32 = &HE0040
'        Private Shared VK_MBUTTON As UInt16 = &HE04
'        Private Shared VK_LBUTTON As UInt16 = &HE01
'        Private Shared VK_RBUTTON As UInt16 = &HE02
'    End Class
'End Class