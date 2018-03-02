Imports System.Runtime.InteropServices
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Threading
Imports System.Diagnostics
Imports System.Net.Sockets
Imports System.Net
Imports System.ComponentModel
Imports System.Text
Imports System.Web


Module Module1
    Private Sub log(ByVal msg As String, Optional ByVal type As EventLogEntryType = EventLogEntryType.Error)
        Dim appName As String = "DesktopInteractServer"
        Dim eventData As EventSourceCreationData
        eventData = New EventSourceCreationData(appName, "Application")
        If Not EventLog.SourceExists(appName) Then
            EventLog.CreateEventSource(eventData)
        End If
        Dim eLog As New EventLog()
        eLog.Source = appName
        eLog.WriteEntry(msg, type)
    End Sub

    Private Class tcpserver
        Private idletime As Byte = 5
        Private timer As DateTime
        Private _TCPListener As TcpListener
        Private _trd As Thread
        Private _ttrd As Thread

        Private Sub subThread(ByVal _TCPClient As TcpClient)
            Try
                timer = Now
                _TCPClient.Client.ReceiveTimeout = 10
                Dim stream As NetworkStream = _TCPClient.GetStream()
                Dim bytes(128) As Byte
                Dim datastring As String = ""
                Dim i As Int32
                Do
                    i = stream.Read(bytes, 0, bytes.Length)
                    datastring &= System.Text.Encoding.ASCII.GetString(bytes, 0, i)
                    If stream.DataAvailable Then
                        i = stream.Read(bytes, 0, bytes.Length)
                    Else
                        i = 0
                    End If
                Loop While i > 0

                Dim operation As String = datastring.Substring(0, 5)
                Dim parameters As String()
                If operation.Equals("txtsd") Then
                    parameters = {datastring.Substring(5)}
                Else
                    parameters = datastring.Substring(5).Split(",")
                End If
                Dim reply As String
                Select Case operation
                    Case "txtsd"
                        Functionality.sendText(parameters(0))
                    Case "backs"
                        Functionality.sendKey(8)
                    Case "mousd"
                        Functionality.mousemove(Convert.ToInt32(parameters(0)), Convert.ToInt32(parameters(1)))
                        Functionality.mousedown()
                    Case "mousu"
                        Functionality.mousemove(Convert.ToInt32(parameters(0)), Convert.ToInt32(parameters(1)))
                        Functionality.mouseup()
                    Case "mousm"
                        Functionality.mousemove(Convert.ToInt32(parameters(0)), Convert.ToInt32(parameters(1)))
                    Case "gscre"
                        If Functionality.grab(parameters(0), Convert.ToInt32(parameters(1)), Convert.ToInt32(parameters(2)), CType(Convert.ToInt32(parameters(3)), Long)) Then
                            reply = "1"
                        Else
                            reply = "0"
                        End If
                    Case "gscrr"
                        Dim retval As Integer() = Functionality.getscreen_resolution()
                        reply = retval(0) & "," & retval(1)
                    Case "gmpos"
                        Dim retval As Integer() = Functionality.getmousepos()
                        reply = retval(0) & "," & retval(1)
                    Case "smcll"
                        Functionality.mouseclickl(Convert.ToInt32(parameters(0)), Convert.ToInt32(parameters(1)))
                    Case "smclr"
                        Functionality.mouseclickr(Convert.ToInt32(parameters(0)), Convert.ToInt32(parameters(1)))
                    Case "smdcl"
                        Functionality.mousedblclickl(Convert.ToInt32(parameters(0)), Convert.ToInt32(parameters(1)))
                    Case "smdcr"
                        Functionality.mousedblclickr(Convert.ToInt32(parameters(0)), Convert.ToInt32(parameters(1)))
                    Case "svmut"
                        Functionality.mute()
                    Case "svodo"
                        Functionality.volumedown()
                    Case "svoup"
                        Functionality.volumeup()
                End Select
                If Not reply Is Nothing Then
                    Dim msg As Byte() = System.Text.Encoding.ASCII.GetBytes(reply)
                    stream.Write(msg, 0, msg.Length)
                End If
                _TCPClient.Close()
            Catch ex As Exception
                log(ex.Message)
            End Try
        End Sub

        Private Sub timerThread()
            Do
                Dim currentDate As DateTime = Now
                If currentDate.Subtract(timer).TotalSeconds > idletime Then
                    _TCPListener.Stop()
                    _trd.Abort()
                    _ttrd.Abort()
                End If
                Thread.Sleep(500)
            Loop
        End Sub

        Private Sub ThreadTask()
            timer = Now
            _ttrd = New Thread(AddressOf timerThread)
            _ttrd.Start()
            Try
                Dim localPort As Int32 = 8888
                Dim localAddr As IPAddress = IPAddress.Parse("127.0.0.1")
                _TCPListener = New TcpListener(localAddr, localPort)
                _TCPListener.Start()
                Do
                    Dim _tcpClient As TcpClient = _TCPListener.AcceptTcpClient()
                    Dim trd As Thread = New Thread(AddressOf subThread)
                    trd.IsBackground = False
                    trd.Start(_tcpClient)
                Loop
                _TCPListener.Stop()
            Catch ex As Exception
                log(ex.Message)
            End Try
        End Sub

        Public Sub New()
            MyBase.New()
            _trd = New Thread(AddressOf ThreadTask)
            _trd.IsBackground = False
            _trd.Start()
        End Sub
    End Class

    Private Class httpserver
        Public _httpListener As HttpListener = New HttpListener()
        Private Sub ThreadTask()
            _httpListener.Prefixes.Add("http://localhost:8000/")
            _httpListener.Start()
            Do
                Dim context As HttpListenerContext = _httpListener.GetContext()
                Dim responsetext As String = "<html><body>" & _
                "url: " & context.Request.Url.ToString() & _
                "<BR>useragent: " & context.Request.UserAgent.ToString() & _
                "<BR>keepalive: " & context.Request.KeepAlive & _
                "<BR>Method: " & context.Request.HttpMethod & _
                "<BR>IP: " & context.Request.RemoteEndPoint.ToString() & _
                "<BR>bodyencoding: " & context.Request.ContentEncoding.ToString() & _
                "<BR>bodylength: " & context.Request.ContentLength64 & _
                "<BR>protocol: " & context.Request.ProtocolVersion.ToString() & _
                "<BR>hostname: " & context.Request.UserHostName
                For Each s As String In context.Request.QueryString.AllKeys
                    responsetext &= "<BR>" & s & "=" & context.Request.QueryString(s)
                Next
                If context.Request.HasEntityBody Then
                    Dim reader As StreamReader = New StreamReader(context.Request.InputStream, context.Request.ContentEncoding)
                    responsetext &= "<HR>" & reader.ReadToEnd() & "<HR>"
                End If
                responsetext &= "<HR><form action='receive' method='post' enctype='multipart/form-data'><input name='say' id='say' value='Hi'><BR><input type='password' name='password'><button type='submit'>Send</button></form><HR>"
                responsetext &= "</body></html>"
                Dim response As HttpListenerResponse = context.Response
                Dim buffer As Byte() = System.Text.Encoding.UTF8.GetBytes(responsetext)
                response.ContentLength64 = buffer.Length
                Dim outstream As Stream = response.OutputStream()
                outstream.Write(buffer, 0, buffer.Length)
                outstream.Close()
            Loop
            _httpListener.Stop()
        End Sub

        Public trd As Thread

        Public Sub New()
            MyBase.New()
            trd = New Thread(AddressOf ThreadTask)
            trd.IsBackground = False
            trd.Start()
        End Sub
    End Class

    Sub Main()
        Dim server As tcpserver = New tcpserver()
        'Dim webserver As httpserver = New httpserver()
        'Console.ReadKey()
        'webserver._httpListener.Stop()
        'webserver._httpListener.Close()
        'webserver.trd.Abort()
    End Sub

    Private Class Functionality
        Private Shared Sub SaveScaledImage(ByVal path As String, ByVal OldImage As Image, ByVal newWidth As Integer, ByVal newHeight As Integer, ByVal quality As Long)
            Dim originalWidth As Integer = OldImage.Width
            Dim originalHeight As Integer = OldImage.Height
            Dim percentWidth As Single = newWidth / originalWidth
            Dim percentHeight As Single = newHeight / originalHeight
            Dim percent As Single = If(percentHeight < percentWidth, percentHeight, percentWidth)
            newWidth = CInt(originalWidth * percent)
            newHeight = CInt(originalHeight * percent)
            Dim newImage As Image = New Bitmap(newWidth, newHeight)
            Using graphicsHandle As Graphics = Graphics.FromImage(newImage)
                graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBilinear
                graphicsHandle.DrawImage(OldImage, 0, 0, newWidth, newHeight)
            End Using
            Dim myEncoder As System.Drawing.Imaging.Encoder
            Dim myEncoderParameter As EncoderParameter
            Dim myEncoderParameters As EncoderParameters
            myEncoder = System.Drawing.Imaging.Encoder.Quality
            myEncoderParameters = New EncoderParameters(1)
            myEncoderParameter = New EncoderParameter(myEncoder, CType(quality, Int32))
            myEncoderParameters.Param(0) = myEncoderParameter
            Try
                newImage.Save(path, GetEncoderInfo(ImageFormat.Jpeg), myEncoderParameters)
                newImage.Dispose()
            Catch ex As Exception
                log(ex.Message)
            End Try
        End Sub

        Public Shared Sub sendText(ByVal text As String)
            ScreenCapture.User32.sendkeys(text)
        End Sub

        Public Shared Sub sendKey(ByVal vk As Byte)
            ScreenCapture.User32.sendkey(vk)
        End Sub

        Public Shared Function getscreen_resolution() As Integer()
            Dim intX As Integer = Screen.PrimaryScreen.Bounds.Width
            Dim intY As Integer = Screen.PrimaryScreen.Bounds.Height
            Dim retval(1) As Integer
            retval(0) = intX
            retval(1) = intY
            Return retval
        End Function

        Public Shared Sub sendkeybevent(ByVal key)
            ScreenCapture.User32.keybd_event(CType(key, Byte), 0, 0, 0)
        End Sub

        Public Shared Sub mute()
            ScreenCapture.User32.keybd_event(CType(Keys.VolumeMute, Byte), 0, 0, 0)
        End Sub

        Public Shared Sub volumeup()
            ScreenCapture.User32.keybd_event(CType(Keys.VolumeUp, Byte), 0, 0, 0)
        End Sub

        Public Shared Sub volumedown()
            ScreenCapture.User32.keybd_event(CType(Keys.VolumeDown, Byte), 0, 0, 0)
        End Sub

        Public Shared Function getmousepos() As Integer()
            Dim p As Point
            Dim R As Long = ScreenCapture.User32.GetCursorPos(p)
            Dim retval(1) As Integer
            retval(0) = p.X
            retval(1) = p.Y
            Return retval
        End Function

        Public Shared Sub mousedown()
            ScreenCapture.User32.SendDown()
        End Sub

        Public Shared Sub mouseup()
            ScreenCapture.User32.SendUp()
        End Sub

        Public Shared Sub mousemove(ByVal x, ByVal y)
            Dim p As System.Drawing.Point = New System.Drawing.Point(x, y)
            ScreenCapture.User32.MoveMouse(p)
        End Sub

        Public Shared Sub mouseclickl(ByVal x, ByVal y)
            mousemove(x, y)
            ScreenCapture.User32.SendClick()
        End Sub

        Public Shared Sub mousedblclickl(ByVal x, ByVal y)
            mousemove(x, y)
            ScreenCapture.User32.SendClick()
            ScreenCapture.User32.SendClick()
        End Sub

        Public Shared Sub mouseclickr(ByVal x, ByVal y)
            mousemove(x, y)
            ScreenCapture.User32.SendRightClick()
        End Sub

        Public Shared Sub mousedblclickr(ByVal x, ByVal y)
            mousemove(x, y)
            ScreenCapture.User32.SendRightClick()
            ScreenCapture.User32.SendRightClick()
        End Sub

        Public Shared Function grab(ByVal location As String, ByVal width As Integer, ByVal height As Integer, ByVal quality As Long) As Boolean
            Dim fullpath As String = "screenshot.jpeg"
            Dim path As String = "."
            If location.Length > 0 Then
                path = location
            End If
            If Directory.Exists(path) Then
                If path.Substring(path.Length - 1).Equals("\\") Or path.Substring(path.Length - 1).Equals("/") Then
                    fullpath = path & fullpath
                Else
                    fullpath = path & "/" & fullpath
                End If
            Else
                fullpath = path
            End If
            Try
                Dim scap As ScreenCapture = New ScreenCapture()
                Dim img As Image = scap.CaptureScreen()
                SaveScaledImage(fullpath, img, width, height, quality)
                img.Dispose()
                Return True
            Catch ex As Exception
                log(ex.InnerException.ToString)
            End Try
            Return False
        End Function

        Private Shared Function GetEncoderInfo(ByVal format As ImageFormat) As ImageCodecInfo
            Dim j As Integer
            Dim encoders() As ImageCodecInfo
            encoders = ImageCodecInfo.GetImageEncoders()
            j = 0
            While j < encoders.Length
                If encoders(j).FormatID = format.Guid Then
                    Return encoders(j)
                End If
                j += 1
            End While
            Return Nothing
        End Function

        Public Sub New()
            MyBase.New()
        End Sub
    End Class

    Private Class ScreenCapture
        Public Function CaptureScreen() As Image
            Return CaptureWindow(User32.GetDesktopWindow())
        End Function

        Public Function CaptureWindow(ByVal handle As IntPtr) As Image
            Dim hdcSrc As IntPtr = User32.GetWindowDC(handle)
            Dim windowRect As New User32.RECT()
            User32.GetWindowRect(handle, windowRect)
            Dim width As Integer = windowRect.right - windowRect.left
            Dim height As Integer = windowRect.bottom - windowRect.top
            Dim hdcDest As IntPtr = GDI32.CreateCompatibleDC(hdcSrc)
            Dim hBitmap As IntPtr = GDI32.CreateCompatibleBitmap(hdcSrc, width, height)
            Dim hOld As IntPtr = GDI32.SelectObject(hdcDest, hBitmap)
            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY)
            GDI32.SelectObject(hdcDest, hOld)
            GDI32.DeleteDC(hdcDest)
            User32.ReleaseDC(handle, hdcSrc)
            Dim img As Image = Image.FromHbitmap(hBitmap)
            GDI32.DeleteObject(hBitmap)
            Return img
        End Function

        Public Sub CaptureWindowToFile(ByVal handle As IntPtr, ByVal filename As String, ByVal format As ImageFormat)
            Dim img As Image = CaptureWindow(handle)
            img.Save(filename, format)
        End Sub

        Public Sub CaptureScreenToFile(ByVal filename As String, ByVal format As ImageFormat)
            Dim img As Image = CaptureScreen()
            img.Save(filename, format)
        End Sub

        Private Class GDI32
            Public Shared SRCCOPY As Integer = &HCC0020
            Public Declare Function BitBlt Lib "gdi32.dll" (ByVal hObject As IntPtr, ByVal nXDest As Integer, ByVal nYDest As Integer, ByVal nWidth As Integer, ByVal nHeight As Integer, ByVal hObjectSource As IntPtr, ByVal nXSrc As Integer, ByVal nYSrc As Integer, ByVal dwRop As Integer) As Boolean
            Public Declare Function CreateCompatibleBitmap Lib "gdi32.dll" (ByVal hDC As IntPtr, ByVal nWidth As Integer, ByVal nHeight As Integer) As IntPtr
            Public Declare Function CreateCompatibleDC Lib "gdi32.dll" (ByVal hDC As IntPtr) As IntPtr
            Public Declare Function DeleteDC Lib "gdi32.dll" (ByVal hDC As IntPtr) As Boolean
            Public Declare Function DeleteObject Lib "gdi32.dll" (ByVal hObject As IntPtr) As Boolean
            Public Declare Function SelectObject Lib "gdi32.dll" (ByVal hDC As IntPtr, ByVal hObject As IntPtr) As IntPtr
        End Class

        Public Class User32
            <StructLayout(LayoutKind.Sequential)> Public Structure RECT
                Public left As Integer
                Public top As Integer
                Public right As Integer
                Public bottom As Integer
            End Structure


            Public Const KEYEVENTF_KEYUP As UInt32 = &H2
            'Const KEYEVENTF_UNICODE As UInt32 = &H4
            'Const KEYEVENTF_EXTENDEDKEY As UInt32 = &H1

            '<StructLayout(LayoutKind.Explicit)> _
            'Friend Structure INPUT
            '    <FieldOffset(0)> Public dwType As InputType
            '    <FieldOffset(4)> Public mi As MOUSEINPUT
            '    <FieldOffset(4)> Public ki As KEYBDINPUT
            '    <FieldOffset(4)> Public hi As HARDWAREINPUT
            'End Structure

            '<StructLayout(LayoutKind.Sequential)> _
            'Friend Structure MOUSEINPUT
            '    Public dx As Int32
            '    Public dy As Int32
            '    Public mouseData As UInt32
            '    Public dwFlags As MOUSEEVENTF
            '    Public time As UInt32
            '    Public dwExtraInfo As IntPtr
            'End Structure

            '<StructLayout(LayoutKind.Sequential)> _
            'Friend Structure KEYBDINPUT
            '    Public wVk As UShort
            '    Public wScan As UShort
            '    Public dwFlags As KEYEVENTF
            '    Public time As UInteger
            '    Public dwExtraInfo As IntPtr
            'End Structure

            '<StructLayout(LayoutKind.Sequential, Pack:=8)> _
            'Friend Structure HARDWAREINPUT
            '    Public uMsg As UInt32
            '    Public wParamL As UInt16
            '    Public wParamH As UInt16
            'End Structure

            'Friend Enum InputType As UInt32
            '    Mouse = 0
            '    Keyboard = 1
            '    Hardware = 2
            'End Enum

            '<Flags()> _
            'Friend Enum MOUSEEVENTF As UInt32
            '    MOVE = &H1
            '    LEFTDOWN = &H2
            '    LEFTUP = &H4
            '    RIGHTDOWN = &H8
            '    RIGHTUP = &H10
            '    MIDDLEDOWN = &H20
            '    MIDDLEUP = &H40
            '    XDOWN = &H80
            '    XUP = &H100
            '    VIRTUALDESK = &H400
            '    WHEEL = &H800
            '    ABSOLUTE = &H8000
            'End Enum

            '<Flags()> _
            'Public Enum KEYEVENTF As UInt32
            '    EXTENDEDKEY = 1
            '    KEYUP = 2
            '    [UNICODE] = 4
            '    SCANCODE = 8
            'End Enum

            'Private Shared Sub SendString(ByVal s As String)
            '    Dim _fwid As IntPtr = GetForegroundWindow()
            '    Dim _dwtid As Integer = GetWindowThreadProcessId(_fwid, Nothing)
            '    Dim _hkl As IntPtr = GetKeyboardLayout(_dwtid)

            '    Dim inputs As New List(Of INPUT)
            '    For Each c As Char In s
            '        'Dim vkcode As UShort = VkKeyScanEx(c, _hkl)
            '        'If (vkcode And &HFF) <> -1 Then
            '        Dim keybinp As KEYBDINPUT
            '        keybinp.wVk = 0
            '        keybinp.wScan = Convert.ToUInt16(c)
            '        keybinp.dwFlags = KEYEVENTF_UNICODE
            '        keybinp.time = 0
            '        keybinp.dwExtraInfo = GetMessageExtraInfo()
            '        Dim inputonec As INPUT
            '        inputonec.dwType = InputType.Keyboard
            '        inputonec.ki = keybinp
            '        inputs.Add(inputonec)
            '        'End If
            '        'If (vkcode And &HFF) <> -1 Then
            '        'Dim keybinp As KEYBDINPUT
            '        keybinp.wVk = 0
            '        keybinp.wScan = Convert.ToUInt16(c)
            '        keybinp.dwFlags = KEYEVENTF_UNICODE Or KEYEVENTF_KEYUP
            '        keybinp.time = 0
            '        keybinp.dwExtraInfo = GetMessageExtraInfo()
            '        'Dim inputonec As INPUT
            '        inputonec.dwType = InputType.Keyboard
            '        inputonec.ki = keybinp
            '        inputs.Add(inputonec)
            '        'End If
            '    Next
            '    Dim inpar As INPUT() = inputs.ToArray()
            '    If SendInput(Convert.ToUInt32(inpar.Length), inpar, Marshal.SizeOf(GetType(INPUT))) <> inputs.Count Then
            '        Dim ex As Win32Exception = New Win32Exception(Marshal.GetLastWin32Error())
            '        Throw New ApplicationException("Input Failed " & ex.Message, ex)
            '    End If
            'End Sub
            '
            '<DllImport("user32.dll", SetLastError:=True)> Public Shared Function SendInput(ByVal cInputs As UInteger, ByRef pInputs As INPUT(), ByVal cbSize As Int32) As UInteger
            'End Function
            Public Declare Function GetDesktopWindow Lib "user32.dll" () As IntPtr
            Public Declare Function GetWindowDC Lib "user32.dll" (ByVal hWnd As IntPtr) As IntPtr
            Public Declare Function ReleaseDC Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal hDC As IntPtr) As IntPtr
            Public Declare Function GetWindowRect Lib "user32.dll" (ByVal hWnd As IntPtr, ByRef rect As RECT) As IntPtr
            Public Declare Sub mouse_event Lib "user32.dll" (ByVal dwFlags As UInt32, ByVal dx As UInt32, ByVal dy As UInt32, ByVal dwData As UInt32, ByVal dwExtraInfo As IntPtr)
            Public Declare Auto Function GetCursorPos Lib "User32.dll" (ByRef lpPoint As Point) As Long
            Public Declare Sub keybd_event Lib "user32.dll" (ByVal bVk As Byte, ByVal bScan As Byte, ByVal dwFlags As UInteger, ByVal dwExtraInfo As Integer)
            Public Declare Function GetMessageExtraInfo Lib "user32.dll" () As IntPtr
            Public Declare Function GetForegroundWindow Lib "user32" () As IntPtr
            Public Declare Auto Function GetWindowThreadProcessId Lib "user32.dll" (ByVal hwnd As IntPtr, ByRef lpdwProcessId As Integer) As Integer
            Public Declare Function GetKeyboardLayout Lib "user32.dll" (ByVal idThread As Integer) As IntPtr
            <DllImport("user32.dll")> Private Shared Function VkKeyScanEx(ByVal ch As Char, ByVal dwhkl As IntPtr) As Short
            End Function
            <DllImport("user32.dll", CharSet:=CharSet.Unicode)> Private Shared Function VkKeyScanW(ByVal ch As Char) As Short
            End Function
            Public Declare Function MapVirtualKeyW Lib "user32.dll" (ByVal uCode As UInteger, ByVal uMapType As UInteger) As UInteger
            Public Declare Function MapVirtualKeyExW Lib "user32.dll" (ByVal uCode As UInteger, ByVal uMapType As UInteger, ByVal dwhkl As IntPtr) As UInteger

            Public Shared Sub sendkey(ByVal vk As Byte)
                Dim _fwid As IntPtr = GetForegroundWindow()
                Dim _dwtid As Integer = GetWindowThreadProcessId(_fwid, Nothing)
                Dim _hkl As IntPtr = GetKeyboardLayout(_dwtid)
                Dim bScan As Byte = MapVirtualKeyExW(vk, 4, _hkl)
                If (bScan) <> 0 Then
                    keybd_event(vk, bScan, 0, IntPtr.Zero)
                    keybd_event(vk, bScan, KEYEVENTF_KEYUP, IntPtr.Zero)
                End If
            End Sub

            Public Shared Sub sendkeys(ByVal s As String)
                Dim _fwid As IntPtr = GetForegroundWindow()
                Dim _dwtid As Integer = GetWindowThreadProcessId(_fwid, Nothing)
                Dim _hkl As IntPtr = GetKeyboardLayout(_dwtid)
                For Each c As Char In s
                    Dim exvkcode As Short = VkKeyScanEx(c, _hkl)
                    Dim bVk As Byte = exvkcode And &HFF
                    Dim dwFlags As UInteger = (exvkcode And &HFF00) >> 8
                    Dim bScan As Byte = MapVirtualKeyExW(bVk, 4, _hkl)
                    If (bScan) <> 0 Then
                        If dwFlags = 1 Then
                            keybd_event(16, 42, 0, IntPtr.Zero)
                        End If
                        keybd_event(bVk, bScan, 0, IntPtr.Zero)
                        keybd_event(bVk, bScan, KEYEVENTF_KEYUP, IntPtr.Zero)
                        If dwFlags = 1 Then
                            keybd_event(16, 42, KEYEVENTF_KEYUP, IntPtr.Zero)
                        End If
                    End If
                Next
            End Sub

            Public Shared Sub SendClick()
                SendDown()
                SendUp()
            End Sub

            Public Shared Sub SendRightClick()
                SendDownRight()
                SendUpRight()
            End Sub

            Public Shared Sub SendUp()
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, New System.IntPtr())
            End Sub

            Public Shared Sub SendDown()
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, New System.IntPtr())
            End Sub

            Public Shared Sub SendUpRight()
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, New System.IntPtr())
            End Sub

            Public Shared Sub SendDownRight()
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, New System.IntPtr())
            End Sub

            Public Shared Sub MoveMouse(ByVal p As System.Drawing.Point)
                System.Windows.Forms.Cursor.Position = New System.Drawing.Point(CType(p.X, Integer), CType(p.Y, Integer))
            End Sub

            Public Shared APPCOMMAND_VOLUME_MUTE As Integer = &HE80000
            Public Shared APPCOMMAND_VOLUME_UP As Integer = &HEA0000
            Public Shared APPCOMMAND_VOLUME_DOWN As Integer = &HE90000
            Public Shared WM_APPCOMMAND As Integer = &HE319
            Public Shared MOUSEEVENTF_LEFTDOWN As UInt32 = &HE0002
            Public Shared MOUSEEVENTF_LEFTUP As UInt32 = &HE0004
            Public Shared MOUSEEVENTF_RIGHTDOWN As UInt32 = &HE08
            Public Shared MOUSEEVENTF_RIGHTUP As UInt32 = &HE10
            Public Shared MOUSEEVENTF_MIDDLEDOWN As UInt32 = &HE0020
            Public Shared MOUSEEVENTF_MIDDLEUP As UInt32 = &HE0040
            Public Shared VK_MBUTTON As UInt16 = &HE04
            Public Shared VK_LBUTTON As UInt16 = &HE01
            Public Shared VK_RBUTTON As UInt16 = &HE02
        End Class
    End Class
End Module
