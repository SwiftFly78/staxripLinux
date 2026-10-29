
Imports StaxRip.UI
Imports StaxRip.VideoEncoderCommandLine

Public Class VvencffappControl
    Inherits EncoderControlBase

    Private Encoder As VvencffappEnc
    Private Params As VvencffappParams
    Private QualityDefinitions As List(Of QualityItem)

    Sub New(enc As VvencffappEnc)

        If Not s.VvencffappQualityDefinitions Is Nothing AndAlso s.VvencffappQualityDefinitions.Any() Then
            QualityDefinitions = s.VvencffappQualityDefinitions
        Else
            QualityDefinitions = New List(Of QualityItem) From {
                New QualityItem(16, "Super High", "Super high quality and file size"),
                New QualityItem(20, "Very High", "Very high quality and file size"),
                New QualityItem(24, "Higher", "Higher quality and file size"),
                New QualityItem(28, "High", "High quality and file size"),
                New QualityItem(32, "Medium (Default)", "Medium quality and file size (Default)"),
                New QualityItem(36, "Low", "Low quality and file size"),
                New QualityItem(40, "Lower", "Lower quality and file size"),
                New QualityItem(44, "Very Low", "Very low quality and file size"),
                New QualityItem(48, "Super Low", "Super low quality and file size"),
                New QualityItem(52, "Extreme Low", "Extreme low quality and file size"),
                New QualityItem(56, "Ultra Low", "Ultra low quality and file size")}
        End If

        Encoder = enc
        Params = Encoder.Params


        cbOverrideName.LeftClickAction = Sub(value)
                                              Params.OverrideTargetFileName.Value = value
                                              If value Then Encoder.UpdateTargetFile()
                                              UpdateControls()
                                          End Sub
        cbOverrideName.RightClickAction = Sub(value)
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
            cbOverrideName.State = DirectCast(item, BoolParam).Value
        End If
    End Sub

    Sub UpdateMenu()
        cms.Items.ClearAndDisplose
        Dim offset = If(Params.Mode.Value = VvencffappRateMode.SingleQuant, 0, 1)
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
        Dim offset = If(Params.Mode.Value = VvencffappRateMode.SingleQuant, 0, 1)

        Params.Preset.Value = value

        Params.ApplyPresetValues()

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
        lv.Items.Clear()

        If Params.Mode.Value = VvencffappRateMode.SingleQuant Then
            lv.Items.Add(New ListViewItem({"Quality", GetQualityCaption(Params.Quant.Value)}))
        End If

        lv.Items.Add(New ListViewItem({"Preset", Params.Preset.OptionText}))

        cbOverrideName.State = Encoder.OverridesTargetFileName
        blCompCheck.Visible = Params.Mode.Value = VvencffappRateMode.TwoPass

        g.MainForm.UpdateEncoderStateRelatedControls()
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
