
Imports System.Text

Imports StaxRip.UI

Public Class VideoComparisonForm
    Inherits FormBase

    Public WithEvents TabControl As TabControlEx
    Public WithEvents TrackBar As TrackBarEx
    Public laFilePath As LabelEx
    Public laInfo As LabelEx
    Public WithEvents bnMenu As ButtonEx

    Shared Property Pos As Integer

    Private ShowMessageSavedPngs As Boolean = True
    Public CropLeft, CropTop, CropRight, CropBottom As Integer

    Shadows Menu As ContextMenuStripEx

    Sub New()
        TabControl = New TabControlEx
        TrackBar = New TrackBarEx With {.Maximum = 1}
        laInfo = New LabelEx With {.Font = FontManager.GetCodeFont(-1)}
        laFilePath = New LabelEx
        Menu = New ContextMenuStripEx With {.Form = Me}
        bnMenu = New ButtonEx With {.ShowMenuSymbol = True, .ContextMenuStrip = Menu}

        Content = PaddedTable(
            ScaledRow(TabControl),
            Row(TrackBar),
            Row(New TableLayout(Row(bnMenu, Scaled(laInfo))) With {.Spacing = DefaultSpacing}),
            Row(laFilePath))

        Text = $"Video Comparison - {g.DefaultCommands.GetApplicationDetails()}"
        RestoreClientSize(53, 36)
        FileDrop = True
        TabControl.ContextMenu = Menu

        Dim enabledFunc = Function() TabControl.SelectedTab IsNot Nothing

        Menu.Add("Add files to compare...", AddressOf Add, Keys.O, "Video files to compare, the file browser has multiselect enabled.")
        Menu.Add("Close selected tab", AddressOf RemoveTab, Keys.Delete, enabledFunc)
        Menu.Add("Select next tab", AddressOf NextTab, Keys.Space, enabledFunc)
        Menu.Add("Save PNGs at current position", AddressOf Save, Keys.S, enabledFunc, "Saves a PNG image for every file/tab at the current position in the directory of the source file.")
        Menu.Add("Crop and Zoom...", AddressOf CropZoom, Keys.C)
        Menu.Add("Go To Frame...", AddressOf GoToFrame, Keys.F, enabledFunc)
        Menu.Add("Go To Time...", AddressOf GoToTime, Keys.T, enabledFunc)
        Menu.Add("Navigate | 1 frame backward", Sub() TrackBar.Value -= 1, Keys.Left, enabledFunc)
        Menu.Add("Navigate | 1 frame forward", Sub() TrackBar.Value += 1, Keys.Right, enabledFunc)
        Menu.Add("Navigate | 100 frame backward", Sub() TrackBar.Value -= 100, Keys.Left Or Keys.Control, enabledFunc)
        Menu.Add("Navigate | 100 frame forward", Sub() TrackBar.Value += 100, Keys.Right Or Keys.Control, enabledFunc)
        Menu.Add("Reload all tabs", AddressOf Reload, Keys.R, enabledFunc)
        Menu.Add("Help", AddressOf Help, Keys.F1)

        AddHandler TabControl.SelectedIndexChanged, AddressOf TabControlOnSelected
    End Sub

    Sub VideoComparisonForm_FilesDropped(files As String()) Handles Me.FilesDropped
        BeginInvoke(Sub() Add(files))
    End Sub

    Sub TabControlOnSelected(sender As Object, e As EventArgs)
        Dim tab = TryCast(TabControl.SelectedTab, VideoTab)

        If tab IsNot Nothing Then
            laFilePath.Text = tab.SourceFilePath
            tab.TrackBarValueChanged()
        End If
    End Sub

    'the comparison uses the engine of the project if available
    Shared Function GetEngine() As ScriptEngine
        If p.Script?.Engine = ScriptEngine.AviSynth AndAlso Package.AviSynth.Path <> "" Then
            Return ScriptEngine.AviSynth
        End If

        If Package.VapourSynth.Path <> "" Then
            Return ScriptEngine.VapourSynth
        End If

        Return ScriptEngine.AviSynth
    End Function

    Sub Add()
        If GetEngine() = ScriptEngine.AviSynth AndAlso Not Package.AviSynth.VerifyOK(True) Then Exit Sub

        Dim sourcePath = p.SourceFile
        Dim sourceAlreadyOpen = TabControl.TabPages.OfType(Of VideoTab).Any(Function(x) x.SourceFilePath = sourcePath)

        If Not sourceAlreadyOpen AndAlso File.Exists(sourcePath) Then
            Add(sourcePath)
            If File.Exists(p.TargetFile) Then Add(p.TargetFile)
        Else
            Using dialog As New OpenFileDialog
                dialog.SetFilter(FileTypes.VideoComparisonInput)
                dialog.Multiselect = True
                dialog.SetInitDir(s.Storage.GetString("video comparison folder"))

                If dialog.ShowDialog() = DialogResult.OK Then
                    s.Storage.SetString("video comparison folder", dialog.FileName.Dir)
                    Add(dialog.FileNames)
                End If
            End Using
        End If

    End Sub

    Sub Add(sourcePath As String)
        Dim tab = New VideoTab With {.Form = Me}
        tab.VideoPanel.ContextMenu = Menu

        If tab.Open(sourcePath) Then
            TabControl.TabPages.Add(tab)
            TabControl.SelectedTab = tab
            tab.TrackBarValueChanged()
        Else
            tab.CloseServer()
        End If

        Dim vt = TryCast(TabControl.SelectedTab, VideoTab)
        laFilePath.Text = vt?.SourceFilePath
    End Sub

    Sub Add(sourcePaths As String())
        For Each path In sourcePaths
            If File.Exists(path) Then
                Add(path)
            End If
        Next
    End Sub

    Sub RemoveTab()
        Dim tab = TryCast(TabControl.SelectedTab, VideoTab)

        If tab IsNot Nothing Then
            TabControl.TabPages.Remove(tab)
            tab.CloseServer()
        End If
    End Sub

    Sub Save()
        For Each tab As VideoTab In TabControl.TabPages
            Dim outputPath = ""

            Select Case s.SaveImageVideoComparisonFrameNumberPosition
                Case ImageFrameNumberPosition.Prefix
                    outputPath = Path.Combine(tab.SourceFilePath.Dir, $"{Pos:00000}_{tab.SourceFilePath.Base}.png")
                Case ImageFrameNumberPosition.Suffix
                    outputPath = Path.Combine(tab.SourceFilePath.Dir, $"{tab.SourceFilePath.Base}_{Pos:00000}.png")
                Case Else
                    Throw New NotSupportedException("VideoComparisonForm.Save()")
            End Select

            Using bmp = tab.GetBitmap
                bmp?.Save(outputPath, ImageFormat.Png)
            End Using
        Next

        If ShowMessageSavedPngs Then
            Using td As New TaskDialog(Of String)
                td.Title = "Images were saved in the source file directory."
                td.AddCommand("OK")
                td.AddCommand("Dismiss this message for this session", "Dismiss")

                Select Case td.Show
                    Case "OK"
                    Case "Dismiss"
                        ShowMessageSavedPngs = False
                End Select
            End Using
        End If
    End Sub

    Sub TrackBar_ValueChanged(sender As Object, e As EventArgs) Handles TrackBar.ValueChanged
        TrackBarValueChanged()
    End Sub

    Sub TrackBarValueChanged()
        If TabControl.TabPages.Count > 0 Then
            TryCast(TabControl.SelectedTab, VideoTab)?.TrackBarValueChanged()
        End If
    End Sub

    Sub Help()
        Dim form As New HelpForm()
        form.Doc.WriteStart(Text)
        form.Doc.WriteParagraph("In the statistic tab of the x265 dialog select Log Level Frame and enable CSV log file creation, the video comparison tool can displays containing frame info.")
        form.Doc.WriteTips(Menu.GetTips)
        form.Doc.WriteTable("Shortcut Keys", Menu.GetKeys, False)
        form.Show()
    End Sub

    Sub NextTab()
        Dim index = TabControl.SelectedIndex + 1

        If index >= TabControl.TabPages.Count Then
            index = 0
        End If

        If index <> TabControl.SelectedIndex Then
            TabControl.SelectedIndex = index
        End If
    End Sub

    Sub Reload()
        For Each tab As VideoTab In TabControl.TabPages
            tab.Reload()
        Next
    End Sub

    Sub OnVideoMouseWheel(sender As Object, e As MouseEventArgs)
        Dim value = 100

        If e.Delta.Height < 0 Then
            value *= -1
        End If

        If s.ReverseVideoScrollDirection Then
            value *= -1
        End If

        TrackBar.Value += value
        e.Handled = True
    End Sub

    Protected Overrides Sub OnClosed(e As EventArgs)
        For Each tab In TabControl.TabPages.OfType(Of VideoTab).ToArray
            tab.CloseServer()
        Next

        MyBase.OnClosed(e)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        Add()
    End Sub

    Sub CropZoom()
        Using form As New SimpleSettingsForm("Crop and Zoom")
            form.ScaleClientSize(24, 20)
            Dim ui = form.SimpleUI
            Dim page = ui.CreateFlowPage("main page")

            Dim nb = ui.AddNum(page)
            nb.Label.Text = "Crop Left:"
            nb.NumEdit.Config = {0, 10000, 10}
            nb.NumEdit.Value = CropLeft
            nb.NumEdit.SaveAction = Sub(value) CropLeft = CInt(value)

            nb = ui.AddNum(page)
            nb.Label.Text = "Crop Top:"
            nb.NumEdit.Config = {0, 10000, 10}
            nb.NumEdit.Value = CropTop
            nb.NumEdit.SaveAction = Sub(value) CropTop = CInt(value)

            nb = ui.AddNum(page)
            nb.Label.Text = "Crop Right:"
            nb.NumEdit.Config = {0, 10000, 10}
            nb.NumEdit.Value = CropRight
            nb.NumEdit.SaveAction = Sub(value) CropRight = CInt(value)

            nb = ui.AddNum(page)
            nb.Label.Text = "Crop Bottom:"
            nb.NumEdit.Config = {0, 10000, 10}
            nb.NumEdit.Value = CropBottom
            nb.NumEdit.SaveAction = Sub(value) CropBottom = CInt(value)

            If form.ShowDialog() = DialogResult.OK Then
                ui.Save()
                Reload()
                TrackBarValueChanged()
            End If
        End Using
    End Sub

    Sub GoToFrame()
        Dim value = InputBox.Show("Go To Frame", TrackBar.Value.ToString)
        Dim pos As Integer

        If Integer.TryParse(value, pos) Then
            TrackBar.Value = pos
        End If
    End Sub

    Sub GoToTime()
        Dim tab = TryCast(TabControl.SelectedTab, VideoTab)
        If tab Is Nothing Then Exit Sub
        Dim d As Date
        d = d.AddSeconds(Pos / tab.Server.FrameRate)
        Dim value = InputBox.Show("Go To Time", d.ToString("HH:mm:ss.fff"))
        Dim time As TimeSpan

        If value <> "" AndAlso TimeSpan.TryParse(value, time) Then
            TrackBar.Value = CInt(time.TotalMilliseconds / 1000 * tab.Server.FrameRate)
        End If
    End Sub

    Public Class VideoTab
        Inherits TabPage

        Property Server As IFrameServer
        Property Form As VideoComparisonForm
        Property FileType As VideoComparisonFileType = VideoComparisonFileType.Video
        Property TempFilePath As String
        Property SourceFilePath As String
        Property VideoPanel As Drawable

        Private Renderer As VideoRenderer
        Private FrameInfo As String()

        Sub New()
            VideoPanel = New Drawable
            Content = VideoPanel
        End Sub

        Sub CloseServer()
            Renderer?.Dispose()
            Renderer = Nothing
            Server?.Dispose()
            Server = Nothing

            If TempFilePath <> "" AndAlso FileType <> VideoComparisonFileType.AviSynthScript AndAlso
                FileType <> VideoComparisonFileType.VapourSynthScript Then

                FileHelp.Delete(TempFilePath)
            End If
        End Sub

        Private Function AdjustName(name As String, Optional lengthLimit As Integer = 50) As String
            If String.IsNullOrWhiteSpace(name) Then Return "<empty>"

            Const replacement = "....."

            If name.Length > lengthLimit Then
                Dim firstHalfLength = lengthLimit \ 2
                Dim secondHalfLength = lengthLimit - firstHalfLength
                Return $"{name.Substring(0, firstHalfLength)}{replacement}{name.Substring(name.Length - secondHalfLength)}"
            End If

            Return name
        End Function

        Sub Reload()
            CloseServer()
            Open(SourceFilePath)
        End Sub

        Function Open(sourcePath As String) As Boolean
            Text = AdjustName(sourcePath.Base)
            ToolTip = sourcePath
            SourceFilePath = sourcePath
            Dim engine = VideoComparisonForm.GetEngine()
            Dim scriptExt = If(engine = ScriptEngine.VapourSynth, ".vpy", ".avs")

            Select Case sourcePath.Ext()
                Case "avs"
                    FileType = VideoComparisonFileType.AviSynthScript
                    TempFilePath = sourcePath.DirAndBase + "_compare" + sourcePath.ExtFull
                Case "vpy"
                    FileType = VideoComparisonFileType.VapourSynthScript
                    TempFilePath = sourcePath.DirAndBase + "_compare" + sourcePath.ExtFull
                Case "png"
                    FileType = VideoComparisonFileType.Picture
                    TempFilePath = Path.Combine(Folder.Temp, Guid.NewGuid.ToString + scriptExt)
                Case Else
                    FileType = VideoComparisonFileType.Video
                    TempFilePath = Path.Combine(Folder.Temp, Guid.NewGuid.ToString + scriptExt)
            End Select

            If FileType = VideoComparisonFileType.AviSynthScript Then
                engine = ScriptEngine.AviSynth
            ElseIf FileType = VideoComparisonFileType.VapourSynthScript Then
                engine = ScriptEngine.VapourSynth
            End If

            Dim script As New VideoScript With {
                .Engine = engine,
                .Path = TempFilePath
            }

            Dim isVS = engine = ScriptEngine.VapourSynth
            Dim pathLiteral = VideoScript.ToScriptString(sourcePath, engine)

            Select Case FileType
                Case VideoComparisonFileType.AviSynthScript
                    script.Filters.Add(New VideoFilter("Source", "Source", File.ReadAllText(sourcePath)))
                Case VideoComparisonFileType.VapourSynthScript
                    script.Filters.Add(New VideoFilter("Source", "Source", File.ReadAllText(sourcePath)))
                Case VideoComparisonFileType.Video
                    If isVS Then
                        script.Filters.Add(New VideoFilter("Source", "Source", "clip = core.ffms2.Source(" + pathLiteral + ")"))
                    ElseIf sourcePath.EndsWith("mp4") Then
                        script.Filters.Add(New VideoFilter("Source", "Source", "LSMASHVideoSource(" + pathLiteral + ", format = ""YUV420P12"")"))
                        script.Filters.Add(New VideoFilter("SetMemoryMax(512)"))
                    Else
                        script.Filters.Add(New VideoFilter("Source", "Source", "FFVideoSource(" + pathLiteral + ", colorspace = ""YV12"")"))
                        script.Filters.Add(New VideoFilter("SetMemoryMax(512)"))
                    End If
                Case VideoComparisonFileType.Picture
                    If isVS Then
                        script.Filters.Add(New VideoFilter("Source", "Source", "clip = core.imwri.Read(" + pathLiteral + ")"))
                    Else
                        script.Filters.Add(New VideoFilter("ImageSource(" + pathLiteral + ", end = 0)"))
                        script.Filters.Add(New VideoFilter("SetMemoryMax(512)"))
                    End If
                Case Else
                    Throw New NotSupportedException("VideoComparisonFileType unhandled!")
            End Select

            Dim cropCode = If(isVS,
                "clip = core.std.Crop(clip, " & Form.CropLeft & ", " & Form.CropRight & ", " & Form.CropTop & ", " & Form.CropBottom & ")",
                "Crop(" & Form.CropLeft & ", " & Form.CropTop & ", -" & Form.CropRight & ", -" & Form.CropBottom & ")")

            Select Case FileType
                Case VideoComparisonFileType.Picture, VideoComparisonFileType.Video
                    If (Form.CropLeft Or Form.CropTop Or Form.CropRight Or Form.CropBottom) <> 0 Then
                        script.Filters.Add(New VideoFilter("Crop", "Crop", cropCode))
                    End If
                Case VideoComparisonFileType.AviSynthScript
                Case VideoComparisonFileType.VapourSynthScript
                Case Else
                    Throw New NotSupportedException("VideoComparisonFileType unhandled!")
            End Select

            Indexing(sourcePath, script)
            script.Synchronize(True, True, True)

            Server = FrameServerFactory.Create(script.Path)
            Renderer = New VideoRenderer(VideoPanel, Server) With {.KeepAspectRatio = True}
            AddHandler VideoPanel.MouseWheel, AddressOf Form.OnVideoMouseWheel
            Dim maximum = Server.Info.FrameCount - 1
            Form.TrackBar.Maximum = If(Form.TrackBar.Maximum < maximum, maximum, Form.TrackBar.Maximum)

            If Server.Error <> "" Then
                MsgError(Server.Error)
                Return False
            End If

            Dim csvFile = sourcePath.DirAndBase + ".csv"

            If File.Exists(csvFile) Then
                Dim len = Form.TrackBar.Maximum
                Dim lines = File.ReadAllLines(csvFile)

                If lines.Length > len + 1 Then
                    FrameInfo = New String(len) {}
                    Dim headers = lines(0).Split({","c})

                    For x = 1 To len + 1
                        Dim values = lines(x).Split({","c})

                        For x2 = 0 To headers.Length - 1
                            Dim value = values(x2).Trim

                            If value <> "" AndAlso value <> "-" Then
                                FrameInfo(x - 1) += headers(x2).Trim + ": " + value + ", "
                            End If
                        Next

                        FrameInfo(x - 1) = FrameInfo(x - 1).TrimEnd(" ,".ToCharArray)
                    Next
                End If
            End If
            Return True
        End Function

        Sub Indexing(sourcePath As String, script As VideoScript)
            If String.IsNullOrWhiteSpace(sourcePath) Then Exit Sub
            If Not File.Exists(sourcePath) Then Exit Sub
            If script Is Nothing Then Exit Sub

            Dim sourceFilter = script.GetFilter("Source")
            If sourceFilter Is Nothing Then Exit Sub

            Dim codeLower = script.GetFilter("Source").Script.ToLowerInvariant

            If codeLower.Contains("ffvideosource(") OrElse codeLower.Contains("ffms2.source") Then
                If codeLower.Contains("cachefile") AndAlso p.TempDir <> "" AndAlso New DirectoryInfo(p.TempDir)?.Name?.EndsWithEx("_temp") Then
                    g.ffmsindex(sourcePath, Path.Combine(p.TempDir, g.GetSourceBase + ".ffindex"))
                Else
                    g.ffmsindex(sourcePath, sourcePath + ".ffindex")
                End If
            ElseIf codeLower.Contains("lwlibavvideosource(") OrElse codeLower.Contains("lwlibavsource(") Then
                If Not File.Exists(sourcePath + ".lwi") AndAlso
                                Not File.Exists(Path.Combine(p.TempDir, g.GetSourceBase + ".lwi")) AndAlso
                                File.Exists(script.Path) AndAlso Not FileTypes.VideoText.Contains(sourcePath.Ext) Then
                    Using proc As New Proc
                        proc.Header = "Index LWLibav"
                        proc.Encoding = Encoding.UTF8
                        proc.SkipString = "Creating lwi"
                        If script.IsAviSynth Then
                            proc.File = Package.ffmpeg.Path
                            proc.Arguments = "-i " + script.Path.Escape + " -hide_banner"
                        Else
                            proc.File = Package.vspipe.Path
                            proc.Arguments = script.Path.Escape + " /dev/null -i"
                        End If

                        proc.AllowedExitCodes = {0, 1}
                        proc.Start()
                    End Using
                End If
            End If
        End Sub

        Sub Draw()
            If Renderer Is Nothing Then Exit Sub
            Renderer.Position = Pos
            Renderer.Draw()
        End Sub

        Function GetBitmap() As Bitmap
            Return FrameHelp.CreateBitmap(Server, Pos)
        End Function

        Sub TrackBarValueChanged()
            Try
                Pos = Form.TrackBar.Value
                Draw()

                If FrameInfo IsNot Nothing Then
                    Form.laInfo.Text = FrameInfo(Form.TrackBar.Value)
                Else
                    Dim frameRate = If(Calc.IsValidFrameRate(Server.FrameRate), Server.FrameRate, 25)
                    Dim dt = DateTime.Today.AddSeconds(Pos / frameRate)
                    Form.laInfo.Text = $"Position: {Pos,5}, Time: {dt:HH:mm:ss.fff}, Size: {Server.Info.Width} x {Server.Info.Height}"
                End If

            Catch ex As Exception
                g.ShowException(ex)
            End Try
        End Sub

    End Class
End Class
