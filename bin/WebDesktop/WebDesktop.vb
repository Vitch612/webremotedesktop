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
        Private enableaudio = True
        Private microphone As Boolean = False
        Private bufferingtime As UShort = 500
        Private idletime As Integer = 10
        Private port As Integer = 8888
        Private imageresolution As Integer = 80
        Private Shared loglevel As Byte = 1
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
            Public startpos As Long
            Public lastpos As Long
            Public Sub New(ByVal _userid As String, ByVal startingpos As Long)
                userid = _userid
                SyncLock locksync
                    startpos = startingpos
                End SyncLock
                lastpos = startpos
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

        Private Function getrandomstring() As String
            Dim s As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
            Dim r As New Random
            Dim sb As New StringBuilder
            For i As Integer = 1 To 8
                Dim idx As Integer = r.Next(0, 35)
                sb.Append(s.Substring(idx, 1))
            Next
            Return sb.ToString()
        End Function

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

        Private Function serveaudio(ByRef response As HttpListenerResponse, ByRef outstream As Stream, ByRef context As HttpListenerContext, ByVal user As Integer) As Boolean
            Dim max As Long = 9223372036854775807
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
            If sessions.Item(user).startpos = 0 Then
                response.StatusCode = 503
                response.StatusDescription = "Service Unavailable"
                'response.AddHeader("Retry-After", "1")
                'loginfo(New String(,) {{"Request Refused 503", ""}, _
                '                       {"User Stream Start", sessions.Item(user).startpos}, _
                '                       {"Cache Length", memcache.Length}})
            Else
                Dim reqrange As String = context.Request.Headers.Item("Range")
                Dim reqaccept As String = context.Request.Headers.Item("Accept")
                If reqrange Is Nothing And reqaccept Is Nothing Then
                    Dim buffer As Byte() = getbuffer(sessions.Item(user).startpos, memcache.Length)
                    Dim tmpbuffer As Byte() = New Byte(buffer.Length + mp3header.Length - 1) {}
                    mp3header.CopyTo(tmpbuffer, 0)
                    buffer.CopyTo(tmpbuffer, mp3header.Length)
                    response.StatusCode = 200
                    response.StatusDescription = "OK"
                    response.KeepAlive = True
                    response.ContentType = "audio/mpeg"
                    'response.AddHeader("Content-Disposition", "attachment; filename=""audio.mp3""")
                    sessions.Item(user).lastpos = sessions.Item(user).startpos + buffer.Length
                    outstream.Write(tmpbuffer, 0, tmpbuffer.Length)
                    loginfo(New String(,) {{"Normal Response", ""}, _
                                           {"userid ", user}, _
                                           {"readfrom", sessions.Item(user).startpos}, _
                                           {"bytesread", buffer.Length}, _
                                           {"currentbuffer", memcache.Length}, _
                                           {"lastpos", sessions.Item(user).lastpos}}, EventLogEntryType.Information)
                Else
                    Dim bytefroms As String = "NA"
                    Dim bytetos As String = "NA"
                    Dim bytefrom As Long = 0
                    Dim byteto As Long = 0
                    Dim refuseranged As Boolean = False
                    Dim requestedbytes As Long = -1
                    If Not reqrange Is Nothing Then
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
                            bytetos = reqrange.Substring(reqrange.LastIndexOf("-") + 1, reqrange.IndexOf("/") - reqrange.LastIndexOf("-") - 1)
                        Else
                            bytetos = reqrange.Substring(reqrange.LastIndexOf("-") + 1)
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
                        If byteto = 0 Then
                            If Not reqaccept Is Nothing Then
                                If reqaccept.Equals("*/*") Then
                                    byteto = -1
                                End If
                            End If
                        End If
                    Else
                        bytefrom = 0
                        byteto = -1
                    End If
                    If byteto <> -1 Then
                        If bytefrom = 0 Then
                            requestedbytes = byteto - bytefrom + 1 - mp3header.Length
                        Else
                            requestedbytes = byteto - bytefrom + 1
                        End If
                    End If
                    If bytefrom <> 0 Then
                        bytefrom -= mp3header.Length
                    End If
                    Dim mp3buffer As Byte() = New Byte() {}
                    If Not refuseranged Then
                        If byteto = -1 Then
                            loginfo(New String(,) {{"Ranged - no end", ""}, _
                                                   {"userid ", user}, _
                                                   {"readfrom", sessions.Item(user).startpos + bytefrom}, _
                                                   {"bytesread", (memcache.Length - sessions.Item(user).startpos - bytefrom)}, _
                                                   {"currentbuffer", memcache.Length}})
                            If memcache.Length - sessions.Item(user).startpos - bytefrom > 0 Then
                                mp3buffer = getbuffer(sessions.Item(user).startpos + bytefrom, memcache.Length - sessions.Item(user).startpos - bytefrom)
                            Else
                                Return False
                            End If
                        ElseIf requestedbytes > 0 Then
                            If memcache.Length - sessions.Item(user).startpos - bytefrom > requestedbytes Then
                                loginfo(New String(,) {{"Ranged - with end - can send requested", ""}, _
                                                       {"userid ", user}, _
                                                       {"readfrom", sessions.Item(user).startpos + bytefrom}, _
                                                       {"bytesread", requestedbytes}, _
                                                       {"currentbuffer", memcache.Length}}, EventLogEntryType.Warning)
                                mp3buffer = getbuffer(sessions.Item(user).startpos + bytefrom, requestedbytes)
                            Else
                                loginfo(New String(,) {{"Ranged - with end - cannot send requested", ""}, _
                                                       {"userid ", user}, _
                                                       {"readfrom", sessions.Item(user).startpos + bytefrom}, _
                                                       {"bytesread", memcache.Length - sessions.Item(user).startpos - bytefrom}, _
                                                       {"currentbuffer", memcache.Length}}, EventLogEntryType.Warning)
                                If memcache.Length - sessions.Item(user).startpos - bytefrom > 0 Then
                                    mp3buffer = getbuffer(sessions.Item(user).startpos + bytefrom, memcache.Length - sessions.Item(user).startpos - bytefrom)
                                Else
                                    Return False
                                End If
                            End If
                        ElseIf requestedbytes < 0 Then
                            refuseranged = True
                        End If
                    End If

                    Dim datalength As Long = If(bytefrom = 0, mp3header.Length + mp3buffer.Length, mp3buffer.Length)
                    If bytefrom <> 0 Then
                        bytefrom += mp3header.Length
                    End If
                    If bytefrom = 0 And byteto <> -1 And byteto + 1 < mp3header.Length Then
                        refuseranged = True
                    End If
                    If Not refuseranged Then
                        If datalength > 0 Then
                            response.StatusCode = 206
                            response.KeepAlive = True
                            response.StatusDescription = "Partial content"
                            response.AddHeader("Content-Type", "audio/mpeg")
                            response.AddHeader("Content-Range", "bytes " & bytefrom & "-" & (datalength + bytefrom - 1) & "/" & max)
                            If bytefrom = 0 Then
                                Dim buffer As Byte() = New Byte(mp3header.Length + mp3buffer.Length - 1) {}
                                mp3header.CopyTo(buffer, 0)
                                mp3buffer.CopyTo(buffer, mp3header.Length)
                                sessions.Item(user).lastpos = sessions.Item(user).startpos + bytefrom + mp3buffer.Length
                                outstream.Write(buffer, 0, buffer.Length)
                            Else
                                sessions.Item(user).lastpos = sessions.Item(user).startpos + bytefrom + mp3buffer.Length
                                outstream.Write(mp3buffer, 0, mp3buffer.Length)
                            End If
                            loginfo(New String(,) {{"Ranged Response", ""}, _
                                                   {"userid", user}, _
                                                   {"range served", bytefrom & "-" & (datalength + bytefrom - 1) & "/" & max}, _
                                                   {"bytes sent", mp3buffer.Length}, _
                                                   {"lastpos", sessions.Item(user).lastpos}})
                        Else
                            Return False
                        End If
                    Else
                        response.StatusCode = 416
                        response.StatusDescription = "Requested range not satisfiable"
                        response.AddHeader("Content-Range", "bytes */" & (memcache.Length - sessions.Item(user).startpos + mp3header.Length - 1))
                        loginfo(New String(,) {{"Ranged Refused", ""}, _
                                               {"userid", user}, _
                                               {"range suggested", "bytes */" & (memcache.Length - sessions.Item(user).startpos + mp3header.Length - 1)}}, EventLogEntryType.Warning)
                    End If
                End If
            End If
            Return True
        End Function


        Private Sub FileServeThread(ByVal context As HttpListenerContext)
            timer = Now
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
                response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate")
                response.AddHeader("Pragma", "no-cache")
                response.AddHeader("Expires", "0")
                If context.Request.RawUrl.IndexOf("/audio.mp3") >= 0 And enableaudio Then
                    While Not serveaudio(response, outstream, context, user)
                        Thread.Sleep(10)
                        user = FindUser(userid)
                    End While
                ElseIf context.Request.RawUrl.Equals("/audio.html") Or context.Request.RawUrl.Equals("/cursor.png") Or context.Request.RawUrl.Equals("/favicon.ico") Or context.Request.RawUrl.Equals("/jquery.min.js") Or context.Request.RawUrl.Equals("/styles.css") Or context.Request.RawUrl.Equals("/close.png") Or context.Request.RawUrl.Equals("/settings.png") Or context.Request.RawUrl.Equals("/interactions.js") Or context.Request.RawUrl.Equals("/audio.js") Then
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
                    If microphone Then
                        index = index.Replace("{microphone}", "checked")
                    Else
                        index = index.Replace("{microphone}", "")
                    End If
                    index = index.Replace("{timeout}", idletime)
                    index = index.Replace("{port}", port)
                    index = index.Replace("{resolution}", imageresolution)
                    index = index.Replace("{loglevel}", loglevel)
                    index = index.Replace("{buffering}", bufferingtime)
                    index = index.Replace("{randomstring}", getrandomstring())
                    Dim buffer As Byte() = System.Text.Encoding.ASCII.GetBytes(index)
                    response.StatusCode = 200
                    response.StatusDescription = "OK"
                    response.ContentType = "text/html"
                    sessions.Item(user).startpos = getstartingposition()
                    outstream.Write(buffer, 0, buffer.Length)
                ElseIf context.Request.RawUrl.Equals("/resetposition") Then
                    sessions.Item(user).startpos = sessions.Item(user).lastpos
                    Dim buffer As Byte() = System.Text.Encoding.ASCII.GetBytes(getrandomstring())
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
                            Dim config As String()
                            If Not System.IO.File.Exists(filePath) Then
                                File.Create(filePath).Close()
                            End If
                            config = File.ReadAllLines(filePath)
                            For l As Integer = 0 To config.Length - 1
                                For i As Integer = 0 To context.Request.QueryString.Count - 1
                                    If config(l).IndexOf(context.Request.QueryString.Keys.Item(i)) <> -1 Then
                                        saved(i) = True
                                        If context.Request.QueryString.Keys.Item(i).Equals("Audio") Or context.Request.QueryString.Keys.Item(i).Equals("Microphone") Then
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
                                    If context.Request.QueryString.Keys.Item(i).Equals("Audio") Or context.Request.QueryString.Keys.Item(i).Equals("Microphone") Then
                                        config(config.Length - 1) = context.Request.QueryString.Keys.Item(i) & "=" & If(context.Request.QueryString.Item(i).ToLower().Equals("true"), 1, 0)
                                    Else
                                        config(config.Length - 1) = context.Request.QueryString.Keys.Item(i) & "=" & context.Request.QueryString.Item(i)
                                    End If
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
                If Not ex.Message.Equals("The specified network name is no longer available") And Not ex.Message.Equals("An operation was attempted on a nonexistent network connection") And Not ex.Message.Equals("The I/O operation has been aborted because of either a thread exit or an application request") Then
                    log("[FileServeThread]" & Environment.NewLine & ex.Message & Environment.NewLine & ex.Source & Environment.NewLine & ex.StackTrace, EventLogEntryType.Error)
                End If
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
                If System.IO.File.Exists(filePath) Then
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
                                Case "Microphone"
                                    If parts(1).Trim().Equals("1") Then
                                        microphone = True
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
                                Case "Buffering"
                                    Try
                                        bufferingtime = Convert.ToInt32(parts(1).Trim())
                                    Catch ex As Exception
                                    End Try
                            End Select
                        End If
                    Next
                End If
            Catch ex As Exception
                log("[Load Config]" & Environment.NewLine & ex.Message)
            End Try
            If enableaudio Then
                Try
                    _snd = New CoreAudio(microphone)
                Catch ex As Exception
                    log("[Init Sound]" & Environment.NewLine & ex.Message & Environment.NewLine & ex.StackTrace)
                End Try
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
