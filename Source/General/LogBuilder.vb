
Imports System.Globalization
Imports System.Text

<Serializable>
Public Class LogBuilder
    Private StartTime As DateTime
    Private Log As New StringBuilder
    Private Last As String

    Sub Append(content As String)
        SyncLock Log
            Log.Append(content)

            If content <> "" Then
                Last = content
            End If
        End SyncLock
    End Sub

    Sub Clear()
        SyncLock Log
            Dim unused = Log.Clear()
        End SyncLock
    End Sub

    Function EndsWith(value As String) As Boolean
        If Last = "" Then
            Return False
        End If

        Return Last.EndsWith(value)
    End Function

    Private Shared WriteLock As New Object

    Sub Write(title As String, content As String)
        SyncLock WriteLock
            StartTime = DateTime.Now

            If Not EndsWith(BR2) Then
                Append(BR)
            End If

            Append(FormatHeader(title))

            If content <> "" Then
                If content.EndsWith(BR) Then
                    Append(content)
                Else
                    Append(content + BR)
                End If
            End If
        End SyncLock
    End Sub

    Sub WriteLine(value As String)
        If value <> "" Then
            If value.EndsWith(BR) Then
                Append(value)
            Else
                Append(value + BR)
            End If
        End If
    End Sub

    ReadOnly Property Length As Integer
        Get
            SyncLock Log
                Return Log.Length
            End SyncLock
        End Get
    End Property

    Sub WriteHeader(value As String)
        If value <> "" Then
            StartTime = DateTime.Now

            If Not EndsWith(BR2) Then
                Append(BR)
            End If

            Append(FormatHeader(value))
        End If
    End Sub

    Function FormatHeader(value As String) As String
        Dim len = (65 - value.Length) \ 2
        Return "--" + "-".Multiply(len) + " " + value + " " + "-".Multiply(len) + "--" + BR2
    End Function

    Shared EnvironmentString As String 'cached due to bug report

    Shared Function GetCpuName() As String
        Try
            For Each line In File.ReadLines("/proc/cpuinfo")
                If line.StartsWith("model name") Then
                    Return line.Right(":").Trim
                End If
            Next
        Catch
        End Try

        Return Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString
    End Function

    Shared Function GetTotalMemory() As Long
        Try
            For Each line In File.ReadLines("/proc/meminfo")
                If line.StartsWith("MemTotal:") Then
                    Return line.Right(":").Replace("kB", "").Trim.ToLong * 1024
                End If
            Next
        Catch
        End Try

        Return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes
    End Function

    Sub WriteEnvironment()
        If ToString.Contains("- System Environment -") Then Exit Sub

        WriteHeader("System Environment")

        If EnvironmentString = "" Then
            Dim resolution = UIInvoke(Function()
                                          Try
                                              Dim bounds = Screen.PrimaryScreen.Bounds
                                              Return CInt(bounds.Width) & " x " & CInt(bounds.Height)
                                          Catch
                                              Return ""
                                          End Try
                                      End Function)

            EnvironmentString =
                AppInfo.ProductName + ":" + g.DefaultCommands.GetApplicationDetails(False, True) + BR +
                $"Settings: v{s.Version.Major}.{s.Version.Minor}.{s.Version.Build}{BR}" +
                $"OS: {OSVersion.VersionString}" + BR +
                ".NET:" + Environment.Version.ToString + BR +
                "Desktop:" + Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") + " (" + Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") + ")" + BR +
                "Language:" + CultureInfo.CurrentCulture.EnglishName + BR +
                "CPU:" + GetCpuName() + BR +
                $"RAM:{GetTotalMemory() / 1024 ^ 3:0}GB" + BR +
                "GPU:" + String.Join(", ", OS.VideoControllers) + BR +
                "Resolution:" & resolution
        End If

        WriteLine(EnvironmentString.FormatColumn(":"))
    End Sub

    Sub WriteConfiguration()
        WriteHeader("Configuration")

        Dim sb = New StringBuilder()
        For i = 1 To p.AudioTracksAvailable
            Dim at = p.AudioTracks(i - 1)
            If Not at.IsRelevant Then Continue For
            Dim text = UIInvoke(Function() at.TextEdit.Text)
            sb.AppendLine($"{i,3}. {at.AudioProfile.Name} ({at.AudioProfile.AudioCodec}) [{at.AudioProfile.Language.Name}]: {text}")
        Next
        Dim audioConfig = sb.ToString()

        Dim configurationString =
            $"Template: {p.TemplateName}{BR}" +
            $"Video Encoder: {p.VideoEncoder.GetType().Name}{BR}" +
            $"Video Encoder Profile: {p.VideoEncoder.Name}{BR}" +
            $"Container/Muxer Profile: {p.VideoEncoder.Muxer.Name}{BR}" +
            $"+++++{BR}" +
            $"Audio Tracks...{BR}" +
            $"{audioConfig}" +
            $"+++++{BR}" +
            $"AviSynth/VapourSynth Mode: {s.AviSynthMode}/{s.VapourSynthMode}{BR}" +
            $"Encoder Process Priority: {s.EncoderProcessPriority}{BR}" +
            $"Tool Process Priority: {s.ToolProcessPriority}{BR}" +
            $"Delete Temp Files: {p.DeleteTempFilesMode}{BR}" + 
            $"Delete Temp Files Selection: {p.DeleteTempFilesSelectionMode}{BR}"

        WriteLine(configurationString.FormatColumn(":"))
    End Sub

    Sub WriteStats()
        WriteStats(StartTime)
    End Sub

    Sub WriteStats(start As DateTime)
        Dim dt = DateTime.Now.Subtract(start)

        If Not EndsWith(BR2) Then Append(BR)
        Append("Start: ".PadRight(10) + start.ToLongTimeString + BR)
        Append("End: ".PadRight(10) + DateTime.Now.ToLongTimeString + BR)
        Append("Duration: " + CInt(Math.Floor(dt.TotalHours)).ToString("d2") + ":" + dt.Minutes.ToString("d2") + ":" + dt.Seconds.ToString("d2") + BR2)
    End Sub

    Function IsEmpty() As Boolean
        SyncLock Log
            Return Log.Length = 0
        End SyncLock
    End Function

    Public Overrides Function ToString() As String
        SyncLock Log
            Return Log.ToString
        End SyncLock
    End Function

    Sub Save(Optional proj As Project = Nothing)
        If proj Is Nothing Then
            proj = p
        End If

        SyncLock Log
            If Log.Length > 0 Then
                Log.ToString.WriteFileUTF8(GetPath(proj))
            End If
        End SyncLock
    End Sub

    Function GetPath(Optional proj As Project = Nothing) As String
        If proj Is Nothing Then
            proj = p
        End If

        If proj.SourceFile = "" Then
            Return Path.Combine(Folder.Temp, "staxrip.log")
        ElseIf proj.TempDir = "" Then
            Return Path.Combine(proj.SourceFile.Dir, proj.SourceFile.FileName + "_staxrip.log")
        Else
            Return Path.Combine(proj.TempDir, proj.TargetFile.Base + "_staxrip.log")
        End If
    End Function
End Class
