
Imports StaxRip.VideoEncoderCommandLine
Imports StaxRip.UI
Imports System.Text
Imports System.Threading.Tasks
Imports System.Threading

Public Class AudioForm
    Inherits DialogBase

    Public WithEvents numQuality, numBitrate, numDelay, numGain As NumEdit
    Public WithEvents mbCodec, mbLanguage, mbChannels, mbSamplingRate, mbEncoder, mbDecoder As MenuButton
    Public lQualiy, lCodec, lLanguage, lDelay, lChannels, laProfileName, Label1, Label2, Label3, Label4, laStreamName, laCustom, laDecoder As LabelEx
    Public WithEvents tbProfileName, tbStreamName, tbCustom As TextEdit
    Public TipProvider As TipProvider
    Public WithEvents bnOK, bnCancel, bnMenu, bnAdvanced As ButtonEx
    Public WithEvents SimpleUI As SimpleUI
    Public WithEvents rtbCommandLine As CommandLineRichTextBox
    Public WithEvents cbForcedTrack, cbDefaultTrack, cbNormalize, cbCenterOptimizedStereo, cbCommentaryTrack As CheckBoxEx
    Public gbBasic, gbAdvanced As GroupBox

    Private Sub InitializeComponent()
        Dim label = Function(text As String) New LabelEx With {.Text = text}

        lCodec = label("Codec:")
        mbCodec = New MenuButton With {.ShowMenuSymbol = True}
        laDecoder = label("Decoder:")
        mbDecoder = New MenuButton With {.ShowMenuSymbol = True}
        Label2 = label("Encoder:")
        mbEncoder = New MenuButton With {.ShowMenuSymbol = True}
        lChannels = label("Channels:")
        mbChannels = New MenuButton With {.ShowMenuSymbol = True}
        Label1 = label("Sample Rate:")
        mbSamplingRate = New MenuButton With {.ShowMenuSymbol = True}
        lLanguage = label("Language:")
        mbLanguage = New MenuButton With {.ShowMenuSymbol = True}
        laProfileName = label("Profile Name:")
        tbProfileName = New TextEdit
        laStreamName = label("Track Name:")
        tbStreamName = New TextEdit
        laCustom = label("Custom:")
        tbCustom = New TextEdit
        Label3 = label("Bitrate:")
        numBitrate = New NumEdit
        lQualiy = label("Quality:")
        numQuality = New NumEdit With {.DecimalPlaces = 2, .Increment = 0.1}
        Label4 = label("Gain:")
        numGain = New NumEdit With {.Minimum = -100, .Maximum = 100}
        lDelay = label("Delay:")
        numDelay = New NumEdit With {.Minimum = -100000, .Maximum = 100000}
        cbNormalize = New CheckBoxEx With {.Text = "Normalize"}
        cbCenterOptimizedStereo = New CheckBoxEx With {.Text = "Center/Speech optimized Stereo"}
        cbDefaultTrack = New CheckBoxEx With {.Text = "Default Track"}
        cbForcedTrack = New CheckBoxEx With {.Text = "Forced Track"}
        cbCommentaryTrack = New CheckBoxEx With {.Text = "Commentary Track"}
        SimpleUI = New SimpleUI
        bnAdvanced = New ButtonEx("More...")
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        bnMenu = New ButtonEx With {.ShowMenuSymbol = True}
        rtbCommandLine = New CommandLineRichTextBox With {.ReadOnly = True}
        TipProvider = New TipProvider

        Dim basic As New TableLayout(
            Row(lCodec, Scaled(mbCodec), Label3, Scaled(numBitrate)),
            Row(laDecoder, Scaled(mbDecoder), lQualiy, Scaled(numQuality)),
            Row(Label2, Scaled(mbEncoder), Label4, Scaled(numGain)),
            Row(lChannels, Scaled(mbChannels), lDelay, Scaled(numDelay)),
            Row(Label1, Scaled(mbSamplingRate), Nothing, Nothing),
            Row(lLanguage, Scaled(mbLanguage), Nothing, Nothing),
            Row(laProfileName, Scaled(tbProfileName), Nothing, Nothing),
            Row(laStreamName, Scaled(tbStreamName), Nothing, Nothing),
            Row(laCustom, Scaled(tbCustom), Nothing, Nothing),
            Row(cbNormalize, Nothing, Nothing, Nothing),
            Row(cbCenterOptimizedStereo, Nothing, Nothing, Nothing),
            Row(cbDefaultTrack, Nothing, Nothing, Nothing),
            Row(cbForcedTrack, Nothing, Nothing, Nothing),
            Row(cbCommentaryTrack, Nothing, Nothing, Nothing),
            New TableRow With {.ScaleHeight = True}) With {.Spacing = DefaultSpacing}

        gbBasic = New GroupBox With {.Text = "Basic", .Content = basic}
        gbAdvanced = New GroupBox With {
            .Text = "Advanced",
            .Content = New TableLayout(ScaledRow(SimpleUI), Row(HRow(bnAdvanced))) With {.Spacing = DefaultSpacing}}

        Content = PaddedTable(
            ScaledRow(New TableLayout(ScaledRow(Scaled(gbBasic), Scaled(gbAdvanced))) With {.Spacing = DefaultSpacing}),
            Row(rtbCommandLine),
            Row(ButtonRow(bnMenu, bnOK, bnCancel)))

        AbortButton = bnCancel
        Text = $"Audio Settings - {g.DefaultCommands.GetApplicationDetails()}"
    End Sub

    Private Profile, TempProfile As GUIAudioProfile
    Private CommandLineHighlightingMenuItem As MenuItemEx
    Private BlockUpdate As Boolean = False

    Sub New()
        MyBase.New()
        InitializeComponent()
        RestoreClientSize(55, 35)

        numBitrate.Minimum = 1
        numBitrate.Maximum = 16000
        numGain.DecimalPlaces = 1

        'cbDefaultTrack.Visible = TypeOf p.VideoEncoder.Muxer Is MkvMuxer
        cbForcedTrack.Visible = TypeOf p.VideoEncoder.Muxer Is MkvMuxer
        cbCommentaryTrack.Visible = TypeOf p.VideoEncoder.Muxer Is MkvMuxer

        Dim cms As New ContextMenuStripEx With {
            .Form = Me
        }
        bnMenu.ContextMenuStrip = cms

        cms.Add("Execute Command Line", AddressOf Execute)
        cms.Add("Copy Command Line", Sub() Clipboard.SetText(TempProfile.GetCommandLine(True)))
        cms.Add("Show Command Line...", Sub() g.ShowCommandLinePreview("Command Line", TempProfile.GetCommandLine(True), False))
        cms.Add("-")

        Dim a = Sub()
                    CommandLineHighlightingMenuItem.Marked = Not CommandLineHighlightingMenuItem.Marked
                    s.CommandLineHighlighting = CommandLineHighlightingMenuItem.Marked
                    rtbCommandLine.Format(rtbCommandLine.Text.ToString)
                End Sub

        CommandLineHighlightingMenuItem = cms.Add("Command Line Highlighting", a, Keys.Control Or Keys.H)
        CommandLineHighlightingMenuItem.Marked = s.CommandLineHighlighting

        cms.Add("-")
        cms.Add("Save Profile...", AddressOf SaveProfile, "Saves the current settings as profile")
        cms.Add("-")
        cms.Add("Help", AddressOf ShowHelp)
        cms.Add("eac3to Help", Sub() g.ShellExecute("https://en.wikibooks.org/wiki/Eac3to"))
        cms.Add("ffmpeg Help", Sub() Package.ffmpeg.ShowHelp())

        TipProvider.SetTip("Defines which decoder to use and forces decoding even if not necessary.", laDecoder, mbDecoder)
        TipProvider.SetTip("Profile name that is auto generated when undefined.", laProfileName)
        TipProvider.SetTip("Language used by the muxer. Saved in projects/templates but not in profiles.", mbLanguage, lLanguage)
        TipProvider.SetTip("Delay in milliseconds. eac3to handles delay, ffmpeg don't but it is handled by the muxer. Saved in projects/templates but not in profiles.", numDelay, lDelay)
        TipProvider.SetTip("Track name used by the muxer.", tbStreamName, laStreamName)
        TipProvider.SetTip("Custom command line arguments.", tbCustom, laCustom)
        TipProvider.SetTip("Default MKV Track.", cbDefaultTrack)
        TipProvider.SetTip("Forced MKV Track.", cbForcedTrack)
        TipProvider.SetTip("Commentary MKV Track.", cbCommentaryTrack)
    End Sub

    Protected Overrides Sub OnClosed(e As EventArgs)
        If DialogResult = DialogResult.OK Then
            SetValues(Profile)
        End If

        MyBase.OnClosed(e)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        g.PopulateLanguagesAsync(mbLanguage)
        UpdateControls()
    End Sub

    Sub SetValues(gap As GUIAudioProfile)
        gap.Bitrate = TempProfile.Bitrate
        gap.Language = TempProfile.Language
        gap.Delay = TempProfile.Delay
        gap.Name = TempProfile.Name
        gap.StreamName = TempProfile.StreamName
        gap.Gain = TempProfile.Gain
        gap.Default = TempProfile.Default
        gap.Forced = TempProfile.Forced
        gap.Commentary = TempProfile.Commentary
        gap.Params = TempProfile.Params
        gap.Decoder = TempProfile.Decoder
        gap.DecodingMode = TempProfile.DecodingMode
        gap.ExtractDTSCore = TempProfile.ExtractDTSCore
    End Sub

    Sub UpdateBitrate()
        nudQuality_ValueChanged(numQuality)
    End Sub

    Sub nudQuality_ValueChanged(numEdit As NumEdit) Handles numQuality.ValueChanged
        If TempProfile IsNot Nothing Then
            TempProfile.Params.Quality = CSng(numQuality.Value)
            numBitrate.Value = TempProfile.GetBitrate
            If Not BlockUpdate Then UpdateControls()
        End If
    End Sub

    Sub nudBitrate_ValueChanged(numEdit As NumEdit) Handles numBitrate.ValueChanged
        If TempProfile IsNot Nothing Then
            TempProfile.Bitrate = CSng(numBitrate.Value)
            If Not BlockUpdate Then UpdateControls()
        End If
    End Sub

    Sub nudDelay_ValueChanged(numEdit As NumEdit) Handles numDelay.ValueChanged
        TempProfile.Delay = CInt(numDelay.Value)
        If Not BlockUpdate Then UpdateControls()
    End Sub

    Sub numGain_ValueChanged(numEdit As NumEdit) Handles numGain.ValueChanged
        TempProfile.Gain = CSng(numGain.Value)
        If Not BlockUpdate Then UpdateControls()
    End Sub

    Sub SimpleUIValueChanged()
        SimpleUI.Save()
        UpdateControls()
    End Sub

    Sub UpdateEncoderMenu()
        Dim list As New List(Of GuiAudioEncoder) From {
            GuiAudioEncoder.Automatic
        }

        Select Case TempProfile.Params.Codec
            Case AudioCodec.None
            Case AudioCodec.AAC
                list.Add(GuiAudioEncoder.eac3to)
                list.Add(GuiAudioEncoder.fdkaac)
                list.Add(GuiAudioEncoder.ffmpeg)
            Case AudioCodec.AC3
                list.Add(GuiAudioEncoder.deezy)
                list.Add(GuiAudioEncoder.eac3to)
                list.Add(GuiAudioEncoder.ffmpeg)
            Case AudioCodec.AC4
                list.Add(GuiAudioEncoder.deezy)
            Case AudioCodec.DTS
                list.Add(GuiAudioEncoder.eac3to)
            Case AudioCodec.EAC3
                list.Add(GuiAudioEncoder.deezy)
                list.Add(GuiAudioEncoder.ffmpeg)
            Case AudioCodec.FLAC
                list.Add(GuiAudioEncoder.eac3to)
                list.Add(GuiAudioEncoder.ffmpeg)
            Case AudioCodec.MP3
                list.Add(GuiAudioEncoder.ffmpeg)
            Case AudioCodec.Opus
                list.Add(GuiAudioEncoder.ffmpeg)
                list.Add(GuiAudioEncoder.opusenc)
            Case AudioCodec.Vorbis
                list.Add(GuiAudioEncoder.ffmpeg)
            Case AudioCodec.W64, AudioCodec.WAV
                list.Add(GuiAudioEncoder.eac3to)
                list.Add(GuiAudioEncoder.ffmpeg)
            Case Else
                Throw New NotImplementedException("UpdateEncoderMenu")
        End Select

        mbEncoder.Clear()

        For Each item As GuiAudioEncoder In list
            mbEncoder.Add([Enum].GetName(GetType(GuiAudioEncoder), item), item)
        Next

        mbEncoder.Value = If(list.Contains(TempProfile.Params.Encoder), TempProfile.Params.Encoder, GuiAudioEncoder.Automatic)
    End Sub

    Sub UpdateControls()
        If TempProfile.ExtractCore Then
            numQuality.Enabled = False
            numBitrate.Enabled = False
        Else
            Select Case TempProfile.Params.Codec
                Case AudioCodec.Opus, AudioCodec.FLAC, AudioCodec.W64, AudioCodec.WAV, AudioCodec.DTS
                    numQuality.Enabled = False
                Case Else
                    numQuality.Enabled = TempProfile.Params.RateMode = AudioRateMode.VBR
            End Select

            numBitrate.Enabled = Not {AudioCodec.FLAC, AudioCodec.W64, AudioCodec.WAV}.ContainsEx(TempProfile.Params.Codec) AndAlso Not numQuality.Enabled
        End If

        UpdateEncoderMenu()

