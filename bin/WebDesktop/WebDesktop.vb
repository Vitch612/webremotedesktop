Imports System.Runtime.InteropServices
Imports System.Runtime.ConstrainedExecution
Imports System.ComponentModel
Imports System.Security.Principal
Imports System.Security.Cryptography
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Threading
Imports System.Diagnostics
Imports System.Net.Sockets
Imports System.Net
Imports System.Text
Imports System.Web
Imports SoundCapture

Module WebDesktop
    Private Class httpserver
        Private idletime As Integer = 10
        Private port As Integer = 8888
        Private imageresolution As Integer = 80
        Private Shared loglevel As Byte = 2
        Dim bufferingtime As UShort = 500
        Private enableaudio = True
        Dim _snd As CoreAudio
        Private timer As DateTime
        Private watchfortimeout As Thread
        Private httplistenthread As Thread
        Private getsoundthread As Thread
        Public _httpListener As HttpListener = New HttpListener()
        Public keepgoing As Boolean = True
        Dim sessions As List(Of usersession) = New List(Of usersession)
        Private memcache As MemoryStream = New MemoryStream()
        Private Shared locksync As Object = New Object
        Private mp3header As Byte() = New Byte() {255, 251, 144, 4, 0, 15, 240, 0, 0, 105, 0, 0, 0, 8, 0, 0, 13, 32, 0, 0, 1, 0, 0, 1, 164, 0, 0, 0, 32, 0, 0, 52, 128, 0, 0, 4, 76, 65, 77, 69, 51, 46, 57, 57, 46, 49, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85, 85}
        Protected restart As Boolean = False
        Dim samples As List(Of samplepointer) = New List(Of samplepointer)

        Private Class samplepointer
            Public time As DateTime
            Public position As Long
            Public Sub New(ByVal pos As Long)
                time = Now
                position = pos
            End Sub
        End Class

        Private Class usersession
            Public userid As String
            Public lastrequest As DateTime
            Public position As Long
            Public startpos As Long
            Public Sub New(ByVal _userid As String, ByVal startingpos As Long)
                userid = _userid
                position = 0
                SyncLock locksync
                    startpos = startingpos
                End SyncLock
                lastrequest = Now
            End Sub
        End Class

        Private Function getstartingposition() As Long
            Dim currenttime As DateTime = Now
            Dim ts1 As TimeSpan
            Dim ts2 As TimeSpan
            Dim match As samplepointer
            For i As Integer = 0 To samples.Count - 1
                ts1 = currenttime - samples.Item(i).time
                If ts1.TotalMilliseconds > bufferingtime - 50 And ts1.TotalMilliseconds < bufferingtime + 50 Then
                    If Not match Is Nothing Then
                        ts2 = currenttime - match.time
                        If Math.Abs(bufferingtime - ts1.TotalMilliseconds) > Math.Abs(bufferingtime - ts2.TotalMilliseconds) Then
                            match = samples.Item(i)
                        End If
                    Else
                        match = samples.Item(i)
                    End If
                End If
            Next
            If match Is Nothing Then
                Return 0
            Else
                Return match.position
            End If
        End Function

        Private Function FindUser(ByVal _userid As String) As Integer
            For i As Integer = 0 To sessions.Count - 1
                If sessions(i).userid.Equals(_userid) Then
                    Return i
                End If
            Next
            Return -1
        End Function

        Private Sub timerThread()
            Dim count As Byte = 0
            Do
                Dim currentDate As DateTime = Now
                If currentDate.Subtract(timer).TotalSeconds > idletime And idletime <> 0 Then
                    shutdown()
                End If
                count += 1
                If count = 20 Then
                    count = 0
                    listcleanup()
                End If
                Thread.Sleep(50)
            Loop Until Not keepgoing
        End Sub

        Private Sub listcleanup()
            Dim currenttime As DateTime = Now
            Dim ts As TimeSpan
            For i As Integer = sessions.Count - 1 To 0 Step -1
                ts = currenttime - sessions.Item(i).lastrequest
                If ts.TotalSeconds > 20 Then
                    sessions.RemoveAt(i)
                End If
            Next
            For i As Integer = samples.Count - 1 To 0 Step -1
                ts = currenttime - samples.Item(i).time
                If ts.TotalMilliseconds > 2 * bufferingtime Then
                    samples.RemoveAt(i)
                End If
            Next
        End Sub

        Private Sub stopall(Optional ByVal waittime As Integer = 50)
            Thread.Sleep(waittime)
            If enableaudio Then
                _snd.Dispose()
                getsoundthread.Abort()
            End If
            Try
                If _httpListener.IsListening Then
                    _httpListener.Stop()
                End If
                _httpListener.Abort()
                _httpListener.Close()
                Thread.Sleep(20)
                httplistenthread.Abort()
            Catch ex As Exception
                log("[Shutdown]" & Environment.NewLine & ex.Message & Environment.NewLine & ex.Source & Environment.NewLine & ex.StackTrace, EventLogEntryType.Error)
            End Try

            If restart Then
                restart = False
                Dim filePath As String = System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase
                filePath = (filePath.Substring(filePath.IndexOf("file:///") + 8)).Replace("\", "/")
                Dim Info As ProcessStartInfo = New ProcessStartInfo()
                Info.Arguments = "/C ping 127.0.0.1 -n 1 & """ & filePath & """"
                Info.WindowStyle = ProcessWindowStyle.Hidden
                Info.CreateNoWindow = True
                Info.FileName = "cmd.exe"
                Process.Start(Info)
            End If
        End Sub

        Private Sub shutdown(Optional ByVal waittime As Integer = 50)
            keepgoing = False
            Dim exitall As Thread = New Thread(Sub() stopall(waittime))
            exitall.IsBackground = False
            exitall.Start()
        End Sub

        Private Sub getsoundsamples()
            While keepgoing
                Dim buffer As Byte() = _snd.readbytes()
                If buffer.Length > 0 Then
                    SyncLock locksync
                        memcache.Write(buffer, 0, buffer.Length)
                        samples.Add(New samplepointer(memcache.Length))
                    End SyncLock
                End If
                Thread.Sleep(20)
            End While
        End Sub

        Private Function getbuffer(ByVal startpos As Long, ByVal count As Long) As Byte()
            If count > 0 Then
                Dim buffer As Byte() = New Byte(count - 1) {}
                SyncLock locksync
                    Dim oldpos As Long = memcache.Position
                    memcache.Position = startpos
                    memcache.Read(buffer, 0, count)
                    memcache.Position = oldpos
                End SyncLock
                Return buffer
            Else
                Return New Byte() {}
            End If
        End Function

        Public Shared Sub log(ByVal msg As String, Optional ByVal type As EventLogEntryType = EventLogEntryType.Error)
            If loglevel < 1 Then
                Return
            End If
            Dim appName As String = "WebDesktop"
            Dim eventData As EventSourceCreationData
            eventData = New EventSourceCreationData(appName, "Application")
            If Not EventLog.SourceExists(appName) Then
                EventLog.CreateEventSource(eventData)
            End If
            Dim eLog As New EventLog()
            eLog.Source = appName
            eLog.WriteEntry(msg, type)
        End Sub

        Public Shared Sub loginfo(ByVal info As String(,), Optional ByVal type As EventLogEntryType = EventLogEntryType.Information)
            If loglevel < 2 Then
                Return
            End If
            Dim msg As String = ""
            For i As Integer = 0 To info.GetUpperBound(0)
                If info(i, 1).Equals("") Then
                    msg &= info(i, 0) & Environment.NewLine
                Else
                    msg &= info(i, 0) & " = " & info(i, 1) & Environment.NewLine
                End If
            Next
            log(msg, EventLogEntryType.Information)
        End Sub

        Private Function getmimetype(ByVal filename As String) As String
            Try
                Select Case filename.Substring(filename.LastIndexOf("."))
                    Case ".avi"
                        Return "video/x-msvideo"
                    Case ".css"
                        Return "text/css"
                    Case ".doc"
                        Return "application/msword"
                    Case ".gif"
                        Return "image/gif"
                    Case ".htm"
                    Case ".html"
                        Return "text/html"
                    Case ".jpg"
                    Case ".jpeg"
                        Return "image/jpeg"
                    Case ".js"
                        Return "application/x-javascript"
                    Case ".mp3"
                        Return "audio/mpeg"
                    Case ".png"
                        Return "image/png"
                    Case ".pdf"
                        Return "application/pdf"
                    Case ".ppt"
                        Return "application/vnd.ms-powerpoint"
                    Case ".zip"
                        Return "application/zip"
                    Case ".txt"
                        Return "text/plain"
                    Case ".ico"
                        Return "image/x-icon"
                End Select
            Catch ex As Exception
            End Try
            Return "application/octet-stream"
        End Function

        Private Sub FileServeThread(ByVal context As HttpListenerContext)
            timer = Now
            Dim max As Long = 9223372036854775807
            Dim userid As String = System.Text.Encoding.Unicode.GetString(MD5.Create().ComputeHash(System.Text.Encoding.Unicode.GetBytes(context.Request.RemoteEndPoint.ToString().Substring(0, context.Request.RemoteEndPoint.ToString().LastIndexOf(":")) & context.Request.UserAgent.ToString())))
            Dim user As Integer = FindUser(userid)
            If user = -1 Then
                sessions.Add(New usersession(userid, getstartingposition()))
                user = FindUser(userid)
            End If
            sessions.Item(user).lastrequest = Now
            Try
                Dim response As HttpListenerResponse = context.Response
                Dim outstream As Stream = response.OutputStream()                
                If context.Request.RawUrl.Equals("/audio.mp3") And enableaudio Then
                    Dim keys As String() = context.Request.Headers.AllKeys
                    Dim sreport As String(,) = New String(keys.Length, 1) {}
                    sreport(0, 0) = "[HTTP Request Headers]"
                    sreport(0, 1) = ""
                    For i As Integer = 0 To keys.Length - 1
                        sreport(i + 1, 0) = keys(i)
                        sreport(i + 1, 1) = context.Request.Headers.Item(keys(i))
                    Next
                    loginfo(sreport)
                    If sessions.Item(user).startpos = 0 Then
                        sessions.Item(user).startpos = getstartingposition()
                    End If
                    If sessions.Item(user).startpos = 0 Or memcache.Length = 0 Then
                        response.StatusCode = 404
                        response.StatusDescription = "Not Found"
                        'loginfo(New String(,) {{"Request Refused 404", ""}, _
                        '                       {"User Stream Start", sessions.Item(user).startpos}, _
                        '                       {"Cache Length", memcache.Length}})
                    Else
                        Dim reqrange As String = context.Request.Headers.Item("Range")
                        Dim reqaccept As String = context.Request.Headers.Item("Accept")
                        If (reqrange Is Nothing And reqaccept Is Nothing) Or Not reqaccept.Equals("*/*") Then
                            Dim buffer As Byte() = getbuffer(sessions.Item(user).startpos, memcache.Length)
                            Dim tmpbuffer As Byte() = New Byte(buffer.Length + mp3header.Length - 1) {}
                            mp3header.CopyTo(tmpbuffer, 0)
                            buffer.CopyTo(tmpbuffer, mp3header.Length)
                            response.StatusCode = 200
                            response.StatusDescription = "OK"
                            response.KeepAlive = True
                            response.ContentType = "audio/mpeg"
                            response.AddHeader("Content-Disposition", "attachment; filename=""audio.mp3""")
                            outstream.Write(tmpbuffer, 0, tmpbuffer.Length)
                            loginfo(New String(,) {{"Normal Response", ""}, _
                                                   {"userid ", user}, _
                                                   {"user startpos", sessions.Item(user).startpos}, _
                                                   {"BufferSize", memcache.Length}, _
                                                   {"data sent", tmpbuffer.Length}}, EventLogEntryType.Information)
                        Else
                            Dim bytefroms As String = "NA"
                            Dim bytetos As String = "NA"
                            Dim bytefrom As Long = 0
                            Dim byteto As Long = 0
                            Dim refuseranged As Boolean = False
                            Dim requestedbytes As Long = -1
                            If reqaccept.Equals("*/*") Then
                                byteto = -1
                            Else
                                If reqrange.IndexOf("-") <> -1 Then
                                    bytefroms = reqrange.Substring(reqrange.IndexOf("=") + 1, reqrange.IndexOf("-") - reqrange.IndexOf("=") - 1)
                                End If
                                If bytefroms = "NA" Then
                                    bytefrom = -1
                                    refuseranged = True
                                Else
                                    Try
                                        bytefrom = Convert.ToInt64(bytefroms)
                                    Catch ex As Exception
                                        bytefrom = -1
                                        refuseranged = True
                                    End Try
                                End If
                                If reqrange.IndexOf("/") <> -1 Then
                                    bytetos = reqrange.Substring(reqrange.IndexOf("-") + 1, reqrange.IndexOf("/") - reqrange.IndexOf("-") - 1)
                                Else
                                    bytetos = reqrange.Substring(reqrange.IndexOf("-") + 1, reqrange.Length - reqrange.IndexOf("-") - 1)
                                End If
                                If bytetos = "NA" Then
                                    byteto = -1
                                Else
                                    Try
                                        byteto = Convert.ToInt64(bytetos)
                                    Catch ex As Exception
                                        byteto = -1
                                    End Try
                                End If
                            End If

                            If byteto <> -1 Then
                                requestedbytes = byteto - bytefrom + 1
                            End If
                            If bytefrom = 0 Then
                                requestedbytes -= mp3header.Length
                            Else
                                bytefrom -= mp3header.Length
                            End If
                            Dim mp3buffer As Byte() = New Byte() {}
                            If byteto = -1 Then
                                loginfo(New String(,) {{"ranged - no end", ""}, _
                                                       {"userid ", user}, _
                                                       {"readfrom", sessions.Item(user).startpos + bytefrom}, _
                                                       {"bytesread", (memcache.Length - sessions.Item(user).startpos - bytefrom)}, _
                                                       {"currentbuffer", memcache.Length}})
                                mp3buffer = getbuffer(sessions.Item(user).startpos + bytefrom, memcache.Length - sessions.Item(user).startpos - bytefrom)
                            ElseIf requestedbytes > 0 Then
                                If memcache.Length - sessions.Item(user).startpos - bytefrom > requestedbytes Then
                                    loginfo(New String(,) {{"ranged - with end - can send requested", ""}, _
                                                           {"userid ", user}, _
                                                           {"readfrom", sessions.Item(user).startpos + bytefrom}, _
                                                           {"bytesread", requestedbytes}, _
                                                           {"currentbuffer", memcache.Length}}, EventLogEntryType.Warning)
                                    mp3buffer = getbuffer(sessions.Item(user).startpos + bytefrom, requestedbytes)
                                Else
                                    loginfo(New String(,) {{"ranged - with end - cannot send requested", ""}, _
                                                           {"userid ", user}, _
                                                           {"readfrom", sessions.Item(user).startpos + bytefrom}, _
                                                           {"bytesread", memcache.Length - sessions.Item(user).startpos - bytefrom}, _
                                                           {"currentbuffer", memcache.Length}}, EventLogEntryType.Warning)
                                    mp3buffer = getbuffer(sessions.Item(user).startpos + bytefrom, memcache.Length - sessions.Item(user).startpos - bytefrom)
                                End If
                            ElseIf requestedbytes < 0 Then
                                refuseranged = True
                            End If
                            Dim datalength As Long = If(bytefrom = 0, mp3header.Length + mp3buffer.Length, mp3buffer.Length)
                            If bytefrom <> 0 Then
                                bytefrom += mp3header.Length
                            End If
                            If bytefrom = 0 And byteto <> -1 And byteto + 1 < mp3header.Length Then
                                refuseranged = True
                            End If
                            If Not refuseranged Then
                                response.StatusCode = 206
                                response.KeepAlive = True
                                response.StatusDescription = "Partial content"
                                response.AddHeader("Content-Type", "audio/mpeg")
                                response.AddHeader("Content-Range", "bytes " & bytefrom & "-" & (datalength + bytefrom - 1) & "/" & max)
                                If bytefrom = 0 Then
                                    Dim buffer As Byte() = New Byte(mp3header.Length + mp3buffer.Length - 1) {}
                                    mp3header.CopyTo(buffer, 0)
                                    mp3buffer.CopyTo(buffer, mp3header.Length)
                                    outstream.Write(buffer, 0, buffer.Length)
                                Else
                                    If mp3buffer.Length > 0 Then
                                        outstream.Write(mp3buffer, 0, mp3buffer.Length)
                                        outstream.Flush()
                                    End If
                                End If
                                loginfo(New String(,) {{"Ranged Response", ""}, _
                                                       {"userid", user}, _
                                                       {"range served", bytefrom & "-" & (datalength + bytefrom - 1) & "/" & max}, _
                                                       {"bytes sent", mp3buffer.Length}})
                                sessions.Item(user).position += mp3buffer.Length
                            Else
                                response.StatusCode = 416
                                response.StatusDescription = "Requested range not satisfiable"
                                response.AddHeader("Content-Range", "bytes 0-" & (memcache.Length - sessions.Item(user).startpos + mp3header.Length - 1) & "/*")
                                loginfo(New String(,) {{"Ranged Refused", ""}, _
                                                       {"userid", user}, _
                                                       {"range suggested", "0-" & (memcache.Length - sessions.Item(user).startpos + mp3header.Length - 1) & "/*"}}, EventLogEntryType.Warning)
                            End If
                        End If
                    End If                    
                ElseIf context.Request.RawUrl.Equals("/cursor.png") Or context.Request.RawUrl.Equals("/favicon.ico") Or context.Request.RawUrl.Equals("/jquery.min.js") Or context.Request.RawUrl.Equals("/styles.css") Or context.Request.RawUrl.Equals("/close.png") Or context.Request.RawUrl.Equals("/settings.png") Or context.Request.RawUrl.Equals("/interactions.js") Or context.Request.RawUrl.Equals("/audio.js") Then
                    Dim filePath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)
                    filePath = (filePath.Substring(filePath.IndexOf("file:\\") + 7) & "\files").Replace("\", "/")
                    Dim buffer As Byte() = File.ReadAllBytes(filePath & context.Request.RawUrl)
                    response.StatusCode = 200
                    response.StatusDescription = "OK"
                    response.ContentType = getmimetype(context.Request.RawUrl)
                    outstream.Write(buffer, 0, buffer.Length)
                ElseIf context.Request.RawUrl.Equals("/") Then
                    Dim filePath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)
                    If enableaudio Then
                        filePath = (filePath.Substring(filePath.IndexOf("file:\\") + 7) & "/files/index.html").Replace("\", "/")
                    Else
                        filePath = (filePath.Substring(filePath.IndexOf("file:\\") + 7) & "/files/index.html-noaudio").Replace("\", "/")
                    End If
                    Dim index As String = File.ReadAllText(filePath)
                    If enableaudio Then
                        index = index.Replace("{audio}", "checked")
                    Else
                        index = index.Replace("{audio}", "")
                    End If
                    index = index.Replace("{timeout}", idletime)
                    index = index.Replace("{port}", port)
                    index = index.Replace("{resolution}", imageresolution)
                    index = index.Replace("{loglevel}", loglevel)
                    Dim buffer As Byte() = System.Text.Encoding.ASCII.GetBytes(index)
                    response.StatusCode = 200
                    response.StatusDescription = "OK"
                    response.ContentType = "text/html"
                    outstream.Write(buffer, 0, buffer.Length)
                ElseIf context.Request.RawUrl.Equals("/exit") Then
                    response.Redirect("/")
                    shutdown()
                ElseIf context.Request.RawUrl.Equals("/restart") Then
                    response.Redirect("/waitforrestart")
                ElseIf context.Request.RawUrl.Equals("/waitforrestart") Then
                    Dim filePath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)
                    filePath = (filePath.Substring(filePath.IndexOf("file:\\") + 7) & "\files").Replace("\", "/")
                    Dim buffer As Byte() = File.ReadAllBytes(filePath & "/restarting.html")
                    response.StatusCode = 200
                    response.StatusDescription = "OK"
                    response.ContentType = "text/html"
                    outstream.Write(buffer, 0, buffer.Length)
                    restart = True
                    shutdown(100)
                Else
                    Dim requesturl As String = context.Request.RawUrl
                    If requesturl.IndexOf("?") >= 0 Then
                        requesturl = requesturl.Substring(0, requesturl.IndexOf("?"))
                    End If
                    If requesturl.Equals("/screen.jpeg") Then
                        Dim w As Integer = Screen.PrimaryScreen.Bounds.Width
                        Dim h As Integer = Screen.PrimaryScreen.Bounds.Height
                        If Not context.Request.QueryString.Item("w") Is Nothing And Not context.Request.QueryString.Item("h") Is Nothing Then
                            Try
                                w = Convert.ToInt32(context.Request.QueryString.Item("w"))
                                h = Convert.ToInt32(context.Request.QueryString.Item("h"))
                                Dim buffer As Byte() = Functionality.getScreenshot(w, h, imageresolution)
                                response.StatusCode = 200
                                response.StatusDescription = "OK"
                                response.ContentType = "image/jpeg"
                                outstream.Write(buffer, 0, buffer.Length)
                            Catch ex As Exception
                                Dim filePath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)
                                filePath = (filePath.Substring(filePath.IndexOf("file:\\") + 7) & "/files/error.png").Replace("\", "/")
                                Dim buffer As Byte() = File.ReadAllBytes(filePath)
                                response.StatusCode = 200
                                response.StatusDescription = "OK"
                                response.ContentType = "image/png"
                                outstream.Write(buffer, 0, buffer.Length)
                            End Try
                        Else
                            Dim filePath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)
                            filePath = (filePath.Substring(filePath.IndexOf("file:\\") + 7) & "/files/error.png").Replace("\", "/")
                            Dim buffer As Byte() = File.ReadAllBytes(filePath)
                            response.StatusCode = 200
                            response.StatusDescription = "OK"
                            response.ContentType = "image/png"
                            outstream.Write(buffer, 0, buffer.Length)
                        End If
                    ElseIf requesturl.Equals("/savesettings") Then
                        Dim filePath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)
                        filePath = (filePath.Substring(filePath.IndexOf("file:\\") + 7) & "/files/config.ini").Replace("\", "/")
                        Try
                            Dim saved As Boolean() = New Boolean(context.Request.QueryString.Count - 1) {}
                            For i As Integer = 0 To saved.Length - 1
                                saved(i) = False
                            Next
                            Dim config As String() = File.ReadAllLines(filePath)
                            For l As Integer = 0 To config.Length - 1
                                For i As Integer = 0 To context.Request.QueryString.Count - 1
                                    If config(l).IndexOf(context.Request.QueryString.Keys.Item(i)) <> -1 Then
                                        saved(i) = True
                                        If context.Request.QueryString.Keys.Item(i).Equals("Audio") Then
                                            config(l) = config(l).Substring(0, config(l).IndexOf("=") + 1) & If(context.Request.QueryString.Item(i).ToLower().Equals("true"), 1, 0)
                                        Else
                                            config(l) = config(l).Substring(0, config(l).IndexOf("=") + 1) & context.Request.QueryString.Item(i)
                                        End If
                                    End If
                                Next
                            Next
                            For i As Integer = 0 To saved.Length - 1
                                If Not saved(i) Then
                                    Array.Resize(config, config.Length + 1)
                                    config(config.Length - 1) = context.Request.QueryString.Keys.Item(i) & "=" & context.Request.QueryString.Item(i)
                                End If
                            Next
                            Dim newconfig As String = ""
                            For l As Integer = 0 To config.Length - 1
                                newconfig &= config(l) & Environment.NewLine
                            Next
                            Dim configfile As System.IO.FileStream = File.Create(filePath)
                            Dim buffer As Byte() = System.Text.Encoding.ASCII.GetBytes(newconfig)
                            configfile.Write(buffer, 0, buffer.Length)
                        Catch ex As Exception
                            log("[SaveConfig]" & Environment.NewLine & ex.Message & Environment.NewLine & ex.Source & Environment.NewLine & ex.StackTrace, EventLogEntryType.Error)
                        End Try
                        response.StatusCode = 204
                        response.StatusDescription = "No Content"
                    ElseIf requesturl.Equals("/receiveinput") Then
                        If Not context.Request.QueryString.Item("action") Is Nothing Then
                            Select Case context.Request.QueryString.Item("action")
                                Case "getscreensize"
                                    Dim retval As Integer() = Functionality.getscreen_resolution()
                                    Dim reply As String = retval(0) & "," & retval(1)
                                    Dim buffer As Byte() = System.Text.Encoding.ASCII.GetBytes(reply)
                                    response.StatusCode = 200
                                    response.StatusDescription = "OK"
                                    response.ContentType = "text/plain"
                                    outstream.Write(buffer, 0, buffer.Length)
                                Case "getmouse"
                                    Dim retval As Integer() = Functionality.getmousepos()
                                    Dim reply As String = retval(0) & "," & retval(1)
                                    Dim buffer As Byte() = System.Text.Encoding.ASCII.GetBytes(reply)
                                    response.StatusCode = 200
                                    response.StatusDescription = "OK"
                                    response.ContentType = "text/plain"
                                    outstream.Write(buffer, 0, buffer.Length)
                                Case "sendtext"
                                    If Not context.Request.QueryString.Item("text") Is Nothing Then
                                        Functionality.sendText(context.Request.QueryString.Item("text"))
                                    End If
                                    response.StatusCode = 204
                                    response.StatusDescription = "No Content"
                                Case "click"
                                    If Not context.Request.QueryString.Item("x") Is Nothing And Not context.Request.QueryString.Item("y") Is Nothing Then
                                        Try
                                            Dim x As Integer = Convert.ToInt32(context.Request.QueryString.Item("x"))
                                            Dim y As Integer = Convert.ToInt32(context.Request.QueryString.Item("y"))
                                            Functionality.mouseclickl(x, y)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                    response.StatusCode = 204
                                    response.StatusDescription = "No Content"
                                Case "clickr"
                                    If Not context.Request.QueryString.Item("x") Is Nothing And Not context.Request.QueryString.Item("y") Is Nothing Then
                                        Try
                                            Dim x As Integer = Convert.ToInt32(context.Request.QueryString.Item("x"))
                                            Dim y As Integer = Convert.ToInt32(context.Request.QueryString.Item("y"))
                                            Functionality.mouseclickr(x, y)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                    response.StatusCode = 204
                                    response.StatusDescription = "No Content"
                                Case "clickd"
                                    If Not context.Request.QueryString.Item("x") Is Nothing And Not context.Request.QueryString.Item("y") Is Nothing Then
                                        Try
                                            Dim x As Integer = Convert.ToInt32(context.Request.QueryString.Item("x"))
                                            Dim y As Integer = Convert.ToInt32(context.Request.QueryString.Item("y"))
                                            Functionality.mousedblclickl(x, y)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                    response.StatusCode = 204
                                    response.StatusDescription = "No Content"
                                Case "clickrd"
                                    If Not context.Request.QueryString.Item("x") Is Nothing And Not context.Request.QueryString.Item("y") Is Nothing Then
                                        Try
                                            Dim x As Integer = Convert.ToInt32(context.Request.QueryString.Item("x"))
                                            Dim y As Integer = Convert.ToInt32(context.Request.QueryString.Item("y"))
                                            Functionality.mousedblclickr(x, y)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                    response.StatusCode = 204
                                    response.StatusDescription = "No Content"
                                Case "mute"
                                    Functionality.mute()
                                    response.StatusCode = 204
                                    response.StatusDescription = "No Content"
                                Case "voldown"
                                    Functionality.volumedown()
                                    response.StatusCode = 204
                                    response.StatusDescription = "No Content"
                                Case "volup"
                                    Functionality.volumeup()
                                    response.StatusCode = 204
                                    response.StatusDescription = "No Content"
                                Case "moused"
                                    If Not context.Request.QueryString.Item("x") Is Nothing And Not context.Request.QueryString.Item("y") Is Nothing Then
                                        Try
                                            Dim x As Integer = Convert.ToInt32(context.Request.QueryString.Item("x"))
                                            Dim y As Integer = Convert.ToInt32(context.Request.QueryString.Item("y"))
                                            Functionality.mousemove(x, y)
                                            Functionality.mousedown()
                                        Catch ex As Exception
                                        End Try
                                    End If
                                    response.StatusCode = 204
                                    response.StatusDescription = "No Content"
                                Case "mouseu"
                                    If Not context.Request.QueryString.Item("x") Is Nothing And Not context.Request.QueryString.Item("y") Is Nothing Then
                                        Try
                                            Dim x As Integer = Convert.ToInt32(context.Request.QueryString.Item("x"))
                                            Dim y As Integer = Convert.ToInt32(context.Request.QueryString.Item("y"))
                                            Functionality.mousemove(x, y)
                                            Functionality.mouseup()
                                        Catch ex As Exception
                                        End Try
                                    End If
                                    response.StatusCode = 204
                                    response.StatusDescription = "No Content"
                                Case "mousem"
                                    If Not context.Request.QueryString.Item("x") Is Nothing And Not context.Request.QueryString.Item("y") Is Nothing Then
                                        Try
                                            Dim x As Integer = Convert.ToInt32(context.Request.QueryString.Item("x"))
                                            Dim y As Integer = Convert.ToInt32(context.Request.QueryString.Item("y"))
                                            Functionality.mousemove(x, y)
                                        Catch ex As Exception
                                        End Try
                                    End If
                                    response.StatusCode = 204
                                    response.StatusDescription = "No Content"
                                Case "sendbackspace"
                                    Functionality.sendKey(8)
                                    response.StatusCode = 204
                                    response.StatusDescription = "No Content"
                            End Select
                        End If
                    Else
                        response.StatusCode = 404
                        response.StatusDescription = "File Not Found"
                    End If
                End If
                outstream.Flush()
                outstream.Close()
                response.Close()
            Catch ex As Exception
                log("[FileServeThread]" & Environment.NewLine & ex.Message & Environment.NewLine & ex.Source & Environment.NewLine & ex.StackTrace, EventLogEntryType.Error)
            End Try
        End Sub

        Private Sub ThreadTask()
            Try
                Dim addresses As System.Net.IPAddress() = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName())
                For Each ip As System.Net.IPAddress In addresses
                    If ip.AddressFamily = AddressFamily.InterNetwork Then
                        _httpListener.Prefixes.Add("http://" & ip.ToString() & ":" & port & "/")
                    End If
                Next
                _httpListener.Prefixes.Add("http://localhost:" & port & "/")
                _httpListener.Prefixes.Add("http://*:" & port & "/")
                _httpListener.Prefixes.Add("http://+:" & port & "/")
                _httpListener.Start()
                Do
                    Dim context As HttpListenerContext
                    context = _httpListener.GetContext()
                    Dim t As Thread = New Thread(AddressOf FileServeThread)
                    t.IsBackground = True
                    t.Start(context)
                Loop Until Not keepgoing
            Catch e As Exception
                If Not e.Message.Equals("The I/O operation has been aborted because of either a thread exit or an application request") Then
                    log("[HttpListener]" & Environment.NewLine + e.Message, EventLogEntryType.Error)
                End If
            End Try
        End Sub

        Public Sub New()
            MyBase.New()
            Dim filePath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)
            filePath = (filePath.Substring(filePath.IndexOf("file:\\") + 7) & "/files/config.ini").Replace("\", "/")
            Try
                Dim config As String() = File.ReadAllLines(filePath)
                For Each line As String In config
                    Dim parts As String() = line.Split("=")
                    If parts.Length = 2 Then
                        Select Case parts(0).Trim()
                            Case "Port"
                                Try
                                    port = Convert.ToInt32(parts(1).Trim())
                                Catch ex As Exception
                                End Try
                            Case "Audio"
                                If parts(1).Trim().Equals("0") Then
                                    enableaudio = False
                                End If
                            Case "Timeout"
                                Try
                                    idletime = Convert.ToInt32(parts(1).Trim())
                                Catch ex As Exception
                                End Try
                            Case "LogLevel"
                                Try
                                    loglevel = Convert.ToInt32(parts(1).Trim())
                                Catch ex As Exception
                                End Try
                            Case "Resolution"
                                Try
                                    imageresolution = Convert.ToInt32(parts(1).Trim())
                                Catch ex As Exception
                                End Try
                        End Select
                    End If
                Next
            Catch ex As Exception
                log("[Load Config]" & Environment.NewLine & ex.Message)
            End Try
            If enableaudio Then
                _snd = New CoreAudio()
                getsoundthread = New Thread(AddressOf getsoundsamples)
                getsoundthread.IsBackground = False
                getsoundthread.Start()
            End If
            httplistenthread = New Thread(AddressOf ThreadTask)
            httplistenthread.IsBackground = False
            httplistenthread.Start()
            timer = Now
            watchfortimeout = New Thread(AddressOf timerThread)
            watchfortimeout.IsBackground = True
            watchfortimeout.Start()
        End Sub
    End Class

    Sub Main()
        Dim webserver As httpserver = New httpserver()
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
                httpserver.log("[Image Save]" & Environment.NewLine & ex.Message)
            End Try
        End Sub

        Private Shared Sub SaveScaledImage(ByVal strm As MemoryStream, ByVal OldImage As Image, ByVal newWidth As Integer, ByVal newHeight As Integer, ByVal quality As Long)
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
                newImage.Save(strm, GetEncoderInfo(ImageFormat.Jpeg), myEncoderParameters)
                newImage.Dispose()
            Catch ex As Exception
                httpserver.log("[Image Save]" & Environment.NewLine & ex.Message)
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
                httpserver.log("[Screen Capture]" & Environment.NewLine & ex.InnerException.ToString)
            End Try
            Return False
        End Function

        Public Shared Function getScreenshot(ByVal width As Integer, ByVal height As Integer, ByVal quality As Long) As Byte()
            Try
                Dim scap As ScreenCapture = New ScreenCapture()
                Dim img As Image = scap.CaptureScreen()
                Dim converter As New ImageConverter
                Dim ms = New MemoryStream()
                SaveScaledImage(ms, img, width, height, quality)
                img.Dispose()
                Return ms.GetBuffer
            Catch ex As Exception
                httpserver.log("[getScreenshot]" & Environment.NewLine & ex.InnerException.ToString)
            End Try
            Return New Byte() {}
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
                    httpserver.log("[ImpersonateDesktopUser]" & Environment.NewLine & ex.Message)
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
                httpserver.log("[CreateProcess]" & Environment.NewLine & ex.Message)
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

End Module
