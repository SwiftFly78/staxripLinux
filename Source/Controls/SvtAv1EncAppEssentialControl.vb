
Imports StaxRip.UI
Imports StaxRip.VideoEncoderCommandLine

Public Class SvtAv1EncAppEssentialControl
    Inherits EncoderControlBase

    Private ReadOnly Encoder As SvtAv1EssentialEnc
    Private ReadOnly Params As SvtAv1EssentialEncParams
    Private ReadOnly QualityDefinitions As List(Of SvtAv1EncAppControl.QualityItem)

    Sub New(enc As SvtAv1EssentialEnc)

        QualityDefinitions = If(s.SvtAv1EncAppQualityDefinitions IsNot Nothing AndAlso s.SvtAv1EncAppQualityDefinitions.Any(),
            s.SvtAv1EncAppQualityDefinitions,
            New List(Of SvtAv1EncAppControl.QualityItem) From {
                New SvtAv1EncAppControl.QualityItem(15, "Incredible High", "Incredible high quality and file size"),
                New SvtAv1EncAppControl.QualityItem(20, "Ultra High", "Ultra high quality and file size"),
                New SvtAv1EncAppControl.QualityItem(25, "Extreme High", "Extreme high quality and file size"),
                New SvtAv1EncAppControl.QualityItem(30, "Super High", "Super high quality and file size"),
                New SvtAv1EncAppControl.QualityItem(33, "Very High", "Very high quality and file size"),
                New SvtAv1EncAppControl.QualityItem(35, "Higher", "Higher quality and file size"),
                New SvtAv1EncAppControl.QualityItem(38, "High", "High quality and file size"),
                New SvtAv1EncAppControl.QualityItem(40, "Medium", "Medium quality and file size"),
                New SvtAv1EncAppControl.QualityItem(42, "Low", "Low quality and file size"),
                New SvtAv1EncAppControl.QualityItem(45, "Lower", "Lower quality and file size"),
                New SvtAv1EncAppControl.QualityItem(47, "Very Low", "Very low quality and file size"),
                New SvtAv1EncAppControl.QualityItem(50, "Super Low", "Super low quality and file size"),
                New SvtAv1EncAppControl.QualityItem(55, "Extreme Low", "Extreme low quality and file size"),
                New SvtAv1EncAppControl.QualityItem(60, "Ultra Low", "Ultra low quality and file size")})

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
        Dim offset = If(Params.RateControlMode.Value = SvtAv1EncAppRateMode.Quality, 0, 1)
        Dim add = Sub(path As String, action As Action, isSelected As Boolean, help As String)
                      Dim item = MenuItemEx.Add(cms.Items, path & "  ", action, help)
                      'Dim item = cms.Add(path & "  ", action, help)
                      item.Marked = isSelected
                  End Sub

        If lv.SelectedItems.Count > 0 Then
            Dim selectedIndex = lv.SelectedIndices(0)
            Select Case selectedIndex
                Case 0 - offset
                    Dim qualityParam = Params.Quality
                    Dim quantParam = If(Params.QuantizationParameterLow.Visible, Params.QuantizationParameterLow, Params.QuantizationParameterHigh)
                    Dim isCrf = Params.ConstantRateFactorHigh.Visible OrElse Params.ConstantRateFactorLow.Visible
                    For x = 1 To qualityParam.Options.Length - 1
                        Dim temp = x
                        add("Quality | " + qualityParam.Options(temp), Sub() SetQuality(selectedIndex, temp), qualityParam.Value > 0 AndAlso qualityParam.Value = temp, "")
                    Next
                    For x = 0 To QualityDefinitions.Count - 1
                        Dim temp = QualityDefinitions(x)
                        Dim p = If(isCrf, "CRF | ", "QP | ") & temp.Value & If(Not String.IsNullOrWhiteSpace(temp.Text), $": {temp.Text}", "")
                        add(p, Sub() SetQuant(selectedIndex, temp.Value), qualityParam.Value = 0 AndAlso Math.Abs(quantParam.Value - temp.Value) < 0.1, temp.Tooltip)
                    Next
                Case 1 - offset
                    Dim speedParam = If(Params.SpeedHigh.Visible, Params.SpeedHigh, Params.SpeedLow)
                    Dim presetParam = Params.Preset
                    For x = 1 To speedParam.Options.Length - 1
                        Dim temp = x
                        add("Speed | " + speedParam.Options(temp), Sub() SetSpeed(selectedIndex, temp), speedParam.Value > 0 AndAlso speedParam.Value = temp, "")
                    Next
                    For x = 0 To presetParam.Options.Length - 1
                        Dim temp = x
                        add("Preset | " + presetParam.Options(temp), Sub() SetPreset(selectedIndex, temp), speedParam.Value = 0 AndAlso presetParam.Value = temp, "")
                    Next
                Case 2 - offset
                    Dim param = Params.Tune
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetTune(selectedIndex, temp), param.Value = temp, "")
                    Next
                Case 3 - offset
                    Dim param = Params.FastDecode
                    For x = 0 To param.Options.Length - 1
                        Dim temp = x
                        add(param.Options(temp), Sub() SetFastDecode(selectedIndex, temp), param.Value = temp, "")
                    Next
                Case 4 - offset
                    Dim param = Params.Lookahead
                    For x = param.Config(0) To param.Config(1) Step param.Config(2)
                        Dim temp = CInt(x)
                        Dim lowerBound = Math.Floor((temp - 1) / 10) * 10 + 1
                        Dim upperBound = lowerBound + 9
                        Dim category = If(temp <= 0, "", $"{lowerBound:00} - {upperBound:00} | ")
                        Dim def = If(temp = CInt(param.InitialValue), "  (default)", "")
                        Dim p = category + temp.ToInvariantString() + def
                        add(p, Sub() SetLookahead(selectedIndex, temp), param.Value = temp, "")
                    Next
                Case 5 - offset
                    Dim param = Params.FilmGrain
                    For x = param.Config(0) To param.Config(1) Step param.Config(2)
                        Dim temp = CInt(x)
                        Dim lowerBound = Math.Floor((temp - 1) / 10) * 10 + 1
                        Dim upperBound = lowerBound + 9
                        Dim category = If(temp = 0, "", $"{lowerBound:00} - {upperBound:00} | ")
                        Dim def = If(temp = CInt(param.InitialValue), "  (default)", "")
                        Dim p = category + temp.ToInvariantString() + def
                        add(p, Sub() SetFilmGrain(selectedIndex, temp), param.Value = temp, "")
                    Next
                Case Else
                    Throw New NotSupportedException(NameOf(selectedIndex))
            End Select
        End If
    End Sub

    Sub SetQuality(index As Integer, value As Integer)
        Params.Quality.Value = value

        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetQuant(index As Integer, value As Double)
        Params.Quality.Value = 0

        Dim quantParam = If(Params.QuantizationParameterLow.Visible, Params.QuantizationParameterLow, Params.QuantizationParameterHigh)
        quantParam.Value = Math.Min(value, quantParam.Config(1))
        quantParam.ValueChangedAction.Invoke(Math.Min(value, quantParam.Config(1)))

        Dim crfParam = If(Params.ConstantRateFactorLow.Visible, Params.ConstantRateFactorLow, Params.ConstantRateFactorHigh)
        crfParam.Value = Math.Min(value, crfParam.Config(1))
        crfParam.ValueChangedAction.Invoke(Math.Min(value, crfParam.Config(1)))

        lv.Items(index).SubItems(1).Text = GetQualityCaption(value)
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetSpeed(index As Integer, value As Integer)
        Params.SpeedLow.Value = value
        Params.SpeedHigh.Value = value

        Params.ApplyPresetValues()

        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetPreset(index As Integer, value As Integer)
        Params.SpeedLow.Value = 0
        Params.SpeedHigh.Value = 0
        Params.Preset.Value = value

        Params.ApplyPresetValues()

        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetTune(index As Integer, value As Integer)
        Params.Tune.Value = value

        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetFastDecode(index As Integer, value As Integer)
        Params.FastDecode.Value = value

        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetLookahead(index As Integer, value As Integer)
        Params.Lookahead.Value = value

        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Sub SetFilmGrain(index As Integer, value As Integer)
        Params.FilmGrain.Value = value

        lv.Items(index).SubItems(1).Text = value.ToString
        lv.Items(index).Selected = False

        UpdateControls()
        Encoder.UpdateTargetFile()
    End Sub

    Function GetQualityCaption(value As Double) As String
        For Each def In QualityDefinitions
            If def.Value = value Then
                Return value.ToInvariantString() & If(Not String.IsNullOrWhiteSpace(def.Text), $": {def.Text}", "")
            End If
        Next

        Return value.ToInvariantString()
    End Function

    Sub UpdateControls(item As CommandLineParam)
        UpdateControls()
    End Sub

    Sub UpdateControls()
        Dim offset = If(Params.RateControlMode.Value = SvtAv1EncAppRateMode.Quality, 0, 1)

        lv.Items.Clear()
        If offset = 0 Then
            Dim qualityParam = Params.Quality
            If qualityParam.Value > 0 Then
                lv.Items.Add(New ListViewItem({"Quality", qualityParam.OptionText}))
            Else
                Dim crfParam = If(Params.ConstantRateFactorLow.Visible, Params.ConstantRateFactorLow, Params.ConstantRateFactorHigh)
                Dim quantParam = If(Params.QuantizationParameterLow.Visible, Params.QuantizationParameterLow, Params.QuantizationParameterHigh)
                Dim paramValue = If(crfParam.Visible, crfParam.Value, quantParam.Value)

                lv.Items.Add(New ListViewItem({"Quality", GetQualityCaption(paramValue)}))
            End If
        End If
        Dim speedParam = If(Params.SpeedHigh.Visible, Params.SpeedHigh, Params.SpeedLow)
        If speedParam.Value > 0 Then
            lv.Items.Add(New ListViewItem({"Speed", speedParam.OptionText}))
        Else
            Dim presetParam = Params.Preset
            lv.Items.Add(New ListViewItem({"Preset", presetParam.OptionText}))
        End If
        lv.Items.Add(New ListViewItem({"Tune", Params.Tune.OptionText}))
        lv.Items.Add(New ListViewItem({"Fast Decode", Params.FastDecode.OptionText}))
        lv.Items.Add(New ListViewItem({"Lookahead", Params.Lookahead.Value.ToInvariantString() + If(Params.Lookahead.Value = Params.Lookahead.InitialValue, " (default)", "")}))
        lv.Items.Add(New ListViewItem({"Film Grain", Params.FilmGrain.Value.ToInvariantString() + If(Params.FilmGrain.Value = Params.FilmGrain.InitialValue, " (default)", "")}))

        cbOverrideName.State = Encoder.OverridesTargetFileName
        blCompCheck.Visible = Params.RateControlMode.Value <> SvtAv1EncAppRateMode.Quality

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