#Region "SamplingRate"
        Dim oldSamplingRateValue = mbSamplingRate.Value

        If TempProfile.AudioCodec = AudioCodec.Opus Then
            mbSamplingRate.Clear()
            mbSamplingRate.Add("Original", 0)
            mbSamplingRate.Add("8000 Hz", 8000)
            mbSamplingRate.Add("12000 Hz", 12000)
            mbSamplingRate.Add("16000 Hz", 16000)
            mbSamplingRate.Add("24000 Hz", 24000)
            mbSamplingRate.Add("48000 Hz", 48000)
            mbSamplingRate.Value = oldSamplingRateValue

            If Not mbSamplingRate.Items.Cast(Of Integer).Contains(CInt(mbSamplingRate.Value)) Then
                mbSamplingRate.Value = 0
                TempProfile.Params.SamplingRate = 0
            End If
        Else
            mbSamplingRate.Clear()
            mbSamplingRate.Add("Original", 0)
            mbSamplingRate.Add("11025 Hz", 11025)
            mbSamplingRate.Add("22050 Hz", 22050)
            mbSamplingRate.Add("44100 Hz", 44100)
            mbSamplingRate.Add("48000 Hz", 48000)
            mbSamplingRate.Add("88200 Hz", 88200)
            mbSamplingRate.Add("96000 Hz", 96000)
            mbSamplingRate.Value = oldSamplingRateValue

            If Not mbSamplingRate.Items.Cast(Of Integer).Contains(CInt(mbSamplingRate.Value)) Then
                mbSamplingRate.Value = 0
                TempProfile.Params.SamplingRate = 0
            End If
        End If
#End Region

        Dim enc = TempProfile.GetEncoder()

        mbDecoder.Enabled = Not TempProfile.ExtractCore
        mbChannels.Enabled = Not TempProfile.ExtractCore AndAlso enc <> GuiAudioEncoder.opusenc AndAlso enc <> GuiAudioEncoder.deezy
        mbSamplingRate.Enabled = Not TempProfile.ExtractCore AndAlso enc <> GuiAudioEncoder.opusenc
        cbNormalize.Enabled = Not TempProfile.ExtractCore AndAlso enc <> GuiAudioEncoder.deezy
        cbCenterOptimizedStereo.Enabled = Not TempProfile.ExtractCore AndAlso {AudioDecoderMode.ffmpeg, AudioDecoderMode.Automatic}.Contains(TempProfile.Decoder) AndAlso enc <> GuiAudioEncoder.deezy AndAlso ((TempProfile.Params.Codec <> AudioCodec.Opus AndAlso TempProfile.Params.ChannelsMode = ChannelsMode._2) OrElse (TempProfile.Params.Codec = AudioCodec.Opus AndAlso TempProfile.Params.OpusencDownmix = OpusDownmix.Stereo))
        numGain.Enabled = Not TempProfile.ExtractCore

