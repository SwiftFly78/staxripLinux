
Imports StaxRip.UI
Imports StaxRip.VideoEncoderCommandLine

Public Class x264Control
    Inherits EncoderControlBase

    Private Encoder As x264Enc
    Private Params As x264Params

    Private QualityDefinitions As List(Of QualityItem)

    Sub New(enc As x264Enc)
        MyBase.New()

        QualityDefinitions = If(s.X264QualityDefinitions IsNot Nothing AndAlso s.X264QualityDefinitions.Any(),
            s.X264QualityDefinitions,
            New List(Of QualityItem) From {
                New QualityItem(0, "Lossless", "Lossless quality but huge file size"),
                New QualityItem(14, "Super High", "Super high quality and file size"),
                New QualityItem(16, "Very High", "Very high quality and file size"),
                New QualityItem(18, "Higher", "Higher quality and file size"),
                New QualityItem(20, "High", "High quality and file size"),
                New QualityItem(22, "Medium", "Medium quality and file size"),
                New QualityItem(24, "Low", "Low quality and file size"),
                New QualityItem(26, "Lower", "Lower quality and file size"),
                New QualityItem(28, "Very Low", "Very low quality and file size"),
                New QualityItem(30, "Super Low", "Super low quality and file size")})

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
        Dim offset = If(Encoder.QualityMode, 0, 1)
        Dim add = Sub(path As String, action As Action, isSelected As Boolean, help As String)
                      Dim item = MenuItemEx.Add(cms.Items, path & "  ", action, help)
                      item.Marked = isSelected
                  End Sub

        If lv.SelectedItems.Count > 0 Then
            Dim selectedIndex = lv.SelectedIndices(0)
            Select Case selectedIndex
                Case 0 - offset
                    Dim param = Params.Quant
                    For Each def In QualityDefinitions
                        Dim p = def.Value & If(Not String.IsNullOrWhiteSpace(def.Text), $": {def.Text}", "")
                        add(p, Sub() SetQuality(selectedIndex, def.Value), param.Value = def.Value, def.Tooltip)
                    Next
                Case 1 - offset
                    Dim param = Params.Preset
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetPreset(selectedIndex, temp), param.Value = temp, "x264 slower compares to x265 medium")
                    Next
                Case 2 - offset
                    Dim param = Params.Tune
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetTune(selectedIndex, temp), param.Value = temp, "")
                    Next
            End Select
        End If
    End Sub

    Sub SetQuality(index As Integer, value As Double)
        Params.Quant.Value = value

        lv.Items(index).SubItems(1).Tag = GetQualityCaption(value)
        lv.Items(index).SubItems(1).Text = GetQualityCaption(value)
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetPreset(index As Integer, value As Integer)
        Params.Preset.Value = value

        Params.ApplyValues(True)
        Params.ApplyValues(False)

        lv.Items(index).SubItems(1).Tag = value.ToString
        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetTune(index As Integer, value As Integer)
        Params.Tune.Value = value

        Params.ApplyValues(True)
        Params.ApplyValues(False)

        lv.Items(index).SubItems(1).Tag = value.ToString
        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Function GetQualityCaption(value As Double) As String
        For Each def In QualityDefinitions
            If def.Value = value Then
                Return value.ToInvariantString() & If(Not String.IsNullOrWhiteSpace(def.Text), $" - {def.Text}", "")
            End If
        Next

        Return value.ToInvariantString()
    End Function

    Sub UpdateControls()
        If Encoder.QualityMode AndAlso lv.Items.Count < 4 Then
            lv.Items.Clear()
            lv.Items.Add(New ListViewItem({"Quality", GetQualityCaption(Params.Quant.Value)}))
            lv.Items.Add(New ListViewItem({"Preset", Params.Preset.OptionText}))
            lv.Items.Add(New ListViewItem({"Tune", Params.Tune.OptionText}))
        ElseIf Params.Mode.Value <> 2 AndAlso lv.Items.Count <> 3 Then
            lv.Items.Clear()
            lv.Items.Add(New ListViewItem({"Preset", Params.Preset.OptionText}))
            lv.Items.Add(New ListViewItem({"Tune", Params.Tune.OptionText}))
        End If

        tblOverrideName.State = Encoder.OverridesTargetFileName
        blCompCheck.Visible = Params.Mode.Value = x264RateMode.TwoPass Or Params.Mode.Value = x264RateMode.ThreePass

        g.MainForm.UpdateEncoderStateRelatedControls()
    End Sub

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
