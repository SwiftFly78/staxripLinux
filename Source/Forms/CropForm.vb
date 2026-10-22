

Imports System.ComponentModel

Imports StaxRip.UI

Public Class CropForm
    Inherits DialogBase

    Public pnLeftActive, pnTopActive, pnBottomActive, pnRightActive As Panel
    Public WithEvents tbPosition As TrackBarEx
    Public WithEvents pnVideo As Drawable
    Public WithEvents bnMenu As ButtonEx
    Public laStatus As LabelEx
    Public WithEvents cms As ContextMenuStripEx

    Sub InitializeComponent()
        Dim offset = CInt(FontHeight * 0.6)
        pnLeftActive = New Panel With {.Width = offset}
        pnTopActive = New Panel With {.Height = offset}
        pnRightActive = New Panel With {.Width = offset}
        pnBottomActive = New Panel With {.Height = offset}
        pnVideo = New Drawable With {.CanFocus = True}
        tbPosition = New TrackBarEx
        laStatus = New LabelEx With {.Text = "-", .Font = FontManager.GetCodeFont(-1)}
        cms = New ContextMenuStripEx
        bnMenu = New ButtonEx With {.Text = "Menu", .ShowMenuSymbol = True}
        pnVideo.ContextMenu = cms

        Dim videoTable As New TableLayout(
            New TableRow(New TableCell, pnTopActive, New TableCell),
            New TableRow(pnLeftActive, New TableCell(pnVideo, True), pnRightActive) With {.ScaleHeight = True},
            New TableRow(New TableCell, pnBottomActive, New TableCell))

        Content = New TableLayout(
            ScaledRow(videoTable),
            Row(tbPosition),
            Row(New TableLayout(Row(Scaled(laStatus), bnMenu)) With {.Spacing = DefaultSpacing, .Padding = New Padding(FH(0.3), 0)})) With {
            .Spacing = New Size(0, FH(0.2))}

        Text = $"Crop - {g.DefaultCommands.GetApplicationDetails()}"
        Resizable = True
        MinimumSize = New Size(FH(20), FH(15))
    End Sub

    Private FrameServer As IFrameServer
    Private Renderer As VideoRenderer
    Private Side As CropSide
    Private ActiveCropSide As CropSide

    Enum CropSide
        None
        Left
        Top
        Right
        Bottom
    End Enum
    Private CommandManager As New CommandManager
    Private WithEvents CustomMenu As CustomMenu

    Public Property BorderColor As Color
    Public Property BorderSelectedColor As Color

    Sub New()
        InitializeComponent()

        CommandManager.AddCommandsFromObject(Me)
        CommandManager.AddCommandsFromObject(g.DefaultCommands)

        CustomMenu = New CustomMenu(AddressOf GetDefaultMenuCrop,
            s.CustomMenuCrop, CommandManager, cms.Items)

        CustomMenu.AddKeyDownHandler(Me)
        CustomMenu.BuildMenu()

        tbPosition.Maximum = Math.Max(1, p.SourceFrames)

        BorderColor = If(IsDarkMode, Color.FromArgb(70, 70, 70), Color.FromArgb(200, 200, 200))
        BorderSelectedColor = ThemeManager.CurrentTheme.CropForm.BorderSelectedColor.ToColor

        If BorderSelectedColor = BorderColor OrElse BorderSelectedColor.A = 0 Then
            BorderSelectedColor = Color.FromArgb(60, 140, 220)
        End If
    End Sub

    Protected Overrides Sub OnLoad(args As EventArgs)
        MyBase.OnLoad(args)

        Dim zoom = 0.0
        Dim workingArea = Screen.PrimaryScreen.WorkingArea

        While p.SourceWidth * zoom < 0.77 * workingArea.Width AndAlso
            p.SourceHeight * zoom < 0.77 * workingArea.Height

            zoom += 0.01
        End While

        SetDialogSize(CInt(p.SourceWidth * zoom), CInt(p.SourceHeight * zoom))

        Dim script As New VideoScript
        script.Engine = p.Script.Engine
        script.Path = Path.Combine(p.TempDir, p.TargetFile.Base + "_crop." + script.FileType)
        script.Filters.Add(p.Script.GetFilter("Source").GetCopy())

        If p.Script.GetFilter("Rotation") IsNot Nothing Then
            script.Filters.Add(p.Script.GetFilter("Rotation").GetCopy())
        End If

        If p.CropWithTonemapping Then
            If p.SourceVideoBitDepth > 8 AndAlso Not p.SourceVideoHdrFormat?.EqualsAny("", "SDR") Then
                If p.Script.Engine = ScriptEngine.AviSynth AndAlso Package.AVSLibPlacebo.IsAvailable() Then
                    script.Filters.Add(New VideoFilter("Color", "Tonemap", "ConvertBits(16)" + BR + "libplacebo_Tonemap()" + BR + "ConvertToYUV420()" + BR + "ConvertBits(8)"))
                ElseIf p.Script.Engine = ScriptEngine.VapourSynth AndAlso Package.VSLibPlacebo.IsAvailable() AndAlso Package.Fmtconv.IsAvailable() Then
                    script.Filters.Add(New VideoFilter("Color", "Tonemap", "clip = core.fmtc.bitdepth(clip, bits=16)" + BR + "clip = core.placebo.Tonemap(clip, src_csp=1, dst_csp=0, dynamic_peak_detection=0, tone_mapping_function=7)" + BR + $"clip = clip.resize.Bicubic(format = vs.YUV420P8)"))
                End If
            End If
        End If

        If p.CropWithHighContrast Then
            If p.Script.Engine = ScriptEngine.AviSynth Then
                script.Filters.Add(New VideoFilter("Levels", "Levels", "Levels(0, 2.2, 255, 0, 255, coring=true)"))
            Else
                script.Filters.Add(New VideoFilter("Levels", "Levels", "clip = adjust.Tweak(clip, hue=0, sat=1.75, bright=10, cont=1.75)"))
            End If
        End If

        script.Synchronize(True, True, True)

        FrameServer = FrameServerFactory.Create(script.Path)
        Renderer = New VideoRenderer(pnVideo, FrameServer) With {
            .Info = script.Info
        }

        If s.LastPosition < (FrameServer.Info.FrameCount - 1) Then
            Renderer.Position = s.LastPosition
        End If

        tbPosition.Value = Renderer.Position
        SelectBorder(CropSide.Top)
        UpdateAll()

        If FrameServer.Error <> "" Then
            MsgError("CROP", FrameServer.Error)
        End If
    End Sub

    Protected Overrides Sub OnClosing(args As CancelEventArgs)
        MyBase.OnClosing(args)

        Dim err = p.Script.GetError

        If err <> "" Then
            Using td As New TaskDialog(Of String)
                td.Title = "Script Error"
                td.Icon = TaskIcon.Error
                td.Content = err
                td.AddButton("OK")
                td.AddButton("Exit")

                If td.Show() = "OK" Then
                    args.Cancel = True
                    Exit Sub
                End If
            End Using
        End If

        p.RemindToCrop = False

        If Renderer IsNot Nothing Then
            s.LastPosition = Renderer.Position
            Renderer.Dispose()
        End If

        FrameServer?.Dispose()
    End Sub

    Sub TrackLength_Scroll() Handles tbPosition.ValueChanged
        If Renderer IsNot Nothing AndAlso Renderer.Position <> tbPosition.Value Then
            SetPos(tbPosition.Value)
        End If
    End Sub

    Sub SetPos(pos As Integer)
        Try
            Renderer.Position = pos
            tbPosition.Value = Renderer.Position
            Renderer.Draw()
            UpdateAll()
        Catch
        End Try
    End Sub

    Protected Overrides Sub OnSizeChanged(args As EventArgs)
        MyBase.OnSizeChanged(args)
        Renderer?.Draw()
    End Sub

    Sub CropActiveSideInternal(x As Integer, opposite As Boolean)
        Select Case Side
            Case CropSide.Left
                p.CropLeft += x

                If opposite Then
                    p.CropRight += x
                End If
            Case CropSide.Top
                p.CropTop += x

                If opposite Then
                    p.CropBottom += x
                End If
            Case CropSide.Right
                p.CropRight += x

                If opposite Then
                    p.CropLeft += x
                End If
            Case CropSide.Bottom
                p.CropBottom += x

                If opposite Then
                    p.CropTop += x
                End If
        End Select

        UpdateAll()
    End Sub

    Function GetSide(e As MouseEventArgs) As CropSide
        Dim factorX = p.SourceWidth / Math.Max(1, pnVideo.Width)
        Dim factorY = p.SourceHeight / Math.Max(1, pnVideo.Height)

        Dim leftSide = CInt(e.Location.X * factorX)
        Dim topSide = CInt(e.Location.Y * factorY)
        Dim rightSide = CInt((pnVideo.Width - e.Location.X) * factorX)
        Dim bottomSide = CInt((pnVideo.Height - e.Location.Y) * factorY)

        Dim sides As Integer() = {leftSide, topSide, rightSide, bottomSide}
        Array.Sort(sides)

        Select Case sides(0)
            Case leftSide
                Return CropSide.Left
            Case topSide
                Return CropSide.Top
            Case rightSide
                Return CropSide.Right
            Case bottomSide
                Return CropSide.Bottom
        End Select

        Return CropSide.None
    End Function

    Sub MouseCrop(e As MouseEventArgs)
        Dim scaleX = p.SourceWidth / Math.Max(1, pnVideo.Width)
        Dim scaleY = p.SourceHeight / Math.Max(1, pnVideo.Height)

        Dim leftSide = Math.Max(0, CInt(e.Location.X * scaleX))
        Dim topSide = Math.Max(0, CInt(e.Location.Y * scaleY))
        Dim rightSide = Math.Max(0, CInt((pnVideo.Width - e.Location.X) * scaleX))
        Dim bottomSide = Math.Max(0, CInt((pnVideo.Height - e.Location.Y) * scaleY))

        Select Case ActiveCropSide
            Case CropSide.Left
                p.CropLeft = FixMod(leftSide)
            Case CropSide.Top
                p.CropTop = FixMod(topSide)
            Case CropSide.Right
                p.CropRight = FixMod(rightSide)
            Case CropSide.Bottom
                p.CropBottom = FixMod(bottomSide)
        End Select

        UpdateAll()
    End Sub

    Sub MouseSelectBorder(e As MouseEventArgs)
        SelectBorder(GetSide(e))
    End Sub

    Sub SelectBorder(borderSide As CropSide)
        Dim color = BorderSelectedColor

        DeactivateActiveColor()

        Select Case borderSide
            Case CropSide.Left
                pnLeftActive.BackgroundColor = color
            Case CropSide.Top
                pnTopActive.BackgroundColor = color
            Case CropSide.Right
                pnRightActive.BackgroundColor = color
            Case CropSide.Bottom
                pnBottomActive.BackgroundColor = color
        End Select

        Side = borderSide
    End Sub

    Sub DeactivateActiveColor()
        Dim color = BorderColor
        pnLeftActive.BackgroundColor = color
        pnTopActive.BackgroundColor = color
        pnRightActive.BackgroundColor = color
        pnBottomActive.BackgroundColor = color
    End Sub

    Function FixMod(value As Integer) As Integer
        If p.AutoCorrectCropValues AndAlso Not value Mod 2 = 0 Then
            value += 1
        End If

        Return value
    End Function

    Sub UpdateAll()
        If Renderer Is Nothing Then
            Exit Sub
        End If

        Dim maxWidth = CInt(p.SourceWidth * 0.8)
        Dim maxHeight = CInt(p.SourceHeight * 0.8)

        If p.CropLeft > maxWidth Then
            p.CropLeft = FixMod(maxWidth)
        End If

        If p.CropTop > maxHeight Then
            p.CropTop = FixMod(maxHeight)
        End If

        If p.CropRight > maxWidth Then
            p.CropRight = FixMod(maxWidth)
        End If

        If p.CropBottom > maxHeight Then
            p.CropBottom = FixMod(maxHeight)
        End If

        Renderer.CropLeft = p.CropLeft
        Renderer.CropTop = p.CropTop
        Renderer.CropRight = p.CropRight
        Renderer.CropBottom = p.CropBottom
        Renderer.Draw()

        Dim cropw = p.SourceWidth - p.CropLeft - p.CropRight
        Dim croph = p.SourceHeight - p.CropTop - p.CropBottom

        Dim time = "n/a"
        If Renderer.Info.FrameRate > 0 Then
            Dim lengthDate = Date.Today.AddSeconds(Renderer.Position / Renderer.Info.FrameRate)
            time = lengthDate.ToString(If(lengthDate.Hour = 0, "mm:ss.fff", "HH:mm:ss.fff"))
        End If

        Dim isResized = p.Script.IsFilterActive("Resize")
        Dim isValidAnamorphicSize = (p.TargetWidth = 1440 AndAlso p.TargetHeight = 1080) OrElse (p.TargetWidth = 960 AndAlso p.TargetHeight = 720)
        Dim err = If(isResized AndAlso Not isValidAnamorphicSize, Calc.GetAspectRatioError.ToString("f2") + "%", "n/a")

        laStatus.Text =
            "  Frame: " & Renderer.Position.ToString().PadLeft(6) &
            "  |  Time: " & time &
            "  |  Size: " & cropw & "/" & croph &
            "  |  X: " & p.CropLeft & "/" & p.CropRight &
            "  |  Y: " & p.CropTop & "/" & p.CropBottom &
            "  |  Mod: " + Calc.GetMod(cropw, croph, False) +
            "  |  Error: " + err +
            "  |  DAR: " + Calc.GetTargetDAR().ToString("f6")
    End Sub

    Sub pVideo_MouseMove(sender As Object, e As MouseEventArgs) Handles pnVideo.MouseMove
        If e.Buttons = MouseButtons.Primary Then
            MouseCrop(e)
        Else
            MouseSelectBorder(e)
        End If
    End Sub

    Sub pVideo_MouseDown(sender As Object, e As MouseEventArgs) Handles pnVideo.MouseDown
        pnVideo.Focus()

        If e.Buttons = MouseButtons.Primary Then
            ActiveCropSide = GetSide(e)
            MouseCrop(e)
        End If
    End Sub

    Sub pVideo_MouseWheel(sender As Object, e As MouseEventArgs) Handles pnVideo.MouseWheel
        Dim value = 2
        value = If((e.Modifiers And Keys.Shift) = Keys.Shift, 8, value)
        value = If(e.Delta.Height > 0, value, value * -1)
        CropActiveSideInternal(value, (e.Modifiers And Keys.Control) = Keys.Control)
        e.Handled = True
    End Sub

    Sub SetDialogSize(w As Integer, h As Integer)
        Dim extraW = FH(0.6) * 2
        Dim extraH = FH(0.6) * 2 + FH(4.5)
        ClientSize = New Size(w + extraW, h + extraH)
        Renderer?.Draw()
    End Sub

    Shared Function GetDefaultMenuCrop() As CustomMenuItem
        Dim ret As New CustomMenuItem("Root")
        Dim minusKey = If(Not KeysHelp.IsNumPadInstalled, Keys.Subtract, Keys.Minus)
        Dim plusKey = If(Not KeysHelp.IsNumPadInstalled, Keys.Add, Keys.Equal)

        ret.Add("No Crop", NameOf(SetCropValues), Keys.N, {0, 0, 0, 0})
        ret.Add("Auto Crop", NameOf(RunAutoCrop), Keys.A)
        ret.Add("Smart Crop", NameOf(RunSmartCrop), Keys.S)
        ret.Add("-")
        ret.Add("Increase Active Side", NameOf(CropActiveSide), plusKey, {2, 2})
        ret.Add("Decrease Active Side", NameOf(CropActiveSide), minusKey, {-2, -2})
        ret.Add("-")
        ret.Add("Increase Active Side Large", NameOf(CropActiveSide), plusKey Or Keys.Shift, {8, 8})
        ret.Add("Decrease Active Side Large", NameOf(CropActiveSide), minusKey Or Keys.Shift, {-8, -8})
        ret.Add("-")
        ret.Add("Increase Active And Opposite Side", NameOf(CropActiveAndOppositeSide), plusKey Or Keys.Control, {2, 2})
        ret.Add("Decrease Active And Opposite Side", NameOf(CropActiveAndOppositeSide), minusKey Or Keys.Control, {-2, -2})
        ret.Add("-")
        ret.Add("Increase Active And Opposite Side Large", NameOf(CropActiveAndOppositeSide), plusKey Or Keys.Control Or Keys.Shift, {8, 8})
        ret.Add("Decrease Active And Opposite Side Large", NameOf(CropActiveAndOppositeSide), minusKey Or Keys.Control Or Keys.Shift, {-8, -8})
        ret.Add("-")
        ret.Add("Navigation|Go To Frame...", NameOf(GoToFrame), Keys.F)
        ret.Add("Navigation|Go To Time...", NameOf(GoToTime), Keys.T)
        ret.Add("Navigation|-")
        ret.Add("Navigation|Go To Start", NameOf(SetAbsolutePosition), Keys.Control Or Keys.Left, {0})
        ret.Add("Navigation|100 Frames Backward", NameOf(SetRelativePosition), Keys.PageUp, {-100})
        ret.Add("Navigation|1000 Frames Backward", NameOf(SetRelativePosition), Keys.PageUp Or Keys.Control, {-1000})
        ret.Add("Navigation|-")
        ret.Add("Navigation|100 Frames Forward", NameOf(SetRelativePosition), Keys.PageDown, {100})
        ret.Add("Navigation|1000 Frames Forward", NameOf(SetRelativePosition), Keys.PageDown Or Keys.Control, {1000})
        ret.Add("Navigation|Go To End", NameOf(SetAbsolutePosition), Keys.Control Or Keys.Right, {1000000000})
        ret.Add("-")
        ret.Add("Crop Color | Theme", NameOf(SetCropColor), {System.Drawing.Color.Empty})
        ret.Add("Crop Color | White", NameOf(SetCropColor), {System.Drawing.Color.White})
        ret.Add("Crop Color | Black", NameOf(SetCropColor), {System.Drawing.Color.Black})
        ret.Add("Crop Color | Light Gray", NameOf(SetCropColor), {System.Drawing.Color.LightGray})
        ret.Add("Crop Options...", NameOf(ShowOptions), Keys.O, Symbol.Settings)
        ret.Add("Edit Menu...", NameOf(ShowMenuEditor), Keys.M)
        ret.Add("Help...", NameOf(ShowHelpDialog), Keys.F1, Symbol.Help)
        ret.Add("Exit", NameOf(CloseDialog), Keys.Escape)

        Return ret
    End Function

    <Command("Sets the four crop values.")>
    Sub SetCropValues(
        left As Integer,
        top As Integer,
        right As Integer,
        bottom As Integer)

        p.CropLeft = left
        p.CropTop = top
        p.CropRight = right
        p.CropBottom = bottom

        UpdateAll()
    End Sub

    <Command("Crops the active side.")>
    Sub CropActiveSide(
        <DispName("Pixel (corrected)"),
        Description("Pixels to crop when 'Auto correct crop values' is enabled.")>
        valueSafe As Integer,
        <DispName("Pixel (uncorrected)"),
        Description("Pixels to crop when 'Auto correct crop values' is disabled.")>
        valueUnsafe As Integer)

        CropActiveSideInternal(FixMod(If(p.AutoCorrectCropValues, valueSafe, valueUnsafe)), False)
    End Sub

    <Command("Crops the active and the opposite side of the active side")>
    Sub CropActiveAndOppositeSide(
        <DispName("Pixel (corrected)"),
        Description("Pixels to crop when 'Auto correct crop values' is enabled.")>
        valueSafe As Integer,
        <DispName("Pixel (uncorrected)"),
        Description("Pixels to crop when 'Auto correct crop values' is disabled.")>
        valueUnsafe As Integer)

        CropActiveSideInternal(FixMod(If(p.AutoCorrectCropValues, valueSafe, valueUnsafe)), True)
    End Sub

    <Command("Detects the crop values automatically.")>
    Sub RunAutoCrop()
        Dim isDV = p.HdrDolbyVisionMetadataFile IsNot Nothing AndAlso p.VideoEncoder.IsDolbyVisionSet

        If Not isDV Then
            p.CropLeft = 0
            p.CropTop = 0
            p.CropRight = 0
            p.CropBottom = 0
            UpdateAll()
        End If

        g.RunAutoCrop(Sub(progress As Double)
                          UIInvoke(Sub()
                                       tbPosition.Value = CInt(tbPosition.Maximum / 100 * progress)
                                       TrackLength_Scroll()
                                   End Sub)
                          DoEvents()
                      End Sub)
        UpdateAll()

        If Not isDV Then
            tbPosition.Value = 0
            TrackLength_Scroll()
        End If
    End Sub

    <Command("Crops until the proper aspect ratio is found.")>
    Sub RunSmartCrop()
        g.SmartCrop()
        UpdateAll()
    End Sub

    <Command("Dialog to configure the menu.")>
    Sub ShowMenuEditor()
        s.CustomMenuCrop = CustomMenu.Edit()
        g.SaveSettings()
    End Sub

    <Command("Exits the dialog.")>
    Sub CloseDialog()
        Close()
    End Sub

    <Command("Shows a dialog with crop options.")>
    Sub ShowOptions()
        g.MainForm.ShowOptionsDialog("Image | Crop")
        UpdateAll()
    End Sub

    <Command("Sets the color used for cropping.")>
    Sub SetCropColor(color As System.Drawing.Color)
        s.CropColor = color
        Renderer.ApplyTheme()
        Renderer.Draw()
    End Sub

    <Command("Dialog to jump to a specific time.")>
    Sub GoToTime()
        Dim d As Date
        d = d.AddSeconds(Renderer.Position / FrameServer.FrameRate)
        Dim value = InputBox.Show("Go To Time", d.ToString("HH:mm:ss.fff"))
        Dim time As TimeSpan

        If value <> "" AndAlso TimeSpan.TryParse(value, time) Then
            SetPos(CInt((time.TotalMilliseconds / 1000) * FrameServer.FrameRate))
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

    <Command("Jumps to a given frame.")>
    Sub SetAbsolutePosition(<DispName("Position")> pos As Integer)
        SetPos(pos)
    End Sub

    <Command("Jumps a given frame count.")>
    Sub SetRelativePosition(<DispName("Offset"), Description("Frames to jump, negative values jump backward.")> offset As Integer)
        Renderer.Position += offset
        tbPosition.Value = Renderer.Position
        Renderer.Draw()
        UpdateAll()
    End Sub

    <Command("Opens the help of the crop dialog.")>
    Sub ShowHelpDialog()
        Dim form As New HelpForm()
        form.Doc.WriteStart("Crop Dialog")
        form.Doc.WriteParagraph("The crop dialog allows to crop borders. Right-click to show a '''context menu''' with available features. By default StaxRip detects the crop values automatically. It's recommended to check the detected values visually. A crop value can be changed roughly by moving the mouse while the left mouse button is pressed. Alternative methods are using the '''mousewheel''' or keyboard shortcuts. The Ctrl key crops the active and opposite side, the Shift key crops 8 instead of 2 pixel.")
        form.Doc.WriteTips(CustomMenu.GetTips)
        form.Doc.WriteTable("Shortcut Keys", CustomMenu.GetKeys, False)
        form.Show()
    End Sub

    Sub bnMenu_Click(sender As Object, e As EventArgs) Handles bnMenu.Click
        cms.Show(bnMenu, 0, 0)
    End Sub
End Class