#Region "Bitrate"
        Dim setIncrement = Sub(bitrate As Double, increment As Double)
                               If CInt(numBitrate.Value) >= bitrate Then numBitrate.UpIncrement = increment
                               If CInt(numBitrate.Value) > bitrate Then numBitrate.DownIncrement = increment
                           End Sub

        If TempProfile.Params.Codec = AudioCodec.AC3 Then
            If enc = GuiAudioEncoder.deezy Then
                numBitrate.Increment = 8
                setIncrement(128, 32)
                setIncrement(256, 64)
                numBitrate.Maximum = 640
                numBitrate.Minimum = 96
                If {6}.Contains(TempProfile.SourceChannels) OrElse TempProfile.Params.DeezyChannelsDd = DeezyChannelsDd._6 Then numBitrate.Minimum = 224
            Else
                numBitrate.Increment = 32
                setIncrement(320, 64)
                numBitrate.Maximum = 1664
                numBitrate.Minimum = 32
            End If
        ElseIf TempProfile.Params.Codec = AudioCodec.AC4 Then
            numBitrate.Increment = 8
            setIncrement(72, 40)
            setIncrement(112, 32)
            setIncrement(144, 112)
            setIncrement(256, 64)
            numBitrate.Maximum = 320
            numBitrate.Minimum = 64
        ElseIf TempProfile.Params.Codec = AudioCodec.EAC3 Then
            If enc = GuiAudioEncoder.deezy Then
                If TempProfile.Params.DeezyDdpMode = DeezyDdpMode.Ddp Then
                    numBitrate.Increment = 8
                    numBitrate.Maximum = 1024
                    If {1}.Contains(TempProfile.SourceChannels) OrElse {DeezyChannelsDdp._1}.Contains(TempProfile.Params.DeezyChannelsDdp) Then
                        setIncrement(128, 16)
                        setIncrement(400, 48)
                        setIncrement(448, 64)
                        setIncrement(960, 48)
                        setIncrement(1008, 16)
                        numBitrate.Minimum = 32
                    ElseIf {2}.Contains(TempProfile.SourceChannels) OrElse {DeezyChannelsDdp._2}.Contains(TempProfile.Params.DeezyChannelsDdp) Then
                        setIncrement(128, 16)
                        setIncrement(400, 48)
                        setIncrement(448, 64)
                        setIncrement(960, 48)
                        setIncrement(1008, 16)
                        numBitrate.Minimum = 96
                    ElseIf {6}.Contains(TempProfile.SourceChannels) OrElse {DeezyChannelsDdp._6}.Contains(TempProfile.Params.DeezyChannelsDdp) Then
                        setIncrement(256, 16)
                        setIncrement(400, 48)
                        setIncrement(448, 64)
                        setIncrement(960, 48)
                        setIncrement(1008, 16)
                        numBitrate.Minimum = 192
                    Else
                        numBitrate.Increment = 64
                        setIncrement(960, 48)
                        setIncrement(1008, 16)
                        numBitrate.Minimum = 384
                    End If
                ElseIf TempProfile.Params.DeezyDdpMode = DeezyDdpMode.DdpBluray Then
                    numBitrate.Increment = 256
                    setIncrement(1536, 128)
                    numBitrate.Maximum = 1664
                    numBitrate.Minimum = 768
                ElseIf TempProfile.Params.DeezyDdpMode = DeezyDdpMode.Atmos Then
                    If TempProfile.Params.DeezyChannelsAtmos = DeezyChannelsAtmos._6 Then
                        numBitrate.Increment = 64
                        setIncrement(640, 128)
                        setIncrement(768, 256)
                        numBitrate.Maximum = 1024
                        numBitrate.Minimum = 384
                    ElseIf TempProfile.Params.DeezyChannelsAtmos = DeezyChannelsAtmos._8 Then
                        numBitrate.Increment = 128
                        setIncrement(1408, 104)
                        setIncrement(1512, 24)
                        setIncrement(1536, 128)
                        numBitrate.Maximum = 1664
                        numBitrate.Minimum = 1152
                    Else
                        Throw New NotImplementedException("AudioForm.UpdateControls()")
                    End If
                Else
                    Throw New NotImplementedException("AudioForm.UpdateControls()")
                End If
            Else
                numBitrate.UpIncrement = If(CInt(numBitrate.Value) >= 320, 64, 32)
                numBitrate.DownIncrement = If(CInt(numBitrate.Value) > 320, 64, 32)
                numBitrate.Maximum = 1664
                numBitrate.Minimum = 32
            End If
        Else
            numBitrate.Increment = 1
            numBitrate.Maximum = 10_000
            numBitrate.Minimum = 1
        End If
        numBitrate.UpdateValue()
