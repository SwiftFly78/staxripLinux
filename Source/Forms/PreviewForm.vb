
Imports System.ComponentModel

Imports StaxRip.UI

Public Class PreviewForm
    Inherits FormBase

    Public WithEvents pnVideo As Drawable
    Public WithEvents pnTrack As Drawable
    Private WithEvents bnDelete, bnRight1, bnStartCutRange, bnEndCutRange, bnRight2, bnLeft2, bnMenu, bnRight3, bnLeft3, bnLeft1 As ButtonEx
    Public WithEvents cmsMain As ContextMenuStripEx
    Private ButtonsRow As Control

    Private Sub InitializeComponent()
        Dim createButton = Function(symbol As ButtonEx.ButtonSymbol, tip As String) As ButtonEx
                               Return New ButtonEx With {.SymbolButton = symbol, .ToolTip = tip}
                           End Function

        bnLeft3 = createButton(ButtonEx.ButtonSymbol.Left3, "Backward 100 Frames")
        bnLeft2 = createButton(ButtonEx.ButtonSymbol.Left2, "Backward 10 Frames")
        bnLeft1 = createButton(ButtonEx.ButtonSymbol.Left1, "Backward 1 Frame")
        bnStartCutRange = createButton(ButtonEx.ButtonSymbol.Open, "Sets a start cut point. Press F1 for help about cutting")
        bnEndCutRange = createButton(ButtonEx.ButtonSymbol.Close, "Sets an end cut point. Press F1 for help about cutting")
        bnRight1 = createButton(ButtonEx.ButtonSymbol.Right1, "Forward 1 Frame")
        bnRight2 = createButton(ButtonEx.ButtonSymbol.Right2, "Forward 10 Frames")
        bnRight3 = createButton(ButtonEx.ButtonSymbol.Right3, "Forward 100 Frames")
        bnDelete = createButton(ButtonEx.ButtonSymbol.Delete, "Deletes the cut selection that encloses the current position.")
        bnMenu = createButton(ButtonEx.ButtonSymbol.Menu, "Shows the menu")
        cmsMain = New ContextMenuStripEx
        pnVideo = New Drawable With {.CanFocus = True, .ContextMenu = cmsMain}
        pnTrack = New Drawable With {.Height = FH(1.1)}

        ButtonsRow = New TableLayout(New TableRow(
            New TableCell(Nothing, True),
            New TableCell(bnLeft3), New TableCell(bnLeft2), New TableCell(bnLeft1),
            New TableCell(bnStartCutRange), New TableCell(bnEndCutRange),
            New TableCell(bnRight1), New TableCell(bnRight2), New TableCell(bnRight3),
            New TableCell(bnDelete), New TableCell(bnMenu),
            New TableCell(Nothing, True))) With {.Spacing = New Size(FH(0.2), 0)}

        Content = New TableLayout(
            ScaledRow(pnVideo),
            Row(pnTrack),
            Row(ButtonsRow)) With {.Spacing = New Size(0, FH(0.2))}

        BackgroundColor = Colors.Black
        Text = $"Preview - {g.DefaultCommands.GetApplicationDetails()}"
        MinimumSize = New Size(FH(15), FH(10))
    End Sub

    Shared Property Instances As New List(Of PreviewForm)

    Private FrameServer As IFrameServer
    Private Renderer As VideoRenderer
    Private StartRange As Integer = -1
    Private EndRange As Integer = -1
    Private SourceFilePath As String
    Private PreviewScript As New VideoScript
    Private CommandManager As New CommandManager
    Private TrackBarBorder As Integer = 1
    Private TrackBarGap As Integer = 1
    Private TrackBarPosition As Integer = CInt(FH(1) / 4) - 1
    Private VideoSize As Size
    Private IsFullscreen As Boolean
    Private ShowPreviewInfo As Boolean
    Private HidePreviewButtons As Boolean

    Private WithEvents GenericMenu As CustomMenu

    Sub New(ByRef script As VideoScript)
        InitializeComponent()
        RestoreClientSize(64, 36)

        CommandManager.AddCommandsFromObject(Me)
        CommandManager.AddCommandsFromObject(g.DefaultCommands)

        GenericMenu = New CustomMenu(AddressOf GetDefaultMenu, s.CustomMenuPreview, CommandManager, cmsMain.Items)

        GenericMenu.AddKeyDownHandler(Me)
        GenericMenu.BuildMenu()

        Instances.Add(Me)
        ShowPreviewInfo = s.ShowPreviewInfo
        HidePreviewButtons = s.HidePreviewButtons

        PreviewScript = script
    End Sub

    Protected Overrides Sub OnLoad(args As EventArgs)
        MyBase.OnLoad(args)
        Open(True)
        ShowButtons(Not s.HidePreviewButtons)
    End Sub

    Sub Open(initSize As Boolean)
        PreviewScript.Synchronize(True, True, True)

        If PreviewScript.Error <> "" Then
            MsgError("Script Error", PreviewScript.Error)
            Exit Sub
        End If

        FrameServer = FrameServerFactory.Create(PreviewScript.Path)
        Renderer = New VideoRenderer(pnVideo, FrameServer) With {
            .Info = PreviewScript.OriginalInfo,
            .ShowInfo = s.ShowPreviewInfo,
            .KeepAspectRatio = True
        }

        If FrameServer.Error <> "" Then
            MsgError("Preview Error", FrameServer.Error)
        End If

        If s.LastPosition < FrameServer.Info.FrameCount - 1 Then
            Renderer.Position = s.LastPosition
        End If

        Dim info = FrameServer.Info

        VideoSize = If(Calc.IsARSignalingRequired,
            New Size(CInt(info.Height * Calc.GetTargetDAR), CInt(info.Height)),
            New Size(CInt(info.Width), CInt(info.Height)))

        p.CutFrameCount = info.FrameCount
        p.CutFrameRate = FrameServer.FrameRate

        Dim workingArea = Screen.PrimaryScreen.WorkingArea
        Dim initHeight = CInt((workingArea.Height / 100) * s.PreviewSize)

        If Not s.ExpandPreviewWindow Then
            SetSize(s.Storage.GetInt(Me.GetType().Name + "height"))
        ElseIf initSize Then
            SetSize(initHeight)
        End If

        If s.PreviewFormBorderStyle = FormBorderStyle.None Then
            Fullscreen()
        Else
            NormalScreen()
        End If

        AfterPositionChanged()
    End Sub

    <Command("Reloads the preview script.")>
    Sub Reload()
        Renderer?.Dispose()
        FrameServer?.Dispose()

        Dim err = PreviewScript.GetError

        If err <> "" Then
            MsgError("Script Error", err)
            Exit Sub
        End If

        Open(False)
    End Sub

    Sub NormalScreen()
        IsFullscreen = False
        s.PreviewFormBorderStyle = FormBorderStyle.Sizable
        TryCast(ControlObject, Global.Gtk.Window)?.Unfullscreen()
    End Sub

    Sub Fullscreen()
        IsFullscreen = True
        s.PreviewFormBorderStyle = FormBorderStyle.None
        TryCast(ControlObject, Global.Gtk.Window)?.Fullscreen()
        pnVideo.Invalidate()
    End Sub

    Sub ShowButtons(vis As Boolean)
        ButtonsRow.Visible = vis
    End Sub

    Sub bnRight1_Click() Handles bnRight1.Click
        For Each i In Instances
            SetRelativePos(1)
        Next
    End Sub

    Sub bnLeft1_Click() Handles bnLeft1.Click
        For Each i In Instances
            SetRelativePos(-1)
        Next
    End Sub

    Sub bnStartCutRange_Click() Handles bnStartCutRange.Click
        For Each i In Instances
            SetRangeStart()
        Next
    End Sub

    Sub bnEndCutRange_Click() Handles bnEndCutRange.Click
        For Each i In Instances
            SetRangeEnd()
        Next
    End Sub

    Sub bnLeft2_Click() Handles bnLeft2.Click
        For Each i In Instances
            SetRelativePos(-10)
        Next
    End Sub

    Sub bnRight2_Click() Handles bnRight2.Click
        For Each i In Instances
            SetRelativePos(10)
        Next
    End Sub

    Sub bnDelete_Click() Handles bnDelete.Click
        For Each i In Instances
            DeleteRange()
        Next
    End Sub

    Sub bnLeft3_Click() Handles bnLeft3.Click
        For Each i In Instances
            i.SetRelativePos(-100)
        Next
    End Sub

    Sub bnRight3_Click() Handles bnRight3.Click
        For Each i In Instances
            SetRelativePos(100)
        Next
    End Sub

    Function GetDrawPos(frame As Integer) As Integer
        Dim values = TrackBarBorder * 2 + TrackBarGap * 2 + TrackBarPosition
        Dim width = CInt(((pnTrack.Width - values) / Math.Max(1, CInt(PreviewScript.Info.FrameCount - 1))) * frame)
        Return width + CInt(values / 2)
    End Function

    Sub pnTrack_MouseMove(sender As Object, e As MouseEventArgs) Handles pnTrack.MouseMove
        If e.Buttons = MouseButtons.Primary Then
            HandleMouseOntrackBar(e.Location.X)
        End If
    End Sub

    Sub pnTrack_MouseDown(sender As Object, e As MouseEventArgs) Handles pnTrack.MouseDown
        HandleMouseOntrackBar(e.Location.X)
    End Sub

    Sub HandleMouseOntrackBar(x As Single)
        Dim pos = CInt((PreviewScript.Info.FrameCount / Math.Max(1, pnTrack.Width)) * x)
        Dim remainder = pos Mod 4

        If remainder <> 0 Then
            pos -= remainder
        End If

        For Each i In Instances
            i.SetAbsolutePos(pos)
        Next
    End Sub

    Sub pnTrack_Paint(sender As Object, e As PaintEventArgs) Handles pnTrack.Paint
        If Renderer Is Nothing Then Exit Sub

        Dim gx = e.Graphics
        Dim w = pnTrack.Width
        Dim h = pnTrack.Height
        gx.FillRectangle(Colors.White, 0, 0, w, h)
        gx.DrawRectangle(Colors.Black, 0, 0, w - 1, h - 1)

        Dim trackTop = TrackBarBorder + TrackBarGap
        Dim trackHeight = h - TrackBarBorder * 2 - TrackBarGap * 2
        Dim half = CInt(TrackBarPosition / 2)

        Dim drawRange = Sub(color As Color, startFrame As Integer, endFrame As Integer)
                            Dim x1 = GetDrawPos(startFrame) - half
                            Dim x2 = GetDrawPos(endFrame) + half
                            gx.FillRectangle(color, x1, trackTop, Math.Max(1, x2 - x1), trackHeight)
                        End Sub

        For x = 0 To p.Ranges.Count - 1
            drawRange(If((x Mod 2) = 0, Colors.Green, Colors.LimeGreen), p.Ranges(x).Start, p.Ranges(x).End)
        Next

        If StartRange > -1 AndAlso StartRange <= Renderer.Position Then
            drawRange(Colors.DarkOrange, StartRange, Renderer.Position)
        End If

        If EndRange > -1 AndAlso EndRange >= Renderer.Position Then
            drawRange(Colors.DarkOrange, EndRange, Renderer.Position)
        End If

        Dim posColor = If(StartRange > -1 OrElse EndRange > -1, Colors.DarkOrange, Colors.Black)
        drawRange(posColor, Renderer.Position, Renderer.Position)
    End Sub

    Sub bnExtras_Click() Handles bnMenu.Click
        cmsMain.Show(bnMenu, 1, bnMenu.Height)
    End Sub

    Sub SetPos(pos As Integer)
        Try
            Renderer.Position = pos
            Renderer.Draw()
            AfterPositionChanged()
        Catch
        End Try
    End Sub

    Sub AfterPositionChanged()
        If Renderer Is Nothing Then Exit Sub
        s.LastPosition = Renderer.Position
        pnTrack.Invalidate()
        Dim time = TimeSpan.FromSeconds(Renderer.Position / FrameServer.FrameRate).ToString.Shorten(12)

        If time.StartsWith("00") Then
            time = time.Substring(3)
        End If

        Text = $"Preview  Frame {s.LastPosition} @ {time} - {g.DefaultCommands.GetApplicationDetails()}"
    End Sub

    <Command("Jumps to a given frame.")>
    Sub SetAbsolutePos(<DispName("Position")> pos As Integer)
        SetPos(pos)
    End Sub

    <Command("Jumps a given frame count.")>
    Sub SetRelativePos(<DispName("Position"),
        Description("Frames to jump, negative values jump backward.")>
        pos As Integer)

        SetPos(Renderer.Position + pos)
    End Sub

    <Command("Switches the window state between full and normal.")>
    Sub SwitchWindowState()
        ShowButtons(False)

        If IsFullscreen Then
            NormalScreen()
        Else
            Fullscreen()
        End If

        ShowButtons(Not s.HidePreviewButtons)
        AfterPositionChanged()
    End Sub

    <Command("Dialog to jump to a specific time.")>
    Sub GoToTime()
        Dim d As Date
        d = d.AddSeconds(Renderer.Position / FrameServer.FrameRate)
        Dim value = InputBox.Show("Go To Time", d.ToString("HH:mm:ss.fff"))

        If value <> "" Then
            SetPos(CInt((TimeSpan.Parse(value).TotalMilliseconds / 1000) * FrameServer.FrameRate))
        End If
    End Sub

    <Command("Dialog to jump to a specific frame.")>
    Sub GoToFrame()
        Dim value = InputBox.Show("Go To Frame", Renderer.Position.ToString)
        Dim pos As Integer

        If Integer.TryParse(value, pos) Then
            SetPos(pos)
        End If
    End Sub

    <Command("Opens the menu editor.")>
    Sub OpenMenuEditor()
        s.CustomMenuPreview = GenericMenu.Edit()
        g.SaveSettings()
    End Sub

    <Command("Opens the help.")>
    Sub OpenHelp()
        Dim form As New HelpForm
        form.Doc.WriteStart("Preview")
        form.Doc.WriteParagraph("The preview dialog allows to preview and cut the video. It's possible to open more than one instance of the preview for filter comparisons.")
        form.Doc.WriteH2("Cutting")
        form.Doc.WriteParagraph("Cutting can be achieved by selecting the sections that should be kept. Selected sections appear green in the trackbar. Cutting in muxing mode without video encoding works only with mkv muxer (using the nearest key frames).")
        form.Doc.WriteTips(GenericMenu.GetTips)
        form.Doc.WriteTable("Shortcut Keys", GenericMenu.GetKeys, False)
        form.Show()
    End Sub

    <Command("Sets the start cut position.")>
    Sub SetRangeStart()
        If EndRange > -1 Then
            p.Ranges.Add(New Range(Renderer.Position, EndRange))
            p.Ranges.Sort()
            EndRange = -1
        Else
            Dim currentRange = GetCurrentRange()

            If currentRange Is Nothing OrElse Renderer.Position = currentRange.End Then
                StartRange = Renderer.Position
            Else
                currentRange.Start = Renderer.Position
                EndRange = -1
            End If
        End If

        MergeRanges()
        AfterPositionChanged()
    End Sub

    <Command("Sets the end cut position.")>
    Sub SetRangeEnd()
        If StartRange > -1 Then
            p.Ranges.Add(New Range(StartRange, Renderer.Position))
            p.Ranges.Sort()
            StartRange = -1
        Else
            Dim currentRange = GetCurrentRange()

            If currentRange Is Nothing Then
                EndRange = Renderer.Position
            Else
                currentRange.End = Renderer.Position
                StartRange = -1
            End If
        End If

        MergeRanges()
        AfterPositionChanged()
    End Sub

    <Command("Splits the clip or selection into two selections.")>
    Sub SplitRange()
        If p.Ranges.Count = 0 Then
            p.Ranges.Add(New Range(0, FrameServer.Info.FrameCount - 1))
        End If

        For Each i In p.Ranges.ToArray
            If i.Start < Renderer.Position AndAlso i.End > Renderer.Position Then
                p.Ranges.Add(New Range(i.Start, Renderer.Position))
                p.Ranges.Add(New Range(Renderer.Position + 1, i.End))
                p.Ranges.Remove(i)
                Exit For
            End If
        Next

        p.Ranges.Sort()
        MergeRanges()
        AfterPositionChanged()
    End Sub

    <Command("Creates a job for each selection.")>
    Sub CreateJobForEachSelection()
        If p.SourceFile = "" OrElse p.Ranges.Count = 0 Then
            Exit Sub
        End If

        If Not g.MainForm.AssistantPassed Then
            MsgError("Follow assistant message in main dialog.")
            Exit Sub
        End If

        Dim tempProject = ObjectHelp.GetCopy(p)
        Dim projectPath = g.ProjectPath
        Dim mainFormText = g.MainForm.Text

        Dim ranges = p.Ranges.ToArray
        Dim targetFile = p.TargetFile

        For x = 0 To ranges.Length - 1
            p.Ranges.Clear()
            p.Ranges.Add(ranges(x))
            g.UpdateTrim(p.Script)
            g.MainForm.UpdateFilters()
            g.MainForm.tbTargetFile.Text = targetFile.DirAndBase + "_" & (x + 1) & targetFile.ExtFull
            g.MainForm.AddJob(False)
        Next

        g.MainForm.OpenProject(tempProject, g.ProjectPath)
        g.MainForm.tbTargetFile.Text = targetFile
        g.MainForm.Text = mainFormText
        g.ProjectPath = projectPath

        g.UpdateTrim(p.Script)
        g.MainForm.UpdateFilters()
        g.MainForm.ShowJobsDialog()
    End Sub

    <Command("Clears all cuts.")>
    Sub ClearAllRanges()
        p.Ranges.Clear()
        AfterPositionChanged()
    End Sub

    <Command("Deletes the range that encloses the current position.")>
    Sub DeleteRange()
        For Each i As Range In p.Ranges.ToArray
            If Renderer.Position >= i.Start AndAlso Renderer.Position <= i.End Then
                p.Ranges.Remove(i)
            End If
        Next

        AfterPositionChanged()
    End Sub

    <Command("Shows/hides the buttons.")>
    Sub ShowHideButtons()
        HidePreviewButtons = Not HidePreviewButtons
        ShowButtons(Not HidePreviewButtons)
        AfterPositionChanged()
    End Sub

    <Command("Shows/hides the trackbar.")>
    Sub ShowHideTrackbar()
        pnTrack.Visible = Not pnTrack.Visible
        AfterPositionChanged()
    End Sub

    <Command("Changes the size.")>
    Sub Zoom(<DispName("Factor")> factor As Single)
        SetSize(CInt(ClientSize.Height * factor))
    End Sub

    <Command("Shows/hides various infos.")>
    Sub ToggleInfos()
        ShowPreviewInfo = Not ShowPreviewInfo
        Renderer.ShowInfo = ShowPreviewInfo
        Renderer.Draw()
    End Sub

    <Command("Plays the script with a player.")>
    Sub ShowExternalPlayer()
        Dim script = PreviewScript.GetNewScript()
        script.Path = Path.Combine(p.TempDir, p.TargetFile.Base + "_play." + script.FileType)
        g.UpdateTrim(script)
        g.PlayScript(script)
    End Sub

    <Command("Plays the script with mpv.")>
    Sub PlayWithMpvnet()
        Dim script = PreviewScript.GetNewScript()
        script.Path = Path.Combine(p.TempDir, p.TargetFile.Base + "_play." + script.FileType)
        g.UpdateTrim(script)
        g.PlayScriptWithMPV(script, "--start=" + GetPlayPosition.ToString)
    End Sub

    <Command("Plays the script with MPC.")>
    Sub PlayWithMPC()
        Dim script = PreviewScript.GetNewScript()
        script.Path = Path.Combine(p.TempDir, p.TargetFile.Base + "_play." + script.FileType)
        g.UpdateTrim(script)
        g.PlayScriptWithMPC(script, "/start " & GetPlayPosition.TotalMilliseconds)
    End Sub

    <Command("Closes the dialog.")>
    Sub CloseDialog()
        Close()
    End Sub

    <Command("Jumps to the previous cut point.")>
    Sub JumpToThePreviousRangePos()
        Dim list As New List(Of Object)

        For Each range As Range In p.Ranges
            list.Add(range.Start)
            list.Add(range.End)
        Next

        list.Add(Renderer.Position - 1)
        list.Sort()

        Dim index = list.IndexOf(Renderer.Position - 1) - 1

        If index >= 0 AndAlso index < list.Count Then
            SetPos(CInt(list(index)))
        End If
    End Sub

    <Command("Copies the time of the current position.")>
    Sub CopyTime()
        Clipboard.SetText(Date.Today.AddSeconds(Renderer.Position / FrameServer.FrameRate).ToString("HH:mm:ss.fff"))
    End Sub

    <Command("Jumps to the next cut point.")>
    Sub JumpToTheNextRangePos()
        Dim list As New List(Of Object)

        For Each i As Range In p.Ranges
            list.Add(i.Start)
            list.Add(i.End)
        Next

        list.Add(Renderer.Position + 1)
        list.Sort()

        Dim index = list.IndexOf(Renderer.Position + 1) + 1

        If index >= 0 AndAlso index < list.Count Then
            SetPos(CInt(list(index)))
        End If
    End Sub

    <Command("Saves the current frame as bitmap.")>
    Sub SaveBitmap()
        Dim filename = ""

        Select Case s.SaveImagePreviewFrameNumberPosition
            Case ImageFrameNumberPosition.Prefix
                filename = $"{Renderer.Position:00000}_{p.TargetFile.Base}"
            Case ImageFrameNumberPosition.Suffix
                filename = $"{p.TargetFile.Base}_{Renderer.Position:00000}"
            Case Else
                Throw New NotSupportedException("PreviewForm.SaveBitmap()")
        End Select

        Using dialog As New SaveFileDialog
            dialog.SetFilter({"bmp"})
            dialog.FileName = filename

            If dialog.ShowDialog = DialogResult.OK Then
                ImageHelp.Save(FrameHelp.CreateBitmap(FrameServer, Renderer.Position), dialog.FileName, ImageFormat.Bitmap)
            End If
        End Using
    End Sub

    <Command("Saves the current frame as bitmap.")>
    Sub SavePng()
        Dim filename = ""

        Select Case s.SaveImagePreviewFrameNumberPosition
            Case ImageFrameNumberPosition.Prefix
                filename = $"{Renderer.Position:00000}_{p.TargetFile.Base}"
            Case ImageFrameNumberPosition.Suffix
                filename = $"{p.TargetFile.Base}_{Renderer.Position:00000}"
            Case Else
                Throw New NotSupportedException("PreviewForm.SavePNG()")
        End Select

        Using dialog As New SaveFileDialog
            dialog.SetFilter({"png"})
            dialog.FileName = filename

            If dialog.ShowDialog = DialogResult.OK Then
                FrameHelp.CreateBitmap(FrameServer, Renderer.Position)?.Save(dialog.FileName, ImageFormat.Png)
            End If
        End Using
    End Sub

    <Command("Saves the current frame as JPG.")>
    Sub SaveJPG()
        Dim filename = ""

        Select Case s.SaveImagePreviewFrameNumberPosition
            Case ImageFrameNumberPosition.Prefix
                filename = $"{Renderer.Position:00000}_{p.TargetFile.Base}"
            Case ImageFrameNumberPosition.Suffix
                filename = $"{p.TargetFile.Base}_{Renderer.Position:00000}"
            Case Else
                Throw New NotSupportedException("PreviewForm.SaveJPG()")
        End Select

        Using dialog As New SaveFileDialog
            dialog.DefaultExt = "jpg"
            dialog.FileName = filename

            If dialog.ShowDialog = DialogResult.OK Then
                SaveJpgByPath(dialog.FileName)
            End If
        End Using
    End Sub

    <Command("Saves the current frame as JPG to the given path which can contain macros.")>
    Sub SaveJpgByPath(
        <DispName("File Path")>
        <Description("File path which can contain macros.")>
        path As String)

        path = Macro.Expand(path)
        Dim result = InputBox.Show("Enter the compression quality",
                                   s.Storage.GetInt("preview compression quality", 95).ToString)

        If result.IsInt Then
            s.Storage.SetInt("preview compression quality", result.ToInt)
            ImageHelp.SaveJpeg(FrameHelp.CreateBitmap(FrameServer, Renderer.Position), path, result.ToInt)
        End If
    End Sub

    <Command("Shows a dialog to navigate to a chapter.")>
    Sub GoToChapter()
        Dim fp = Path.Combine(p.TempDir, p.SourceFile.Base + "_chapters.txt")

        If Not File.Exists(fp) Then
            MsgError("No chapter file found.")
            Exit Sub
        End If

        Using td As New TaskDialog(Of String)
            td.Title = "Select a chapter"

            For Each line In File.ReadAllLines(fp)
                Dim left = line.Left("=")

                If left.Length = 9 Then
                    td.AddCommand(left.Substring(7) + "   " + line.Right("="), line.Right("="))
                End If
            Next

            If td.Show() <> "" Then
                SetPos(CInt((TimeSpan.Parse(td.SelectedValue).TotalMilliseconds / 1000) * FrameServer.FrameRate))
            End If
        End Using
    End Sub

    Shared Function GetDefaultMenu() As CustomMenuItem
        Dim ret As New CustomMenuItem("Root")

        ret.Add("Navigation|Go To Start", NameOf(SetAbsolutePos), Keys.Control Or Keys.Left, {0})
        ret.Add("Navigation|Go To End", NameOf(SetAbsolutePos), Keys.Control Or Keys.Right, {1000000000})
        ret.Add("Navigation|-")
        ret.Add("Navigation|Go To Frame...", NameOf(GoToFrame), Keys.G)
        ret.Add("Navigation|Go To Time...", NameOf(GoToTime), Keys.T)
        ret.Add("Navigation|Go To Chapter...", NameOf(GoToChapter), Keys.C)
        ret.Add("Navigation|-")
        ret.Add("Navigation|Go To Previous Cut Point", NameOf(JumpToThePreviousRangePos), Keys.Control Or Keys.Up)
        ret.Add("Navigation|Go To Next Cut Point", NameOf(JumpToTheNextRangePos), Keys.Control Or Keys.Down)
        ret.Add("Navigation|-")
        ret.Add("Navigation|Backward 1 Frame", NameOf(SetRelativePos), Keys.Left, {-1})
        ret.Add("Navigation|Backward 10 Frames", NameOf(SetRelativePos), Keys.Up, {-10})
        ret.Add("Navigation|Backward 100 Frames", NameOf(SetRelativePos), Keys.PageUp, {-100})
        ret.Add("Navigation|Backward 1000 Frames", NameOf(SetRelativePos), Keys.Subtract, {-1000})
        ret.Add("Navigation|-")
        ret.Add("Navigation|Forward 1000 Frames", NameOf(SetRelativePos), Keys.Add, {1000})
        ret.Add("Navigation|Forward 100 Frames", NameOf(SetRelativePos), Keys.PageDown, {100})
        ret.Add("Navigation|Forward 10 Frames", NameOf(SetRelativePos), Keys.Down, {10})
        ret.Add("Navigation|Forward 1 Frames", NameOf(SetRelativePos), Keys.Right, {1})

        ret.Add("Cut|Begin Selection", NameOf(SetRangeStart), Keys.Home)
        ret.Add("Cut|End Selection", NameOf(SetRangeEnd), Keys.End)
        ret.Add("Cut|-")
        ret.Add("Cut|Split Selection", NameOf(SplitRange), Keys.S)
        ret.Add("Cut|-")
        ret.Add("Cut|Delete Selection", NameOf(DeleteRange), Keys.Delete, Symbol.Delete)
        ret.Add("Cut|Delete All Selections", NameOf(ClearAllRanges), Keys.Control Or Keys.Delete)
        ret.Add("Cut|-")
        ret.Add("Cut|Create job for each selection", NameOf(CreateJobForEachSelection))

        ret.Add("View|Reload", NameOf(Reload), Keys.R, Symbol.CircleRing)
        ret.Add("View|Info", NameOf(ToggleInfos), Keys.I, Symbol.Info)
        ret.Add("View|Fullscreen", NameOf(SwitchWindowState), Keys.Enter, Symbol.FullScreen)
        ret.Add("View|-")
        ret.Add("View|Zoom In", NameOf(Zoom), Keys.Equal, Symbol.ZoomIn, {1.1F})
        ret.Add("View|Zoom Out", NameOf(Zoom), Keys.Minus, Symbol.ZoomOut, {0.9F})
        ret.Add("View|-")
        ret.Add("View|Buttons", NameOf(ShowHideButtons), Keys.B)
        ret.Add("View|Trackbar", NameOf(ShowHideTrackbar), Keys.Control Or Keys.T)

        ret.Add("Tools|Play with mpv", NameOf(PlayWithMpvnet), Keys.F9, Symbol.Play)
        ret.Add("Tools|-")
        ret.Add("Tools|Copy Frame Number", NameOf(g.DefaultCommands.CopyToClipboard), {"%pos_frame%"})
        ret.Add("Tools|Copy Time", NameOf(CopyTime))
        ret.Add("Tools|-")
        ret.Add("Tools|Save Png", NameOf(SavePng), Keys.Control Or Keys.P, Symbol.Save)
        ret.Add("Tools|Save Bitmap", NameOf(SaveBitmap), Keys.Control Or Keys.S, Symbol.Save)

        ret.Add("Edit Menu...", NameOf(OpenMenuEditor), Keys.M)
        ret.Add("Help...", NameOf(OpenHelp), Keys.F1, Symbol.Help)
        ret.Add("Exit", NameOf(CloseDialog), Keys.Escape)

        Return ret
    End Function

    Sub MergeRanges()
        For Each r1 In p.Ranges.ToArray
            For Each r2 In p.Ranges.ToArray
                If Not r1 Is r2 Then
                    If (r2.Start >= r1.Start AndAlso r2.Start <= r1.End) OrElse
                        (r2.End >= r1.Start AndAlso r2.End <= r1.End) Then

                        p.Ranges.Remove(r1)
                        p.Ranges.Remove(r2)
                        p.Ranges.Add(New Range(Math.Min(r1.Start, r2.Start), Math.Max(r1.End, r2.End)))
                        MergeRanges()
                    End If
                End If
            Next
        Next

        p.Ranges.Sort()
    End Sub

    Sub GenericMenu_Command(e As CustomMenuItemEventArgs) Handles GenericMenu.Command
        e.Handled = True

        Select Case e.Item.MethodName
            Case NameOf(CloseDialog), NameOf(PlayWithMPC), NameOf(PlayWithMpvnet), NameOf(Reload)
                ProcessMenu(e.Item)
            Case Else
                For Each i In Instances.ToArray()
                    i.ProcessMenu(e.Item)
                Next
        End Select
    End Sub

    Sub ProcessMenu(item As CustomMenuItem)
        GenericMenu.Process(item)
    End Sub

    Function GetCurrentRange() As Range
        For Each range In p.Ranges
            If Renderer.Position >= range.Start AndAlso Renderer.Position <= range.End Then
                Return range
            End If
        Next
    End Function

    Function GetPlayPosition() As TimeSpan
        If p.Ranges.Count = 0 Then
            Dim pos = Renderer.Position

            If pos = FrameServer.Info.FrameCount - 1 Then
                pos -= 2
            End If

            Return TimeSpan.FromSeconds(pos / FrameServer.FrameRate)
        Else
            If GetCurrentRange() Is Nothing Then
                Return TimeSpan.Zero
            Else
                Dim frames As Integer

                For x = 0 To p.Ranges.Count - 1
                    Dim current = p.Ranges(x)

                    If current.End < Renderer.Position Then
                        frames += current.End - current.Start
                    Else
                        Dim pos = Renderer.Position
                        Return TimeSpan.FromSeconds((frames + (pos - current.Start)) / FrameServer.FrameRate)
                    End If
                Next
            End If
        End If
    End Function

    Sub SetSize(newHeight As Integer)
        Dim workingArea = Screen.PrimaryScreen.WorkingArea
        Dim extraHeight = If(pnTrack.Visible, pnTrack.Height + FH(0.2), 0) + If(ButtonsRow.Visible, FH(2.4), 0)
        Dim clientHeight = newHeight

        If clientHeight > workingArea.Height * 0.9 Then
            clientHeight = CInt(workingArea.Height * 0.9)
        End If

        If clientHeight <= 0 OrElse VideoSize.Height = 0 Then
            Exit Sub
        End If

        Dim videoHeight = clientHeight - extraHeight
        Dim clientWidth = CInt((videoHeight / VideoSize.Height) * VideoSize.Width)

        If clientWidth > workingArea.Width * 0.9 Then
            clientWidth = CInt(workingArea.Width * 0.9)
            videoHeight = CInt((clientWidth / VideoSize.Width) * VideoSize.Height)
            clientHeight = videoHeight + extraHeight
        End If

        ClientSize = New Size(clientWidth, clientHeight)
    End Sub

    Sub pnVideo_MouseWheel(sender As Object, e As MouseEventArgs) Handles pnVideo.MouseWheel
        Dim pos = 1

        If e.Modifiers = Keys.Control Then pos = 10
        If e.Modifiers = Keys.Shift Then pos = 100
        If e.Modifiers = Keys.Alt Then pos = 1000

        If e.Delta.Height < 0 Then
            pos = pos * -1
        End If

        If s.ReverseVideoScrollDirection Then
            pos = pos * -1
        End If

        SetRelativePos(pos)
        e.Handled = True
    End Sub

    Protected Overrides Sub OnClosing(args As CancelEventArgs)
        Instances.Remove(Me)
        GenericMenu.RemoveKeyDownHandler(Me)
        g.UpdateTrim(p.Script)
        s.ShowPreviewInfo = ShowPreviewInfo
        s.HidePreviewButtons = HidePreviewButtons

        If Renderer IsNot Nothing Then
            s.LastPosition = Renderer.Position
        End If

        g.MainForm.UpdateFilters()
        Renderer?.Dispose()
        Renderer = Nothing
        FrameServer?.Dispose()
        FrameServer = Nothing
        MyBase.OnClosing(args)
    End Sub

    Sub pnVideo_MouseDown(sender As Object, e As MouseEventArgs) Handles pnVideo.MouseDown
        pnVideo.Focus()
    End Sub

    Sub pnVideo_DoubleClick(sender As Object, e As MouseEventArgs) Handles pnVideo.MouseDoubleClick
        SwitchWindowState()
    End Sub
End Class
