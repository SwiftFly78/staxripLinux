
Imports System.Reflection
Imports StaxRip.UI
Imports StaxRip.VideoEncoderCommandLine

Public Class x265Control
    Inherits EncoderControlBase

    Private Encoder As x265Enc
    Private Params As x265Params
    Private QualityDefinitions As List(Of QualityItem)

    Sub New(enc As x265Enc)

        QualityDefinitions = If(s.X265QualityDefinitions IsNot Nothing AndAlso s.X265QualityDefinitions.Any(),
            s.X265QualityDefinitions,
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
                New QualityItem(28, "Extreme Low", "Extreme low quality and file size"),
                New QualityItem(30, "Ultra Low", "Ultra low quality and file size")})

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
                        add(param.Options(temp), Sub() SetPreset(selectedIndex, temp), param.Value = temp, "")
                    Next
                Case 2 - offset
                    Dim param = Params.Tune
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetTune(selectedIndex, temp), param.Value = temp, "")
                    Next
                Case 3 - offset
                    Dim param = Params.AQmode
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetAqMode(selectedIndex, temp), param.Value = temp, "")
                    Next
                Case 4 - offset
                    Dim param = Params.DolbyVisionProfile
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetDolbyVisionProfile(selectedIndex, temp), param.Value = temp, "")
                    Next
                Case 5 - offset
                    Dim param = Params.Range
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetRange(selectedIndex, temp), param.Value = temp, "")
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

        Params.ApplyPresetValues()
        Params.ApplyTuneValues()

        lv.Items(index).SubItems(1).Tag = value.ToString
        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetTune(index As Integer, value As Integer)
        Params.Tune.Value = value

        Params.ApplyPresetValues()
        Params.ApplyTuneValues()

        lv.Items(index).SubItems(1).Tag = value.ToString
        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetAqMode(index As Integer, value As Integer)
        Params.AQmode.Value = value

        lv.Items(index).SubItems(1).Tag = value.ToString
        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetDolbyVisionProfile(index As Integer, value As Integer)
        Params.DolbyVisionProfile.Value = value

        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetRange(index As Integer, value As Integer)
        Params.Range.Value = value

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
        lv.Items.Clear()
        If Encoder.QualityMode Then
            lv.Items.Add(New ListViewItem({"Quality", GetQualityCaption(Params.Quant.Value)}))
        End If
        lv.Items.Add(New ListViewItem({"Preset", Params.Preset.OptionText}))
        lv.Items.Add(New ListViewItem({"Tune", Params.Tune.OptionText}))
        lv.Items.Add(New ListViewItem({"AQ-Mode", Params.AQmode.OptionText}))
        lv.Items.Add(New ListViewItem({"DV Profile", Params.DolbyVisionProfile.OptionText}))
        lv.Items.Add(New ListViewItem({"Range", Params.Range.OptionText}))

        tblOverrideName.State = Encoder.OverridesTargetFileName
        blCompCheck.Visible = Params.Mode.Value = x265RateMode.TwoPass Or Params.Mode.Value = x265RateMode.ThreePass

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