#End Region

        tbProfileName.SendMessageCue(TempProfile.Name, False)
        rtbCommandLine.SetText(TempProfile.GetCommandLine(False))
        rtbCommandLine.UpdateHeight()
    End Sub

    Sub mbCodec_ValueChangedUser() Handles mbCodec.ValueChangedUser
        TempProfile.Params.Codec = mbCodec.GetValue(Of AudioCodec)()

        Select Case TempProfile.Params.Codec
            Case AudioCodec.AAC
                Select Case TempProfile.Params.Encoder
                    Case GuiAudioEncoder.qaac, GuiAudioEncoder.Automatic
                        SetQuality(50)
                    Case GuiAudioEncoder.eac3to
                        SetQuality(0.5)
                    Case Else
                        SetQuality(3)
                End Select

                TempProfile.Params.RateMode = AudioRateMode.VBR
            Case AudioCodec.AC3, AudioCodec.EAC3
                numBitrate.Value = If(TempProfile.Channels = 6, 448, 224)
                TempProfile.Params.RateMode = AudioRateMode.CBR
            Case AudioCodec.AC4
                numBitrate.Value = 256
                TempProfile.Params.RateMode = AudioRateMode.CBR
            Case AudioCodec.FLAC
                numBitrate.Value = TempProfile.GetBitrate
                TempProfile.Params.RateMode = AudioRateMode.CBR
            Case AudioCodec.DTS
                numBitrate.Value = If(TempProfile.Channels = 6, 1536, 768)
                TempProfile.Params.RateMode = AudioRateMode.CBR
            Case AudioCodec.MP3
                SetQuality(4)
                TempProfile.Params.RateMode = AudioRateMode.VBR
            Case AudioCodec.Vorbis
                SetQuality(1)
                TempProfile.Params.RateMode = AudioRateMode.VBR
            Case AudioCodec.Opus
                numBitrate.Value = 80
        End Select

        UpdateBitrate()
        TempProfile.GetCommandLine(False) 'set encoder
        LoadAdvanced()
        If Not BlockUpdate Then UpdateControls()
    End Sub

    Sub SetQuality(value As Single)
        If TempProfile.Params.Codec = AudioCodec.AAC AndAlso
            TempProfile.Params.Encoder = GuiAudioEncoder.qaac OrElse
            TempProfile.Params.Encoder = GuiAudioEncoder.Automatic Then

            numQuality.Minimum = 0
            numQuality.Maximum = 127
            numQuality.Increment = 1
            numQuality.DecimalPlaces = 0
        ElseIf TempProfile.Params.Codec = AudioCodec.AAC Then
            Select Case TempProfile.Params.Encoder
                Case GuiAudioEncoder.eac3to
                    numQuality.Minimum = 0
                    numQuality.Maximum = 1
                    numQuality.Increment = 0.01
                    numQuality.DecimalPlaces = 2
                Case GuiAudioEncoder.fdkaac, GuiAudioEncoder.ffmpeg
                    numQuality.Minimum = 1
                    numQuality.Maximum = 5
                    numQuality.Increment = 1
                    numQuality.DecimalPlaces = 0
                Case Else
            End Select
        ElseIf TempProfile.Params.Codec = AudioCodec.MP3 Then
            numQuality.Minimum = 0
            numQuality.Maximum = 9
            numQuality.Increment = 1
            numQuality.DecimalPlaces = 0
        ElseIf TempProfile.Params.Codec = AudioCodec.Vorbis Then
            numQuality.Minimum = 0
            numQuality.Maximum = 10
            numQuality.Increment = 1
            numQuality.DecimalPlaces = 0
        Else
            numQuality.Minimum = 0
            numQuality.Maximum = Integer.MaxValue
            numQuality.Increment = 0.01
            numQuality.DecimalPlaces = 2
        End If

        numQuality.Value = value
    End Sub

    Sub mbSamplingRate_ValueChanged() Handles mbSamplingRate.ValueChangedUser
        TempProfile.Params.SamplingRate = mbSamplingRate.GetValue(Of Integer)()
        UpdateBitrate()
        If Not BlockUpdate Then UpdateControls()
    End Sub

    Sub mbLanguage_ValueChanged() Handles mbLanguage.ValueChangedUser
        TempProfile.Language = mbLanguage.GetValue(Of Language)()
        mbLanguage.Text = g.GetExtendedLanguageName(TempProfile.Language)
        If Not BlockUpdate Then UpdateControls()
    End Sub

    Sub tbName_TextChanged(sender As Object, e As EventArgs) Handles tbProfileName.TextChanged
        TempProfile.Name = tbProfileName.Text
        If Not BlockUpdate Then UpdateControls()
    End Sub

    Sub SaveProfile()
        Dim gap = ObjectHelp.GetCopy(TempProfile)
        Dim name = InputBox.Show("Enter the profile name", gap.Name)

        If name <> "" Then
            gap.Name = name
            gap.Clean()
            s.AudioProfiles.Add(gap)
            g.SaveAudioProfiles()
            MsgInfo("The profile was saved.")
        End If
    End Sub

    Sub LoadProfile(gap As GUIAudioProfile)
        Profile = gap
        TempProfile = ObjectHelp.GetCopy(gap)
        LoadProfile()
    End Sub

    Sub LoadProfile()
        BlockUpdate = True

        If TempProfile.Name <> TempProfile.DefaultName Then
            tbProfileName.Text = TempProfile.Name
        End If

        tbProfileName.SendMessageCue(TempProfile.Name, False)

        tbCustom.Text = TempProfile.Params.CustomSwitches
        tbStreamName.Text = TempProfile.StreamName

        cbDefaultTrack.Checked = TempProfile.Default
        cbForcedTrack.Checked = TempProfile.Forced
        cbCommentaryTrack.Checked = TempProfile.Commentary
        cbNormalize.Checked = TempProfile.Params.Normalize
        cbCenterOptimizedStereo.Checked = TempProfile.Params.CenterOptimizedStereo

        mbCodec.Value = TempProfile.Params.Codec
        mbChannels.Value = TempProfile.Params.ChannelsMode
        mbSamplingRate.Value = TempProfile.Params.SamplingRate
        mbEncoder.Value = TempProfile.Params.Encoder
        'NicAudio and DirectShow from Windows profiles are replaced with FFAudioSource
        If TempProfile.Decoder = AudioDecoderMode.NicAudio OrElse TempProfile.Decoder = AudioDecoderMode.DirectShow Then
            TempProfile.Decoder = AudioDecoderMode.FFAudioSource
        End If

        mbDecoder.Value = TempProfile.Decoder

        mbLanguage.Value = TempProfile.Language
        mbLanguage.Text = g.GetExtendedLanguageName(TempProfile.Language)

        SetQuality(TempProfile.Params.Quality)

        numBitrate.Value = TempProfile.Bitrate
        numDelay.Value = TempProfile.Delay
        numGain.Value = TempProfile.Gain

        BlockUpdate = False

        LoadAdvanced()
        UpdateControls()
    End Sub

    Sub SetBitrate(v As Integer)
        numBitrate.Value = v
    End Sub

    Sub LoadAdvanced()
        RemoveHandler SimpleUI.ValueChanged, AddressOf SimpleUIValueChanged

        Dim ui = SimpleUI
        ui.Store = TempProfile.Params
        ui.ClearPages()

        Dim page = ui.CreateFlowPage()

        Dim cb As SimpleUI.SimpleUICheckBox

        Select Case TempProfile.GetEncoder
            Case GuiAudioEncoder.deezy
                Dim mbDdpMode As SimpleUI.MenuBlock(Of DeezyDdpMode)
                Dim mbChannelsDdp As SimpleUI.MenuBlock(Of DeezyChannelsDdp)
                Dim mbChannelsDdpBluray As SimpleUI.MenuBlock(Of DeezyChannelsDdpBluray)
                Dim mbChannelsAtmos As SimpleUI.MenuBlock(Of DeezyChannelsAtmos)
                Dim mbStereodownmix As SimpleUI.MenuBlock(Of DeezyStereodownmix)
                Dim mbDrc As SimpleUI.MenuBlock(Of DeezyDrcLineMode)

                Select Case TempProfile.Params.Codec
                    Case AudioCodec.AC3
                        Dim mbChannelsDd = ui.AddMenu(Of DeezyChannelsDd)(page)
                        mbChannelsDd.Label.Text = "Channels:"
                        mbChannelsDd.Button.Expand = True
                        mbChannelsDd.Button.Value = TempProfile.Params.DeezyChannelsDd
                        mbChannelsDd.Button.SaveAction = Sub(value)
                                                             TempProfile.Params.DeezyChannelsDd = value
                                                             mbStereodownmix.Enabled = value = DeezyChannelsDd._2
                                                             'TempProfile.Params.ChannelsMode = If(TempProfile.Params.DeezyChannelsDd = DeezyChannelsDd._1, ChannelsMode._1, If(TempProfile.Params.DeezyChannelsDd = DeezyChannelsDd._2, ChannelsMode._2, If(TempProfile.Params.DeezyChannelsDd = DeezyChannelsDd._6, ChannelsMode._6, ChannelsMode.Original)))
                                                         End Sub
                    Case AudioCodec.AC4
                        Dim mbChannelsAc4 = ui.AddMenu(Of DeezyChannelsAc4)(page)
                        mbChannelsAc4.Label.Text = "Channels:"
                        mbChannelsAc4.Button.Expand = True
                        mbChannelsAc4.Button.Value = TempProfile.Params.DeezyChannelsAc4
                        mbChannelsAc4.Button.SaveAction = Sub(value)
                                                              TempProfile.Params.DeezyChannelsAc4 = value
                                                          End Sub
                    Case AudioCodec.EAC3
                        mbDdpMode = ui.AddMenu(Of DeezyDdpMode)(page)
                        mbDdpMode.Label.Text = "Mode:"
                        mbDdpMode.Button.Expand = True
                        mbDdpMode.Button.Value = TempProfile.Params.DeezyDdpMode
                        mbDdpMode.Button.SaveAction = Sub(value)
                                                          TempProfile.Params.DeezyDdpMode = value
                                                          mbChannelsDdp.Visible = value = DeezyDdpMode.Ddp
                                                          mbChannelsDdpBluray.Visible = value = DeezyDdpMode.DdpBluray
                                                          mbChannelsAtmos.Visible = value = DeezyDdpMode.Atmos
                                                          mbStereodownmix.Enabled = value = DeezyDdpMode.Ddp AndAlso TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._2
                                                          'TempProfile.Params.ChannelsMode = If(TempProfile.Params.Codec = AudioCodec.AC3 AndAlso TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._1, ChannelsMode._1, If(TempProfile.Params.Codec = AudioCodec.EAC3 AndAlso TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._2, ChannelsMode._2, If(TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._6, ChannelsMode._6, If(TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._8, ChannelsMode._8, ChannelsMode.Original))))
                                                      End Sub
                        mbChannelsDdp = ui.AddMenu(Of DeezyChannelsDdp)(page)
                        mbChannelsDdp.Label.Text = "Channels:"
                        mbChannelsDdp.Visible = TempProfile.Params.DeezyDdpMode = DeezyDdpMode.Ddp
                        mbChannelsDdp.Button.Expand = True
                        mbChannelsDdp.Button.Value = TempProfile.Params.DeezyChannelsDdp
                        mbChannelsDdp.Button.SaveAction = Sub(value)
                                                              TempProfile.Params.DeezyChannelsDdp = value
                                                              mbStereodownmix.Enabled = value = DeezyChannelsDdp._2
                                                              'TempProfile.Params.ChannelsMode = If(TempProfile.Params.Codec = AudioCodec.AC3 AndAlso TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._1, ChannelsMode._1, If(TempProfile.Params.Codec = AudioCodec.EAC3 AndAlso TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._2, ChannelsMode._2, If(TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._6, ChannelsMode._6, If(TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._8, ChannelsMode._8, ChannelsMode.Original))))
                                                          End Sub
                        mbChannelsDdpBluray = ui.AddMenu(Of DeezyChannelsDdpBluray)(page)
                        mbChannelsDdpBluray.Label.Text = "Channels:"
                        mbChannelsDdpBluray.Visible = TempProfile.Params.DeezyDdpMode = DeezyDdpMode.DdpBluray
                        mbChannelsDdpBluray.Button.Expand = True
                        mbChannelsDdpBluray.Button.Value = TempProfile.Params.DeezyChannelsDdpBluray
                        mbChannelsDdpBluray.Button.SaveAction = Sub(value)
                                                                    TempProfile.Params.DeezyChannelsDdpBluray = value
                                                                    mbStereodownmix.Enabled = TempProfile.Params.DeezyDdpMode = DeezyDdpMode.Ddp AndAlso TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._2
                                                                    'TempProfile.Params.ChannelsMode = If(TempProfile.Params.Codec = AudioCodec.AC3 AndAlso TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._1, ChannelsMode._1, If(TempProfile.Params.Codec = AudioCodec.EAC3 AndAlso TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._2, ChannelsMode._2, If(TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._6, ChannelsMode._6, If(TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._8, ChannelsMode._8, ChannelsMode.Original))))
                                                                End Sub
                        mbChannelsAtmos = ui.AddMenu(Of DeezyChannelsAtmos)(page)
                        mbChannelsAtmos.Label.Text = "Atmos Mode:"
                        mbChannelsAtmos.Visible = TempProfile.Params.DeezyDdpMode = DeezyDdpMode.Atmos
                        mbChannelsAtmos.Button.Expand = True
                        mbChannelsAtmos.Button.Value = TempProfile.Params.DeezyChannelsAtmos
                        mbChannelsAtmos.Button.SaveAction = Sub(value)
                                                                TempProfile.Params.DeezyChannelsAtmos = value
                                                                mbStereodownmix.Enabled = TempProfile.Params.DeezyDdpMode = DeezyDdpMode.Ddp AndAlso TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._2
                                                                'TempProfile.Params.ChannelsMode = If(TempProfile.Params.Codec = AudioCodec.AC3 AndAlso TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._1, ChannelsMode._1, If(TempProfile.Params.Codec = AudioCodec.EAC3 AndAlso TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._2, ChannelsMode._2, If(TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._6, ChannelsMode._6, If(TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._8, ChannelsMode._8, ChannelsMode.Original))))
                                                            End Sub
                    Case Else
                        Throw New NotImplementedException("LoadAdvanced")
                End Select

                If TempProfile.Params.Codec <> AudioCodec.AC4 Then
                    mbStereodownmix = ui.AddMenu(Of DeezyStereodownmix)(page)
                    mbStereodownmix.Enabled = (TempProfile.Params.Codec = AudioCodec.AC4 AndAlso TempProfile.Params.DeezyChannelsAc4 = DeezyChannelsAc4._2) OrElse (TempProfile.Params.Codec = AudioCodec.AC3 AndAlso TempProfile.Params.DeezyChannelsDd = DeezyChannelsDd._2) OrElse (TempProfile.Params.DeezyDdpMode = DeezyDdpMode.Ddp AndAlso TempProfile.Params.DeezyChannelsDdp = DeezyChannelsDdp._2)
                    mbStereodownmix.Label.Text = "Stereo Downmix:"
                    mbStereodownmix.Button.Expand = True
                    mbStereodownmix.Button.Value = TempProfile.Params.DeezyStereodownmix
                    mbStereodownmix.Button.SaveAction = Sub(value) TempProfile.Params.DeezyStereodownmix = value

                    mbDrc = ui.AddMenu(Of DeezyDrcLineMode)(page)
                    mbDrc.Label.Text = "Dynamic Range Comp.:"
                    mbDrc.Button.Expand = True
                    mbDrc.Button.Value = TempProfile.Params.DeezyDynamicrangecompression
                    mbDrc.Button.SaveAction = Sub(value) TempProfile.Params.DeezyDynamicrangecompression = value
                End If

                Dim bKeeptemp = ui.AddBool(page)
                bKeeptemp.Text = "Keep Temp Files"
                bKeeptemp.Checked = TempProfile.Params.DeezyKeeptemp
                bKeeptemp.SaveAction = Sub(value) TempProfile.Params.DeezyKeeptemp = value
            Case GuiAudioEncoder.eac3to
                Dim mbFrameRateMode = ui.AddMenu(Of AudioFrameRateMode)(page)
                mbFrameRateMode.Label.Text = "Frame Rate:"
                mbFrameRateMode.Button.Expand = True
                mbFrameRateMode.Button.Value = TempProfile.Params.FrameRateMode
                mbFrameRateMode.Button.SaveAction = Sub(value) TempProfile.Params.FrameRateMode = value

                Dim mbStereoDownmix = ui.AddMenu(Of Integer)(page)
                mbStereoDownmix.Label.Text = "Stereo Downmix:"
                mbStereoDownmix.Button.Expand = True
                mbStereoDownmix.Button.Add("Simple", 0)
                mbStereoDownmix.Button.Add("DPL II", 1)
                mbStereoDownmix.Button.Value = TempProfile.Params.eac3toStereoDownmixMode
                mbStereoDownmix.Button.SaveAction = Sub(value) TempProfile.Params.eac3toStereoDownmixMode = value

                cb = ui.AddBool(page)
                cb.Text = "Convert to 16-bit"
                cb.Checked = TempProfile.Params.eac3toDown16
                cb.SaveAction = Sub(value) TempProfile.Params.eac3toDown16 = value

                If (TempProfile.File = "" OrElse TempProfile.File.ToLowerInvariant.Contains("dts") OrElse
                    (TempProfile.Stream IsNot Nothing AndAlso TempProfile.Stream.Name.Contains("DTS"))) AndAlso
                    TempProfile.Params.Codec = AudioCodec.DTS Then

                    cb = ui.AddBool(page)
                    cb.Text = "Extract DTS core"
                    cb.Checked = TempProfile.ExtractDTSCore
                    cb.SaveAction = Sub(value)
                                        TempProfile.ExtractDTSCore = value
                                        UpdateControls()
                                    End Sub
                End If
            Case GuiAudioEncoder.ffmpeg
                Select Case TempProfile.Params.Codec
                    Case AudioCodec.AAC, AudioCodec.AC3, AudioCodec.EAC3, AudioCodec.DTS
                        If TempProfile.Params.Codec = AudioCodec.EAC3 Then
                        Dim mbSpf = ui.AddMenu(Of SamplesPerFrame)
                            mbSpf.Text = "Samples Per Frame:"
                            mbSpf.Expanded = True
                            mbSpf.Button.Value = TempProfile.Params.ffmpegSamplesPerFrame
                            mbSpf.Button.SaveAction = Sub(value) TempProfile.Params.ffmpegSamplesPerFrame = value
                        End If

                        Dim mbRateMode = ui.AddMenu(Of SimpleAudioRateMode)
                        mbRateMode.Text = "Rate Mode:"
                        mbRateMode.Expanded = True
                        mbRateMode.Button.Value = TempProfile.Params.SimpleRateMode
                        mbRateMode.Button.SaveAction = Sub(value) TempProfile.Params.SimpleRateMode = value

                        cb = ui.AddBool
                        cb.Text = "Use fdk-aac"
                        cb.Property = NameOf(TempProfile.Params.ffmpegLibFdkAAC)
                    Case AudioCodec.FLAC
                        Dim mbBitDepth = ui.AddMenu(Of FlacBitDepth)
                        mbBitDepth.Text = "Bit Depth:"
                        mbBitDepth.Expanded = True
                        mbBitDepth.Button.Value = TempProfile.Params.ffmpegFlacBitDepth
                        mbBitDepth.Button.SaveAction = Sub(value)
                                                           TempProfile.Params.ffmpegFlacBitDepth = value
                                                           UpdateBitrate()
                                                           UpdateControls()
                                                       End Sub
                    Case AudioCodec.Opus
                        Dim mbRateMode = ui.AddMenu(Of OpusRateMode)
                        mbRateMode.Text = "Rate Mode:"
                        mbRateMode.Expanded = True
                        mbRateMode.Button.Value = TempProfile.Params.ffmpegOpusRateMode
                        mbRateMode.Button.SaveAction = Sub(value) TempProfile.Params.ffmpegOpusRateMode = value

                        Dim mbOpusApp = ui.AddMenu(Of OpusApp)
                        mbOpusApp.Text = "Application Type:"
                        mbOpusApp.Expanded = True
                        mbOpusApp.Button.Value = TempProfile.Params.ffmpegOpusApp
                        mbOpusApp.Button.SaveAction = Sub(value) TempProfile.Params.ffmpegOpusApp = value

                        Dim frame = ui.AddMenu(Of Double)
                        frame.Text = "Frame Duration:"
                        frame.Expanded = True
                        frame.Add("2.5", 2.5)
                        frame.Add("5", 5)
                        frame.Add("10", 10)
                        frame.Add("20", 20)
                        frame.Add("40", 40)
                        frame.Add("60", 60)
                        frame.Add("80", 80)
                        frame.Add("100", 100)
                        frame.Add("120", 120)
                        frame.Property = NameOf(TempProfile.Params.ffmpegOpusFrame)

                        Dim map = ui.AddMenu(Of Integer)
                        map.Text = "Mapping Family:"
                        map.Expanded = True
                        map.Add("-1", -1)
                        map.Add("0", 0)
                        map.Add("1", 1)
                        map.Add("255", 255)
                        map.Property = NameOf(TempProfile.Params.ffmpegOpusMap)

                        Dim comp = ui.AddNum(page)
                        comp.Text = "Compression Level:"
                        comp.Config = {0, 10, 1}
                        comp.NumEdit.Value = TempProfile.Params.ffmpegOpusCompress
                        comp.NumEdit.SaveAction = Sub(value) TempProfile.Params.ffmpegOpusCompress = CInt(value)

                        Dim packet = ui.AddNum(page)
                        packet.Text = "Packet Loss:"
                        packet.Config = {0, 100, 1}
                        packet.NumEdit.Value = TempProfile.Params.ffmpegOpusPacket
                        packet.NumEdit.SaveAction = Sub(value) TempProfile.Params.ffmpegOpusPacket = CInt(value)
                    Case AudioCodec.W64, AudioCodec.WAV
                        Dim mDepth = ui.AddMenu(Of WaveBitDepth)
                        mDepth.Text = "Bit Depth:"
                        mDepth.Expanded = True
                        mDepth.Button.Value = TempProfile.Params.ffmpegWaveBitDepth
                        mDepth.Button.SaveAction = Sub(val)
                                                       TempProfile.Params.ffmpegWaveBitDepth = val
                                                       UpdateBitrate()
                                                       UpdateControls()
                                                   End Sub
                    Case Else
                        If Not {AudioCodec.WAV, AudioCodec.W64, AudioCodec.FLAC}.Contains(TempProfile.Params.Codec) Then
                            Dim mbRateMode = ui.AddMenu(Of AudioRateMode)
                            mbRateMode.Text = "Rate Mode:"
                            mbRateMode.Expanded = True
                            mbRateMode.Button.Value = TempProfile.Params.RateMode
                            mbRateMode.Button.SaveAction = Sub(value) TempProfile.Params.RateMode = value
                        End If
                End Select

                If (TempProfile.File = "" OrElse TempProfile.File.ToLowerInvariant.Contains("dts") OrElse
                    (TempProfile.Stream IsNot Nothing AndAlso TempProfile.Stream.Name.Contains("DTS"))) AndAlso
                    TempProfile.Params.Codec = AudioCodec.DTS Then

                    cb = ui.AddBool(page)
                    cb.Text = "Extract DTS core"
                    cb.Checked = TempProfile.ExtractDTSCore
                    cb.SaveAction = Sub(value)
                                        TempProfile.ExtractDTSCore = value
                                        UpdateControls()
                                    End Sub
                End If
            Case GuiAudioEncoder.fdkaac
                Dim getHelpAction = Function(switch As String) Sub() CommandLineParams.ShowConsoleHelp(Package.fdkaac, {switch})

                Dim modeMenu = ui.AddMenu(Of SimpleAudioRateMode)
                modeMenu.Text = "Rate Mode:"
                modeMenu.Expanded = True
                modeMenu.HelpAction = getHelpAction("--bitrate-mode")
                modeMenu.Button.Value = TempProfile.Params.SimpleRateMode
                modeMenu.Button.SaveAction = Sub(value)
                                                 TempProfile.Params.SimpleRateMode = value
                                                 UpdateBitrate()
                                             End Sub

                Dim profileMenu = ui.AddMenu(Of Integer)
                profileMenu.Text = "Profile:"
                profileMenu.Expanded = True
                profileMenu.HelpAction = getHelpAction("--profile")
                profileMenu.Add("AAC LC", 2)
                profileMenu.Add("HE-AAC SBR", 5)
                profileMenu.Add("HE-AAC SBR+PS", 29)
                profileMenu.Add("AAC LD", 23)
                profileMenu.Add("AAC ELD", 39)
                profileMenu.Property = NameOf(TempProfile.Params.fdkaacProfile)

                Dim lowDelaySBR = ui.AddMenu(Of Integer)
                lowDelaySBR.Text = "Lowdelay SBR:"
                lowDelaySBR.Expanded = True
                lowDelaySBR.HelpAction = getHelpAction("--lowdelay-sbr")
                lowDelaySBR.Add("ELD SBR auto configuration", -1)
                lowDelaySBR.Add("Disable SBR on ELD", 0)
                lowDelaySBR.Add("Enable SBR on ELD", 1)
                lowDelaySBR.Property = NameOf(TempProfile.Params.fdkaacLowDelaySBR)

                Dim sbrRatio = ui.AddMenu(Of Integer)
                sbrRatio.Text = "SBR Ratio:"
                sbrRatio.Expanded = True
                sbrRatio.HelpAction = getHelpAction("--sbr-ratio")
                sbrRatio.Add("Library Default", 0)
                sbrRatio.Add("Downsampled SBR (ELD+SBR default)", 1)
                sbrRatio.Add("Dual-rate SBR (HE-AAC default)", 2)
                sbrRatio.Property = NameOf(TempProfile.Params.fdkaacSbrRatio)

                Dim gaplessMode = ui.AddMenu(Of Integer)
                gaplessMode.Text = "Gapless Mode:"
                gaplessMode.Expanded = True
                gaplessMode.HelpAction = getHelpAction("--gapless-mode")
                gaplessMode.Add("iTunSMPB", 0)
                gaplessMode.Add("ISO Standard (EDTS And SGPD)", 1)
                gaplessMode.Add("Both", 2)
                gaplessMode.Property = NameOf(TempProfile.Params.fdkaacGaplessMode)

                Dim transportFormat = ui.AddMenu(Of Integer)
                transportFormat.Text = "Transport Format:"
                transportFormat.Expanded = True
                transportFormat.HelpAction = getHelpAction("--transport-format")
                transportFormat.Add("M4A", 0)
                transportFormat.Add("ADIF", 1)
                transportFormat.Add("ADTS", 2)
                transportFormat.Add("LATM MCP=1", 6)
                transportFormat.Add("LATM MCP=0", 7)
                transportFormat.Add("LOAS/LATM (LATM within LOAS)", 10)
                transportFormat.Property = NameOf(TempProfile.Params.fdkaacTransportFormat)

                Dim n = ui.AddNum
                n.Text = "Bandwidth:"
                n.HelpAction = getHelpAction("--bandwidth")
                n.Property = NameOf(TempProfile.Params.fdkaacBandwidth)

                cb = ui.AddBool
                cb.Text = "Afterburner"
                cb.HelpAction = getHelpAction("--afterburner")
                cb.Property = NameOf(TempProfile.Params.fdkaacAfterburner)

                cb = ui.AddBool
                cb.Text = "Add CRC Check on ADTS header"
                cb.HelpAction = getHelpAction("--adts-crc-check")
                cb.Property = NameOf(TempProfile.Params.fdkaacAdtsCrcCheck)

                cb = ui.AddBool
                cb.Text = "Header Period"
                cb.HelpAction = getHelpAction("--header-period")
                cb.Property = NameOf(TempProfile.Params.fdkaacHeaderPeriod)

                cb = ui.AddBool
                cb.Text = "Include SBR Delay"
                cb.HelpAction = getHelpAction("--include-sbr-delay")
                cb.Property = NameOf(TempProfile.Params.fdkaacIncludeSbrDelay)

                cb = ui.AddBool
                cb.Text = "Place moov box before mdat box"
                cb.HelpAction = getHelpAction("--moov-before-mdat")
                cb.Property = NameOf(TempProfile.Params.fdkaacMoovBeforeMdat)
            Case GuiAudioEncoder.qaac
                Dim mbMode = ui.AddMenu(Of Integer)
                mbMode.Text = "Mode:"
                mbMode.Expanded = True
                mbMode.Add("True VBR", 0)
                mbMode.Add("Constrained VBR", 1)
                mbMode.Add("ABR", 2)
                mbMode.Add("CBR", 3)
                mbMode.Button.Value = TempProfile.Params.qaacRateMode
                mbMode.Button.SaveAction = Sub(value)
                                               TempProfile.Params.qaacRateMode = value
                                               TempProfile.Params.RateMode = If(TempProfile.Params.qaacRateMode = 0, AudioRateMode.VBR, AudioRateMode.CBR)
                                               UpdateBitrate()
                                           End Sub

                Dim mbQuality = ui.AddMenu(Of Integer)(page)
                mbQuality.Label.Text = "Quality:"
                mbQuality.Button.Expand = True
                mbQuality.Button.Add("Low", 0)
                mbQuality.Button.Add("Medium", 1)
                mbQuality.Button.Add("High", 2)
                mbQuality.Button.Value = TempProfile.Params.qaacQuality
                mbQuality.Button.SaveAction = Sub(value) TempProfile.Params.qaacQuality = value

                Dim num = ui.AddNum(page)
                num.Text = "Lowpass:"
                num.Config = {0, Integer.MaxValue}
                num.NumEdit.Value = TempProfile.Params.qaacLowpass
                num.NumEdit.SaveAction = Sub(value) TempProfile.Params.qaacLowpass = CInt(value)

                Dim he = ui.AddBool(page)
                he.Text = "High Efficiency"
                he.Checked = TempProfile.Params.qaacHE
                he.SaveAction = Sub(value) TempProfile.Params.qaacHE = value

                AddHandler he.CheckedChanged, Sub() If he.Checked AndAlso mbMode.Button.Value = 0 Then mbMode.Button.Value = 1
                AddHandler mbMode.Button.ValueChangedUser, Sub() If mbMode.Button.Value = 0 Then he.Checked = False

                cb = ui.AddBool(page)
                cb.Text = "No delay"
                cb.Checked = TempProfile.Params.qaacNoDelay
                cb.SaveAction = Sub(value) TempProfile.Params.qaacNoDelay = value

                cb = ui.AddBool(page)
                cb.Text = "No dither when quantizing to lower bit depth"
                cb.Checked = TempProfile.Params.qaacNoDither
                cb.SaveAction = Sub(value) TempProfile.Params.qaacNoDither = value
            Case GuiAudioEncoder.opusenc
                Dim mbMode = ui.AddMenu(Of OpusRateMode)
                mbMode.Text = "Mode:"
                mbMode.Expanded = True
                mbMode.Button.Value = TempProfile.Params.OpusencOpusRateMode
                mbMode.Button.SaveAction = Sub(value)
                                               TempProfile.Params.OpusencOpusRateMode = value
                                               TempProfile.Params.RateMode = If(TempProfile.Params.OpusencOpusRateMode = (OpusRateMode.VBR Or OpusRateMode.CVBR), AudioRateMode.VBR, AudioRateMode.CBR)
                                               UpdateBitrate()
                                           End Sub

                Dim mbTune = ui.AddMenu(Of OpusTune)(page)
                mbTune.Label.Text = "Tune:"
                mbTune.Button.Expand = True
                mbTune.Button.Value = TempProfile.Params.OpusencTune
                mbTune.Button.SaveAction = Sub(value) TempProfile.Params.OpusencTune = value

                Dim mbDownmix = ui.AddMenu(Of OpusDownmix)(page)
                mbDownmix.Label.Text = "Downmix:"
                mbDownmix.Button.Expand = True
                mbDownmix.Button.Value = TempProfile.Params.OpusencDownmix
                mbDownmix.Button.SaveAction = Sub(value)
                                                  TempProfile.Params.OpusencDownmix = value
                                                  'TempProfile.Params.ChannelsMode = If(TempProfile.Params.OpusencDownmix = OpusDownmix.Mono, ChannelsMode._1, If(TempProfile.Params.OpusencDownmix = OpusDownmix.Stereo, ChannelsMode._2, ChannelsMode.Original))
                                              End Sub

                Dim mbFramesize = ui.AddMenu(Of OpusFramesize)(page)
                mbFramesize.Label.Text = "Framesize in ms:"
                mbFramesize.Button.Expand = True
                mbFramesize.Button.Value = TempProfile.Params.OpusencFramesize
                mbFramesize.Button.SaveAction = Sub(value) TempProfile.Params.OpusencFramesize = value

                Dim numExpectloss = ui.AddNum(page)
                numExpectloss.Text = "Expect Packet Loss in %:"
                numExpectloss.Config = {0, 100, 1, 0}
                numExpectloss.NumEdit.Value = TempProfile.Params.OpusencExpectloss
                numExpectloss.NumEdit.SaveAction = Sub(value) TempProfile.Params.OpusencExpectloss = CInt(value)

                Dim numComplexity = ui.AddNum(page)
                numComplexity.Text = "Complexity (10: best):"
                numComplexity.Config = {0, 10, 1, 0}
                numComplexity.NumEdit.Value = TempProfile.Params.OpusencComp
                numComplexity.NumEdit.SaveAction = Sub(value) TempProfile.Params.OpusencComp = CInt(value)

                Dim numMaxdelay = ui.AddNum(page)
                numMaxdelay.Text = "Max Delay in ms:"
                numMaxdelay.Config = {0, 1000, 1, 0}
                numMaxdelay.NumEdit.Value = TempProfile.Params.OpusencMaxdelay
                numMaxdelay.NumEdit.SaveAction = Sub(value) TempProfile.Params.OpusencMaxdelay = CInt(value)

                Dim bPhaseInv = ui.AddBool(page)
                bPhaseInv.Text = "Phase Inversion"
                bPhaseInv.Checked = TempProfile.Params.OpusencPhaseinversion
                bPhaseInv.SaveAction = Sub(value) TempProfile.Params.OpusencPhaseinversion = value
        End Select

        ui.UpdateTree()
        AddHandler SimpleUI.ValueChanged, AddressOf SimpleUIValueChanged
    End Sub

    Sub Execute()
        If TempProfile.File <> "" Then
            If Not TempProfile.IsInputSupported AndAlso Not TempProfile.DecodingMode = AudioDecodingMode.Pipe Then
                MsgWarn("The input format isn't supported by the current encoder," + BR + "convert to WAV or FLAC first or enable piping in the options.")
            Else
                DesktopHelp.RunInTerminal(TempProfile.GetCommandLine(True))
            End If
        Else
            MsgWarn("Source file is missing!")
        End If
    End Sub
    Sub tbStreamName_TextChanged(sender As Object, e As EventArgs) Handles tbStreamName.TextChanged
        TempProfile.StreamName = tbStreamName.Text
    End Sub

    Sub tbCustom_TextChanged(sender As Object, e As EventArgs) Handles tbCustom.TextChanged
        TempProfile.Params.CustomSwitches = tbCustom.Text
        If Not BlockUpdate Then UpdateControls()
    End Sub

    Sub cbDefaultTrack_CheckedChanged(sender As Object, e As EventArgs) Handles cbDefaultTrack.CheckedChanged
        TempProfile.Default = cbDefaultTrack.Checked
    End Sub

    Sub cbForcedTrack_CheckedChanged(sender As Object, e As EventArgs) Handles cbForcedTrack.CheckedChanged
        TempProfile.Forced = cbForcedTrack.Checked
    End Sub

    Sub cbCommentaryTrack_CheckedChanged(sender As Object, e As EventArgs) Handles cbCommentaryTrack.CheckedChanged
        TempProfile.Commentary = cbCommentaryTrack.Checked
    End Sub

    Sub mbEncoder_ValueChangedUser(value As Object) Handles mbEncoder.ValueChangedUser
        Dim enc = mbEncoder.GetValue(Of GuiAudioEncoder)()
        TempProfile.Params.Encoder = enc

        If enc = GuiAudioEncoder.deezy Then
            cbNormalize.Checked = False
            cbCenterOptimizedStereo.Checked = False
        End If
        mbCodec_ValueChangedUser()
    End Sub

    Sub ShowHelp()
        Dim form As New HelpForm()
        form.Doc.WriteStart(Text)
        form.Doc.WriteTips(TipProvider.GetTips, SimpleUI.ActivePage.TipProvider.GetTips)
        form.Show()
    End Sub

    Sub mbChannels_ValueChanged(value As Object) Handles mbChannels.ValueChangedUser
        TempProfile.Params.ChannelsMode = mbChannels.GetValue(Of ChannelsMode)()
        UpdateBitrate()
        If Not BlockUpdate Then UpdateControls()
    End Sub

    Sub mbConvertMode_ValueChangedUser(value As Object) Handles mbDecoder.ValueChangedUser
        TempProfile.Decoder = mbDecoder.GetValue(Of AudioDecoderMode)()
        If Not BlockUpdate Then UpdateControls()
    End Sub

    Sub bnAdvanced_Click(sender As Object, e As EventArgs) Handles bnAdvanced.Click
        Using form As New SimpleSettingsForm("Advanced Audio Options")
            form.ScaleClientSize(30, 21)
            Dim ui = form.SimpleUI
            ui.Store = TempProfile.Params

            ui.CreateFlowPage("General", True)

            Dim convFormat = ui.AddMenu(Of AudioDecodingMode)
            convFormat.Text = "Decoding Method:"
            convFormat.Button.Value = TempProfile.DecodingMode
            convFormat.Button.SaveAction = Sub(val) TempProfile.DecodingMode = val

            ui.CreateFlowPage("ffmpeg", True)

            Dim ffmpegNormalize = ui.AddMenu(Of ffmpegNormalizeMode)
            ffmpegNormalize.Text = "Normalize Method:"
            ffmpegNormalize.Property = NameOf(TempProfile.Params.ffmpegNormalizeMode)

            Dim n = ui.AddNum()
            n.Text = "Probe Size"
            n.Config = {1, 999, 5}
            n.Property = NameOf(TempProfile.Params.ProbeSize)

            n = ui.AddNum()
            n.Text = "Analyze Duration"
            n.Config = {1, 999, 5}
            n.Property = NameOf(TempProfile.Params.AnalyzeDuration)

            ui.CreateFlowPage("ffmpeg | loudnorm", True)

            ui.AddLabel("EBU R128 Loudness Normalization")

            Dim helpUrl = "https://www.ffmpeg.org/ffmpeg-filters.html#loudnorm"

            n = ui.AddNum()
            n.Text = "Integrated"
            n.Help = helpUrl
            n.Config = {0, 0, 0.1, 1}
            n.Property = NameOf(TempProfile.Params.ffmpegLoudnormIntegrated)

            n = ui.AddNum()
            n.Text = "True Peak"
            n.Help = helpUrl
            n.Config = {0, 0, 0.1, 1}
            n.Property = NameOf(TempProfile.Params.ffmpegLoudnormTruePeak)

            n = ui.AddNum()
            n.Text = "LRA"
            n.Help = helpUrl
            n.Config = {0, 0, 0.1, 1}
            n.Property = NameOf(TempProfile.Params.ffmpegLoudnormLRA)

            ui.CreateFlowPage("ffmpeg | dynaudnorm", True)

            ui.AddLabel("Dynamic Audio Normalizer")

            helpUrl = "https://www.ffmpeg.org/ffmpeg-filters.html#dynaudnorm"

            n = ui.AddNum()
            n.Text = "Frame Length"
            n.Help = helpUrl
            n.Config = {10, 8000}
            n.Property = NameOf(TempProfile.Params.ffmpegDynaudnormF)

            n = ui.AddNum()
            n.Text = "Gauss filter window size"
            n.Help = helpUrl
            n.Config = {3, 301, 2, 0}
            n.Property = NameOf(TempProfile.Params.ffmpegDynaudnormG)

            n = ui.AddNum()
            n.Text = "Target Peak"
            n.Help = helpUrl
            n.Config = {0, 0, 0.05, 2}
            n.Property = NameOf(TempProfile.Params.ffmpegDynaudnormP)

            n = ui.AddNum()
            n.Text = "Max gain factor"
            n.Help = helpUrl
            n.Config = {1, 100, 1, 1}
            n.Property = NameOf(TempProfile.Params.ffmpegDynaudnormM)

            n = ui.AddNum()
            n.Text = "Target RMS"
            n.Help = helpUrl
            n.Config = {0, 1, 0.1, 1}
            n.Property = NameOf(TempProfile.Params.ffmpegDynaudnormR)

            n = ui.AddNum()
            n.Text = "Compress factor"
            n.Help = helpUrl
            n.Config = {0, 30, 1, 1}
            n.Property = NameOf(TempProfile.Params.ffmpegDynaudnormS)

            Dim b = ui.AddBool
            b.Text = "Enable channels coupling"
            b.Help = helpUrl
            b.Property = NameOf(TempProfile.Params.ffmpegDynaudnormN)

            b = ui.AddBool
            b.Text = "Enable DC bias correction"
            b.Help = helpUrl
            b.Property = NameOf(TempProfile.Params.ffmpegDynaudnormC)

            b = ui.AddBool
            b.Text = "Enable alternative boundary mode"
            b.Help = helpUrl
            b.Property = NameOf(TempProfile.Params.ffmpegDynaudnormB)

            ui.SelectLast("last advanced audio options page")

            If form.ShowDialog() = DialogResult.OK Then
                ui.Save()
            End If

            UpdateControls()
            ui.SaveLast("last advanced audio options page")
        End Using
    End Sub

    Sub cbNormalize_CheckedChanged(sender As Object, e As EventArgs) Handles cbNormalize.CheckedChanged
        TempProfile.Params.Normalize = cbNormalize.Checked
    End Sub

    Sub cbCenterOptimizedStereo_CheckedChanged(sender As Object, e As EventArgs) Handles cbCenterOptimizedStereo.CheckedChanged
        TempProfile.Params.CenterOptimizedStereo = cbCenterOptimizedStereo.Checked
    End Sub

    Sub AudioForm_HelpRequested(sender As Object, e As EventArgs) Handles Me.HelpRequested
        ShowHelp()
    End Sub
End Class
