
Imports StaxRip.UI
Imports StaxRip.VideoEncoderCommandLine

Public Class NVEncControl
    Inherits EncoderControlBase

    Private ReadOnly Encoder As NVEnc
    Private ReadOnly Params As NVEncParams
    Private ReadOnly QualityDefinitions As List(Of QualityItem)

    Sub New(enc As NVEnc)

        QualityDefinitions = If(s.NVEncCQualityDefinitions IsNot Nothing AndAlso s.NVEncCQualityDefinitions.Any(),
            s.NVEncCQualityDefinitions,
            New List(Of QualityItem) From {
                New QualityItem(10, "Super High", "Super high quality and file size"),
                New QualityItem(12, "Very High", "Very high quality and file size"),
                New QualityItem(14, "Higher", "Higher quality and file size"),
                New QualityItem(16, "High", "High quality and file size"),
                New QualityItem(18, "Medium", "Medium quality and file size"),
                New QualityItem(20, "Low", "Low quality and file size"),
                New QualityItem(22, "Lower", "Lower quality and file size"),
                New QualityItem(24, "Very Low", "Very low quality and file size"),
                New QualityItem(26, "Super Low", "Super low quality and file size"),
                New QualityItem(28, "Giga Low", "Giga low quality and file size"),
                New QualityItem(30, "Extreme Low", "Extreme low quality and file size"),
                New QualityItem(33, "Ultra Low", "Ultra low quality and file size")})

        Encoder = enc
        Params = Encoder.Params

        tblOverrideName.LeftClickAction = Sub(value)
                                              Params.OverrideTargetFileName.Value = value
                                              If value Then Encoder.UpdateTargetFile()
                                              UpdateControls()
                                          End Sub
        tblOverrideName.RightClickAction = Sub(value)
                                               Encoder.ShowConfigDialog(Params.OverrideTargetFileName)
                                           End Sub

        AddHandler Params.ValueChanged, AddressOf ParamsValueChanged
        AddHandler lv.UpdateContextMenu, AddressOf UpdateMenu

        UpdateControls()
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        RemoveHandler Params.ValueChanged, AddressOf ParamsValueChanged
        RemoveHandler lv.UpdateContextMenu, AddressOf UpdateMenu
        MyBase.Dispose(disposing)
    End Sub

    Sub ParamsValueChanged(item As CommandLineParam)
        If item Is Params.OverrideTargetFileName Then
            tblOverrideName.State = DirectCast(item, BoolParam).Value
        End If
    End Sub

    Sub UpdateMenu()
        cms.Items.ClearAndDisplose
        Dim quality = GetQualityParam()
        Dim offset = If(quality IsNot Nothing, 0, 1)
        Dim add = Sub(path As String, action As Action, isSelected As Boolean, help As String)
                      Dim item = MenuItemEx.Add(cms.Items, path & "  ", action, help)
                      item.Marked = isSelected
                  End Sub

        If lv.SelectedItems.Count > 0 Then
            Dim selectedIndex = lv.SelectedIndices(0)
            Select Case selectedIndex
                Case 0 - offset
                    For Each def In QualityDefinitions
                        Dim p = def.Value & If(Not String.IsNullOrWhiteSpace(def.Text), $": {def.Text}", "")
                        add(p, Sub() SetQuality(selectedIndex, def.Value), quality.Value = def.Value, def.Tooltip)
                    Next
                Case 1 - offset
                    Dim param = Params.Mode
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetMode(selectedIndex, temp), param.Value = temp, "")
                    Next
                Case 2 - offset
                    Dim param = Params.Preset
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetPreset(selectedIndex, temp), param.Value = temp, "")
                    Next
                Case 3 - offset
                    Dim param = Params.Tune
                    If Params.TuneH264.Visible Then param = Params.TuneH264
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetTune(selectedIndex, temp), param.Value = temp, "")
                    Next
                Case 4 - offset
                    Dim param = Params.OutputDepth
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetOutputDepth(selectedIndex, temp), param.Value = temp, "")
                    Next
                Case 5 - offset
                    Dim param = If(Params.DolbyVisionProfileAV1.Visible, Params.DolbyVisionProfileAV1, Params.DolbyVisionProfileH265)
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetDolbyVisionProfile(param, selectedIndex, temp), param.Value = temp, "")
                    Next
                Case 6 - offset
                    Dim param = Params.ColorRange
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetColorRange(selectedIndex, temp), param.Value = temp, "")
                    Next
                Case Else
                    Throw New NotSupportedException(NameOf(selectedIndex))
            End Select
        End If
    End Sub

    Sub SetMode(index As Integer, value As Integer)
        Params.Mode.Value = value

        lv.Items(index).SubItems(1).Tag = value.ToString()
        lv.Items(index).SubItems(1).Text = value.ToString()
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetQuality(index As Integer, v As Double)
        If Params.QVBR.Visible Then Params.QVBR.Value = v
        If Params.QVBRAV1.Visible Then Params.QVBRAV1.Value = v
        If Params.QP.Visible Then Params.QP.Value = v
        If Params.QPAV1.Visible Then Params.QPAV1.Value = v
        If Params.VbrQuality.Visible Then Params.VbrQuality.Value = v

        lv.Items(index).SubItems(1).Tag = GetQualityCaption(v)
        lv.Items(index).SubItems(1).Text = GetQualityCaption(v)
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetPreset(index As Integer, value As Integer)
        Params.Preset.Value = value

        lv.Items(index).SubItems(1).Tag = value.ToString()
        lv.Items(index).SubItems(1).Text = value.ToString()
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetTune(index As Integer, value As Integer)
        If Params.Tune.Visible Then Params.Tune.Value = value
        If Params.TuneH264.Visible Then Params.TuneH264.Value = value

        lv.Items(index).SubItems(1).Tag = value.ToString()
        lv.Items(index).SubItems(1).Text = value.ToString()
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetOutputDepth(index As Integer, value As Integer)
        Params.OutputDepth.Value = value

        lv.Items(index).SubItems(1).Tag = value.ToString()
        lv.Items(index).SubItems(1).Text = value.ToString()
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetDolbyVisionProfile(param As OptionParam, index As Integer, value As Integer)
        param.Value = value

        lv.Items(index).SubItems(1).Tag = value.ToString()
        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetColorRange(index As Integer, value As Integer)
        Params.ColorRange.Value = value

        lv.Items(index).SubItems(1).Tag = value.ToString()
        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub UpdateControls(item As CommandLineParam)
        UpdateControls()
    End Sub

    'the quality row and the menus of the rows depend on it,
    'the advanced QP settings have no single quality value
    Function GetQualityParam() As NumParam
        If Not Encoder.QualityMode Then
            Return Nothing
        End If

        Return {Params.QVBR, Params.QVBRAV1, Params.QP, Params.QPAV1, Params.VbrQuality}.LastOrDefault(Function(i) i.Visible)
    End Function

    Sub UpdateControls()
        lv.Items.Clear()
        Dim quality = GetQualityParam()

        If quality IsNot Nothing Then
            lv.Items.Add(New ListViewItem({"Quality", GetQualityCaption(quality.Value)}))
        End If

        lv.Items.Add(New ListViewItem({"Mode", Params.Mode.OptionText}))
        lv.Items.Add(New ListViewItem({"Preset", Params.Preset.OptionText}))
        Dim tuneParam = Params.Tune
        If Params.TuneH264.Visible Then tuneParam = Params.TuneH264
        lv.Items.Add(New ListViewItem({"Tune", tuneParam.OptionText}))
        lv.Items.Add(New ListViewItem({"Output Depth", Params.OutputDepth.OptionText}))
        lv.Items.Add(New ListViewItem({"DV Profile", If(Params.DolbyVisionProfileAV1.Visible, Params.DolbyVisionProfileAV1.OptionText, Params.DolbyVisionProfileH265.OptionText)}))
        lv.Items.Add(New ListViewItem({"Color Range", Params.ColorRange.OptionText}))

        tblOverrideName.State = Encoder.OverridesTargetFileName
        'blCompCheck.Visible = Not Encoder.QualityMode AndAlso Params.Decoder.Value = 0
        blCompCheck.Visible = False

        g.MainForm.UpdateEncoderStateRelatedControls()
    End Sub

    Function GetQualityCaption(value As Double) As String
        For Each def In QualityDefinitions
            If def.Value = value Then
                Return value.ToInvariantString() & If(Not String.IsNullOrWhiteSpace(def.Text), $": {def.Text}", "")
            End If
        Next

        Return value.ToInvariantString()
    End Function

    Sub llConfigCodec_Click(sender As Object, e As EventArgs) Handles blConfigCodec.Click
        Encoder.ShowConfigDialog()
    End Sub

    Sub llConfigContainer_Click(sender As Object, e As EventArgs) Handles blConfigContainer.Click
        Encoder.OpenMuxerConfigDialog()
    End Sub

    Sub llCompCheck_Click(sender As Object, e As EventArgs) Handles blCompCheck.Click
        Encoder.RunCompCheck()
    End Sub

    <Serializable>
    Public Class QualityItem
        Property Value As Double
        Property Text As String
        Property Tooltip As String

        Sub New(value As Double, text As String, tooltip As String)
            Me.Value = value
            Me.Text = text
            Me.Tooltip = tooltip
        End Sub
    End Class
End Class
