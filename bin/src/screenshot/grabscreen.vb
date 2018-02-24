Imports System.Runtime.InteropServices
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Threading

<ComClass(grabscreen.ClassId, grabscreen.InterfaceId, grabscreen.EventsId)> Public Class grabscreen

#Region "COM GUIDs"
    ' These  GUIDs provide the COM identity for this class 
    ' and its COM interfaces. If you change them, existing 
    ' clients will no longer be able to access the class.
    Public Const ClassId As String = "74f8543f-3f45-4425-9516-223ae0909716"
    Public Const InterfaceId As String = "44615411-10b8-44f3-8bc8-ab573e4abca8"
    Public Const EventsId As String = "77185345-459c-48b6-90f2-5c52d3291b67"
#End Region

    Private Sub log(ByVal msg As String)
        Try
            Dim logfile As System.IO.StreamWriter = My.Computer.FileSystem.OpenTextFileWriter("C:\Program Files\Apache24\logs\screenshot.txt", True)
            logfile.WriteLine(msg)
            logfile.Close()
        Catch ex As Exception
        End Try
    End Sub

    Private Shared lock As Boolean = False

    Private Sub SaveScaledImage(ByVal path As String, ByVal OldImage As Image, ByVal newWidth As Integer, ByVal newHeight As Integer, ByVal quality As Long)
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
        Dim myEncoder As Encoder
        Dim myEncoderParameter As EncoderParameter
        Dim myEncoderParameters As EncoderParameters
        myEncoder = Encoder.Quality
        myEncoderParameters = New EncoderParameters(1)
        myEncoderParameter = New EncoderParameter(myEncoder, CType(quality, Int32))
        myEncoderParameters.Param(0) = myEncoderParameter
        Try
            newImage.Save(path, GetEncoderInfo(ImageFormat.Jpeg), myEncoderParameters)
            newImage.Dispose()
        Catch ex As Exception
        End Try
    End Sub

    Public Function getscreen_resolution() As Integer()
        Dim intX As Integer = Screen.PrimaryScreen.Bounds.Width
        Dim intY As Integer = Screen.PrimaryScreen.Bounds.Height
        Dim retval(1) As Integer
        retval(0) = intX
        retval(1) = intY
        Return retval
    End Function

    Public Sub sendkeybevent(ByVal key)
        ScreenCapture.User32.keybd_event(CType(key, Byte), 0, 0, 0)
    End Sub

    Public Sub volumeup()
        ScreenCapture.User32.keybd_event(CType(Keys.VolumeUp, Byte), 0, 0, 0)
    End Sub

    Public Sub volumedown()
        ScreenCapture.User32.keybd_event(CType(Keys.VolumeDown, Byte), 0, 0, 0)
    End Sub

    Public Function getmousepos() As Integer()
        Dim p As Point
        Dim R As Long = ScreenCapture.User32.GetCursorPos(p)
        Dim retval(1) As Integer
        retval(0) = p.X
        retval(1) = p.Y
        Return retval
    End Function


    Public Sub mousemove(ByVal x, ByVal y)
        Dim p As System.Drawing.Point = New System.Drawing.Point(x, y)
        ScreenCapture.User32.MoveMouse(p)
    End Sub

    Public Sub mouseclickl(ByVal x, ByVal y)
        mousemove(x, y)
        ScreenCapture.User32.SendClick()
    End Sub

    Public Sub mousedblclickl(ByVal x, ByVal y)
        mousemove(x, y)
        ScreenCapture.User32.SendClick()
        ScreenCapture.User32.SendClick()
    End Sub

    Public Sub mouseclickr(ByVal x, ByVal y)
        mousemove(x, y)
        ScreenCapture.User32.SendRightClick()
    End Sub

    Public Sub mousedblclickr(ByVal x, ByVal y)
        mousemove(x, y)
        ScreenCapture.User32.SendRightClick()
        ScreenCapture.User32.SendRightClick()
    End Sub


    Public Sub grab(ByVal location As String, ByVal width As Integer, ByVal height As Integer, ByVal quality As Long)
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
        Dim img As Image
        Try
            Dim scap As ScreenCapture = New ScreenCapture()
            img = scap.CaptureScreen()
            SaveScaledImage(fullpath, img, width, height, quality)
            img.Dispose()
        Catch ex As Exception
        End Try
    End Sub

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


Public Class ScreenCapture
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
        Public Declare Function GetDesktopWindow Lib "user32.dll" () As IntPtr
        Public Declare Function GetWindowDC Lib "user32.dll" (ByVal hWnd As IntPtr) As IntPtr
        Public Declare Function ReleaseDC Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal hDC As IntPtr) As IntPtr
        Public Declare Function GetWindowRect Lib "user32.dll" (ByVal hWnd As IntPtr, ByRef rect As RECT) As IntPtr
        Public Declare Sub mouse_event Lib "user32.dll" (ByVal dwFlags As UInt32, ByVal dx As UInt32, ByVal dy As UInt32, ByVal dwData As UInt32, ByVal dwExtraInfo As IntPtr)        
        Public Declare Auto Function GetCursorPos Lib "User32.dll" (ByRef lpPoint As Point) As Long
        Public Declare Sub keybd_event Lib "user32.dll" (ByVal bVk As Byte, ByVal bScan As Byte, ByVal dwFlags As UInteger, ByVal dwExtraInfo As Integer)
        ' sendInput SendMessage PostMessage

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

        Private Shared APPCOMMAND_VOLUME_MUTE As Integer = &HE80000
        Private Shared APPCOMMAND_VOLUME_UP As Integer = &HEA0000
        Private Shared APPCOMMAND_VOLUME_DOWN As Integer = &HE90000
        Private Shared WM_APPCOMMAND As Integer = &HE319

        Private Shared MOUSEEVENTF_LEFTDOWN As UInt32 = &HE0002
        Private Shared MOUSEEVENTF_LEFTUP As UInt32 = &HE0004
        Private Shared MOUSEEVENTF_RIGHTDOWN As UInt32 = &HE08
        Private Shared MOUSEEVENTF_RIGHTUP As UInt32 = &HE10
        Private Shared MOUSEEVENTF_MIDDLEDOWN As UInt32 = &HE0020
        Private Shared MOUSEEVENTF_MIDDLEUP As UInt32 = &HE0040
        Private Shared VK_MBUTTON As UInt16 = &HE04
        Private Shared VK_LBUTTON As UInt16 = &HE01
        Private Shared VK_RBUTTON As UInt16 = &HE02
    End Class
End Class