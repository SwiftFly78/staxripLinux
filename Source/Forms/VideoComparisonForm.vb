
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

    'videos with different frame rates are compared at the same time instead of the same frame number
    Private SyncByTime As Boolean = s.Storage.GetBool("video comparison sync by time")
    Private miSyncByTime As MenuItemEx
    Private LastTab As VideoTab

    'the name, frame number, time and picture type shown on the videos and on saved PNGs
    Property ShowFrameInfo As Boolean = s.Storage.GetBool("video comparison frame info")
    Private miFrameInfo As MenuItemEx

    'the selected video left of a line that can be dragged and the next video right of it
    Property SplitView As Boolean
    Private miSplitView As MenuItemEx

    Property ZoomActualSize As Boolean
    Private miZoom As MenuItemEx

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
        miFrameInfo = Menu.Add("Frame Info", AddressOf ToggleFrameInfo, Keys.I, "Shows the name, frame number, time and picture type on the videos and on saved PNGs.")
        miFrameInfo.Marked = ShowFrameInfo
        miSplitView = Menu.Add("Split View", AddressOf ToggleSplitView, Keys.V, "Shows the selected video left and the next video right of a line, which can be dragged with the mouse.")
        miZoom = Menu.Add("Zoom 100%", AddressOf ToggleZoom, Keys.Z, "Shows the videos with a screen pixel for each video pixel, dragging moves them, in the split view with the middle mouse button.")
        Menu.Add("Crop...", AddressOf CropVideos, Keys.C, "Crops the selected video or all videos.")
        Menu.Add("Go To Frame...", AddressOf GoToFrame, Keys.F, enabledFunc)
        Menu.Add("Go To Time...", AddressOf GoToTime, Keys.T, enabledFunc)
        miSyncByTime = Menu.Add("Synchronize by Time", AddressOf ToggleSyncByTime, "Switching the tab keeps the time instead of the frame number, for videos with different frame rates.")
        miSyncByTime.Marked = SyncByTime
        Menu.Add("Aspect Ratio...", AddressOf SetAspectRatio, Keys.A, enabledFunc, "Display aspect ratio of the selected video, by default it's taken from the sample aspect ratio of the video.")
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
            Dim previousTab = LastTab
            Dim frame = Pos
            LastTab = tab
            UpdateSplitView()

            'the time of the previous tab, before the new maximum can change the position
            If SyncByTime AndAlso previousTab IsNot Nothing AndAlso previousTab IsNot tab AndAlso previousTab.Server IsNot Nothing Then
                frame = tab.GetFrame(previousTab.GetTime(Pos))
            End If

            UpdateTrackBarMaximum()

            If frame <> TrackBar.Value Then
                TrackBar.Value = Math.Min(frame, TrackBar.Maximum)
                Exit Sub
            End If

            tab.TrackBarValueChanged()
        End If
    End Sub

    'with time synchronization the track bar has the frames of the selected video, else of the longest
    Sub UpdateTrackBarMaximum()
        Dim tabs = TabControl.TabPages.OfType(Of VideoTab).Where(Function(i) i.Server IsNot Nothing)
        Dim selected = TryCast(TabControl.SelectedTab, VideoTab)

        If SyncByTime AndAlso selected?.Server IsNot Nothing Then
            TrackBar.Maximum = Math.Max(1, selected.Server.Info.FrameCount - 1)
        ElseIf tabs.Any() Then
            TrackBar.Maximum = Math.Max(1, tabs.Max(Function(i) i.Server.Info.FrameCount - 1))
        End If
    End Sub

    Sub ToggleZoom()
        ZoomActualSize = Not ZoomActualSize
        miZoom.Marked = ZoomActualSize

        For Each i In TabControl.TabPages.OfType(Of VideoTab)
            i.UpdateZoom()
        Next
    End Sub

    Sub ToggleSplitView()
        SplitView = Not SplitView
        miSplitView.Marked = SplitView
        UpdateSplitView()
    End Sub

    'the next video is shown in the split view of the selected video
    Sub UpdateSplitView()
        Dim tabs = TabControl.TabPages.OfType(Of VideoTab).ToList()
        Dim selected = TryCast(TabControl.SelectedTab, VideoTab)

        For Each i In tabs
            i.SetSplitTab(Nothing)
        Next

        If SplitView AndAlso selected IsNot Nothing AndAlso tabs.Count > 1 Then
            selected.SetSplitTab(tabs((tabs.IndexOf(selected) + 1) Mod tabs.Count))
        End If

        selected?.TrackBarValueChanged()
    End Sub

    'the frame of a video at the position of the selected video
    Function GetTabPosition(tab As VideoTab) As Integer
        Dim selected = TryCast(TabControl.SelectedTab, VideoTab)
        Dim ret = Pos

        If SyncByTime AndAlso selected IsNot Nothing AndAlso tab IsNot selected AndAlso selected.Server IsNot Nothing Then
            ret = tab.GetFrame(selected.GetTime(Pos))
        End If

        Return Math.Max(0, Math.Min(ret, If(tab.Server Is Nothing, 0, tab.Server.Info.FrameCount - 1)))
    End Function

    Sub ToggleFrameInfo()
        ShowFrameInfo = Not ShowFrameInfo
        s.Storage.SetBool("video comparison frame info", ShowFrameInfo)
        miFrameInfo.Marked = ShowFrameInfo

        For Each i In TabControl.TabPages.OfType(Of VideoTab)
            i.UpdateFrameInfo()
        Next
    End Sub

    Sub ToggleSyncByTime()
        SyncByTime = Not SyncByTime
        s.Storage.SetBool("video comparison sync by time", SyncByTime)
        miSyncByTime.Marked = SyncByTime
        UpdateTrackBarMaximum()
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
        tab.SetCrop({CropLeft, CropTop, CropRight, CropBottom})
        tab.VideoPanel.ContextMenu = Menu

        If tab.Open(sourcePath) Then
            TabControl.TabPages.Add(tab)
            TabControl.SelectedTab = tab
            UpdateTrackBarMaximum()
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
            If LastTab Is tab Then LastTab = Nothing
            TabControl.TabPages.Remove(tab)
            tab.CloseServer()
            UpdateTrackBarMaximum()
            UpdateSplitView()
        End If
    End Sub

    Sub Save()
        For Each tab As VideoTab In TabControl.TabPages
            Dim outputPath = ""
            Dim frame = GetTabPosition(tab)

            Select Case s.SaveImageVideoComparisonFrameNumberPosition
                Case ImageFrameNumberPosition.Prefix
                    outputPath = Path.Combine(tab.SourceFilePath.Dir, $"{frame:00000}_{tab.SourceFilePath.Base}.png")
                Case ImageFrameNumberPosition.Suffix
                    outputPath = Path.Combine(tab.SourceFilePath.Dir, $"{tab.SourceFilePath.Base}_{frame:00000}.png")
                Case Else
                    Throw New NotSupportedException("VideoComparisonForm.Save()")
            End Select

            Using bmp = tab.GetBitmap(frame)
                If bmp Is Nothing Then Continue For

                If ShowFrameInfo Then
                    Using gx As New Graphics(bmp)
                        Dim font = FontManager.GetCodeFont(Math.Max(8, bmp.Height / 60.0F) - FontManager.SystemFontSize)
                        VideoRenderer.DrawInfoText(gx, tab.GetFrameInfoText(frame), font)
                    End Using
                End If

                ImageHelp.Save(bmp, outputPath, ImageFormat.Png)
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

        UpdateSplitView()
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

    Sub CropVideos()
        Dim tab = TryCast(TabControl.SelectedTab, VideoTab)
        Dim cropAll = tab Is Nothing OrElse s.Storage.GetBool("video comparison crop all", True)
        Dim values = If(tab Is Nothing, {CropLeft, CropTop, CropRight, CropBottom}, {tab.CropLeft, tab.CropTop, tab.CropRight, tab.CropBottom})

        Using form As New SimpleSettingsForm("Crop")
            form.ScaleClientSize(24, 14)
            Dim ui = form.SimpleUI
            Dim page = ui.CreateFlowPage("main page")
            Dim labels = {"Crop Left:", "Crop Top:", "Crop Right:", "Crop Bottom:"}

            For i = 0 To 3
                Dim index = i
                Dim nb = ui.AddNum(page)
                nb.Label.Text = labels(i)
                nb.NumEdit.Config = {0, 10000, 10}
                nb.NumEdit.Value = values(i)
                nb.NumEdit.SaveAction = Sub(value) values(index) = CInt(value)
            Next

            Dim cb = ui.AddBool(page)
            cb.Text = "Crop all videos"
            cb.Checked = cropAll
            cb.Enabled = tab IsNot Nothing
            cb.SaveAction = Sub(value) cropAll = value

            If form.ShowDialog() = DialogResult.OK Then
                ui.Save()

                If tab IsNot Nothing Then
                    s.Storage.SetBool("video comparison crop all", cropAll)
                End If

                If cropAll Then
                    CropLeft = values(0)
                    CropTop = values(1)
                    CropRight = values(2)
                    CropBottom = values(3)

                    For Each i In TabControl.TabPages.OfType(Of VideoTab)
                        i.SetCrop(values)
                    Next

                    Reload()
                Else
                    tab.SetCrop(values)
                    tab.Reload()
                    UpdateSplitView()
                End If

                TrackBarValueChanged()
            End If
        End Using
    End Sub

    Sub SetAspectRatio()
        Dim tab = TryCast(TabControl.SelectedTab, VideoTab)
        If tab Is Nothing Then Exit Sub

        Dim current = If(tab.DisplayAspectRatio > 0, tab.DisplayAspectRatio.ToInvariantString("0.######"), "")
        Dim value = InputBox.Show("Aspect Ratio", current, "Display aspect ratio of the selected video, like 16:9 or 2.35." + BR +
                                  "Empty uses the sample aspect ratio of the video.")
        If value Is Nothing Then Exit Sub

        Dim ar = Calc.ParseCustomAR(value.Trim(), 0, 0)
        tab.DisplayAspectRatio = If(ar.X > 0 AndAlso ar.Y > 0, ar.X / ar.Y, 0)
        tab.TrackBarValueChanged()
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
        d = d.AddSeconds(tab.GetTime(Pos))
        Dim value = InputBox.Show("Go To Time", d.ToString("HH:mm:ss.fff"))
        Dim time As TimeSpan

        If value <> "" AndAlso TimeSpan.TryParse(value, time) Then
            TrackBar.Value = Math.Min(tab.GetFrame(time.TotalSeconds), TrackBar.Maximum)
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

        Property Renderer As VideoRenderer
        Private FrameInfo As String()
        Private DisplayAspectRatioValue As Double

        Property CropLeft As Integer
        Property CropTop As Integer
        Property CropRight As Integer
        Property CropBottom As Integer

        Sub SetCrop(values As Integer())
            CropLeft = values(0)
            CropTop = values(1)
            CropRight = values(2)
            CropBottom = values(3)
        End Sub

        'display aspect ratio that replaces the one of the video, 0 uses the sample aspect ratio
        Property DisplayAspectRatio As Double
            Get
                Return DisplayAspectRatioValue
            End Get
            Set(value As Double)
                DisplayAspectRatioValue = value
                If Renderer IsNot Nothing Then Renderer.DisplayAspectRatio = value
            End Set
        End Property

        Private SplitTab As VideoTab

        Sub New()
            VideoPanel = New Drawable
            Content = VideoPanel

            AddHandler VideoPanel.MouseDown, AddressOf VideoPanelMouseDown
            AddHandler VideoPanel.MouseMove, AddressOf VideoPanelMouseMove
            AddHandler VideoPanel.MouseUp, AddressOf VideoPanelMouseUp
        End Sub

        Sub SetSplitTab(tab As VideoTab)
            SplitTab = tab

            If Renderer IsNot Nothing Then
                Renderer.SplitRenderer = tab?.Renderer
            End If
        End Sub

        Private DragStart As PointF
        Private DragOffset As PointF

        Sub UpdateZoom()
            If Renderer Is Nothing Then Exit Sub
            Renderer.Zoom = If(Form.ZoomActualSize, 1, 0)
            Renderer.Offset = PointF.Empty
            Renderer.Draw()
        End Sub

        Private Panning As Boolean

        'dragging moves a zoomed video, in the split view with the middle button
        Sub VideoPanelMouseDown(sender As Object, e As MouseEventArgs)
            If Renderer Is Nothing Then Exit Sub

            Panning = Renderer.Zoom > 0 AndAlso
                ((SplitTab Is Nothing AndAlso e.Buttons = MouseButtons.Primary) OrElse e.Buttons = MouseButtons.Middle)

            If Panning Then
                DragStart = e.Location
                DragOffset = Renderer.Offset
                e.Handled = True
            Else
                MoveSplitLine(e)
            End If
        End Sub

        Sub VideoPanelMouseMove(sender As Object, e As MouseEventArgs)
            If Renderer Is Nothing OrElse e.Buttons = MouseButtons.None Then Exit Sub

            If Panning Then
                Renderer.Offset = DragOffset + (e.Location - DragStart)
                Renderer.Draw()
                e.Handled = True
            Else
                MoveSplitLine(e)
            End If
        End Sub

        Sub VideoPanelMouseUp(sender As Object, e As MouseEventArgs)
            Panning = False
        End Sub

        'the line of the split view is moved with the left mouse button
        Sub MoveSplitLine(e As MouseEventArgs)
            If SplitTab Is Nothing OrElse e.Buttons <> MouseButtons.Primary Then Exit Sub

            Dim target = Renderer.GetTargetRect()

            If target.Width > 0 Then
                Renderer.SplitPosition = CSng(Math.Max(0, Math.Min(1, (e.Location.X - target.X) / target.Width)))
                Renderer.Draw()
            End If

            e.Handled = True
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

        ReadOnly Property FrameRate As Double
            Get
                Return If(Server IsNot Nothing AndAlso Calc.IsValidFrameRate(Server.FrameRate), Server.FrameRate, 25)
            End Get
        End Property

        Sub UpdateFrameInfo()
            If Renderer Is Nothing Then Exit Sub
            Renderer.ShowInfo = Form.ShowFrameInfo
            Renderer.Draw()
        End Sub

        'the properties are the ones of the last frame of the server, the frame at the position
        Function GetFrameInfoText(frame As Integer) As String
            Dim pictType As String = Nothing
            Server?.FrameProperties?.TryGetValue("_PictType", pictType)
            Dim time = Date.Today.AddSeconds(GetTime(frame)).ToString("HH:mm:ss.fff")
            Return SourceFilePath.FileName + BR + $"Frame {frame}, {time}" + If(pictType <> "", $", {pictType} frame", "")
        End Function

        Function GetTime(frame As Integer) As Double
            Return frame / FrameRate
        End Function

        Function GetFrame(seconds As Double) As Integer
            Return Math.Max(0, CInt(Math.Round(seconds * FrameRate)))
        End Function

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
                "clip = core.std.Crop(clip, " & CropLeft & ", " & CropRight & ", " & CropTop & ", " & CropBottom & ")",
                "Crop(" & CropLeft & ", " & CropTop & ", -" & CropRight & ", -" & CropBottom & ")")

            Select Case FileType
                Case VideoComparisonFileType.Picture, VideoComparisonFileType.Video
                    If (CropLeft Or CropTop Or CropRight Or CropBottom) <> 0 Then
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
            Renderer = New VideoRenderer(VideoPanel, Server) With {
                .KeepAspectRatio = True,
                .UseSampleAspectRatio = True,
                .DisplayAspectRatio = DisplayAspectRatio,
                .ShowInfo = Form.ShowFrameInfo,
                .Zoom = If(Form.ZoomActualSize, 1, 0),
                .Title = SourceFilePath.FileName}

            Renderer.InfoTextFunc = Function() GetFrameInfoText(Renderer.Position)
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

            If SplitTab?.Renderer IsNot Nothing Then
                SplitTab.Renderer.Position = Form.GetTabPosition(SplitTab)
            End If

            Renderer.Draw()
        End Sub

        Function GetBitmap(frame As Integer) As Bitmap
            Return FrameHelp.CreateBitmap(Server, frame)
        End Function

        Sub TrackBarValueChanged()
            Try
                Pos = Form.TrackBar.Value
                Draw()

                'the bitmap has the sample aspect ratio of the frame
                Renderer?.GetBitmap()

                If FrameInfo IsNot Nothing Then
                    Form.laInfo.Text = FrameInfo(Form.TrackBar.Value)
                Else
                    Dim dt = DateTime.Today.AddSeconds(GetTime(Pos))
                    Form.laInfo.Text = $"Position: {Pos,5}, Time: {dt:HH:mm:ss.fff}, Size: {Server.Info.Width} x {Server.Info.Height}, " +
                        $"DAR: {Renderer.VideoAspectRatio.ToInvariantString("0.###")}" +
                        If((CropLeft Or CropTop Or CropRight Or CropBottom) <> 0, $", Crop: {CropLeft},{CropTop},{CropRight},{CropBottom}", "")
                End If

            Catch ex As Exception
                g.ShowException(ex)
            End Try
        End Sub

    End Class
End Class
