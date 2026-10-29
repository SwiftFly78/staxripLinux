
Imports StaxRip.UI

Public Class CommandLineAudioEncoderForm
    Inherits SizeSavingDialogBase

    Public WithEvents EditControl As MacroEditorControl
    Public WithEvents tbStreamName As TextEdit
    Public WithEvents tbChannels As TextEdit
    Public WithEvents bnCancel As ButtonEx
    Public WithEvents bnOK As ButtonEx
    Public WithEvents tbType As TextEdit
    Public WithEvents mbLanguage As MenuButton
    Public WithEvents tbProfileName As TextEdit
    Public WithEvents bnMenu As ButtonEx
    Public WithEvents cms As ContextMenuStripEx
    Public WithEvents cbDefault As CheckBoxEx
    Public WithEvents cbForced As CheckBoxEx
    Public WithEvents cbCommentary As CheckBoxEx
    Public WithEvents tbInput As TextEdit
    Public WithEvents tbBitrate As TextEdit
    Public WithEvents tbDelay As TextEdit
    Public lStreamName, lInput, lChannels, laProfileName, lType, lBitrate, lLanguage, lDelay As LabelEx
    Public TipProvider As TipProvider
    Public ValidationProvider As ValidationProvider

    Private Sub InitializeComponent()
        tbInput = New TextEdit
        tbType = New TextEdit
        mbLanguage = New MenuButton With {.ShowMenuSymbol = True}
        tbBitrate = New TextEdit
        tbChannels = New TextEdit
        tbDelay = New TextEdit
        tbStreamName = New TextEdit
        tbProfileName = New TextEdit
        EditControl = New MacroEditorControl With {.Text = "Command Lines"}
        cbDefault = New CheckBoxEx With {.Text = "Default"}
        cbForced = New CheckBoxEx With {.Text = "Forced"}
        cbCommentary = New CheckBoxEx With {.Text = "Commentary"}
        cms = New ContextMenuStripEx
        bnMenu = New ButtonEx With {.ShowMenuSymbol = True, .ContextMenuStrip = cms}
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        lInput = New LabelEx With {.Text = "Supported Input File Types:"}
        lType = New LabelEx With {.Text = "Output File Type:"}
        lLanguage = New LabelEx With {.Text = "Language:"}
        lBitrate = New LabelEx With {.Text = "Bitrate:"}
        lChannels = New LabelEx With {.Text = "Channels:"}
        lDelay = New LabelEx With {.Text = "Delay:"}
        lStreamName = New LabelEx With {.Text = "Track Name:"}
        laProfileName = New LabelEx With {.Text = "Name:"}
        TipProvider = New TipProvider
        ValidationProvider = New ValidationProvider
        ValidationProvider.SetPattern(tbBitrate, "^[1-9]+\d*$")
        ValidationProvider.SetPattern(tbDelay, "^(-?[1-9]+\d*|-?0)$")
        ValidationProvider.SetPattern(tbChannels, "^[1-9]{1}$")

        Dim smallValues As New TableLayout(
            New TableRow(Scaled(lBitrate), Scaled(lChannels), Scaled(lDelay))) With {.Spacing = DefaultSpacing}
        Dim smallEdits As New TableLayout(
            New TableRow(Scaled(tbBitrate), Scaled(tbChannels), Scaled(tbDelay))) With {.Spacing = DefaultSpacing}

        Dim top As New TableLayout(
            New TableRow(Scaled(lInput), Scaled(lType), Scaled(lLanguage)),
            New TableRow(Scaled(tbInput), Scaled(tbType), Scaled(mbLanguage)),
            New TableRow(Scaled(smallValues), Scaled(lStreamName), Scaled(laProfileName)),
            New TableRow(Scaled(smallEdits), Scaled(tbStreamName), Scaled(tbProfileName))) With {.Spacing = DefaultSpacing}

        Content = DialogContent(SplitRow({cbDefault, cbForced, cbCommentary}, {bnMenu, bnOK, bnCancel}), Row(top), ScaledRow(EditControl))

        AbortButton = bnCancel
        Text = $"Audio Command Lines - {g.DefaultCommands.GetApplicationDetails()}"
    End Sub

    Private Profile, TempProfile As BatchAudioProfile

    Sub New(profile As BatchAudioProfile)
        InitializeComponent()
        RestoreClientSize(50, 30)

        Me.Profile = profile
        TempProfile = ObjectHelp.GetCopy(Of BatchAudioProfile)(profile)
        tbType.Text = TempProfile.OutputFileType
        tbInput.Text = TempProfile.SupportedInput.Join(" ")

        If TempProfile.Name <> TempProfile.DefaultName Then
            tbProfileName.Text = TempProfile.Name
        End If

        tbProfileName.SendMessageCue(TempProfile.Name, False)

        EditControl.SetCommandLineDefaults()
        EditControl.Value = TempProfile.CommandLines
        EditControl.SpecialMacrosFunction = AddressOf TempProfile.ExpandMacros

        AddHandler EditControl.rtbEdit.TextChanged, AddressOf tbEditTextChanged

        tbStreamName.Text = TempProfile.StreamName
        tbBitrate.Text = TempProfile.Bitrate.ToString
        tbDelay.Text = TempProfile.Delay.ToString
        tbChannels.Text = TempProfile.Channels.ToString
        cbDefault.Checked = TempProfile.Default
        cbForced.Checked = TempProfile.Forced
        cbCommentary.Checked = TempProfile.Commentary

        TipProvider.SetTip("Audio channels count. Macro: %channels%", tbChannels, lChannels)
        TipProvider.SetTip("Display language of the track. Macros: %language_native%, %language_english%", mbLanguage, lLanguage)
        TipProvider.SetTip("The targeted bitrate of the output file. Macro: %bitrate%", tbBitrate, lBitrate)
        TipProvider.SetTip("Audio delay to be fixed. Macro: %delay%", tbDelay, lDelay)
        TipProvider.SetTip("File types accepted as input, leave empty to support any file type.", tbInput, lInput)
        TipProvider.SetTip("Track name used by the muxer. The track name may contain macros.", tbStreamName, lStreamName)
        TipProvider.SetTip("If no name is defined StaxRipLinux auto generate the name.", laProfileName, tbProfileName)
        TipProvider.SetTip("Default MKV Track.", cbDefault)
        TipProvider.SetTip("Forced MKV Track.", cbForced)

        cms.Add("Copy Command Line", Sub() Clipboard.SetText(TempProfile.GetCode))
        cms.Add("Show Command Line...", Sub() g.ShowCommandLinePreview("Command Lines", TempProfile.GetCode, False))
        cms.Add("Save Profile...", AddressOf SaveProfile, "Saves the current settings as profile")
        cms.Add("Help", AddressOf ShowHelp)
    End Sub

    Sub SaveProfile()
        Dim gap = ObjectHelp.GetCopy(TempProfile)
        Dim name = InputBox.Show("Enter the profile name", gap.Name)

        If name <> "" Then
            gap.Name = name
            s.AudioProfiles.Add(gap)
            g.SaveAudioProfiles()
            MsgInfo("The profile was saved.")
        End If
    End Sub

    Sub CommandLineAudioSettingsForm_FormClosed() Handles Me.Closed
        If DialogResult = DialogResult.OK Then
            Profile.SupportedInput = TempProfile.SupportedInput
            Profile.OutputFileType = TempProfile.OutputFileType
            Profile.CommandLines = TempProfile.CommandLines
            Profile.Bitrate = TempProfile.Bitrate
            Profile.StreamName = TempProfile.StreamName
            Profile.Channels = TempProfile.Channels
            Profile.Name = TempProfile.Name
            Profile.Language = TempProfile.Language
            Profile.Delay = TempProfile.Delay
            Profile.Default = TempProfile.Default
            Profile.Forced = TempProfile.Forced
            Profile.Commentary = TempProfile.Commentary
        End If
    End Sub

    Sub tbStreamName_TextChanged() Handles tbStreamName.TextChanged
        TempProfile.StreamName = tbStreamName.Text
        EditControl.UpdatePreview()
    End Sub

    Sub tbInput_TextChanged() Handles tbInput.TextChanged
        TempProfile.SupportedInput = tbInput.Text.ToLowerInvariant.SplitNoEmptyAndWhiteSpace(",", ";", " ")
        EditControl.UpdatePreview()
    End Sub

    Sub tbType_TextChanged() Handles tbType.TextChanged
        TempProfile.OutputFileType = tbType.Text
        EditControl.UpdatePreview()
    End Sub

    Sub tbBitrate_TextChanged() Handles tbBitrate.TextChanged
        TempProfile.Bitrate = tbBitrate.Text.ToInt
        EditControl.UpdatePreview()
    End Sub

    Sub tbChannels_TextChanged() Handles tbChannels.TextChanged
        TempProfile.Channels = tbChannels.Text.ToInt
        EditControl.UpdatePreview()
    End Sub

    Sub mbLanguage_ValueChangedUser(value As Object) Handles mbLanguage.ValueChangedUser
        TempProfile.Language = DirectCast(mbLanguage.Value, Language)
        EditControl.UpdatePreview()
    End Sub

    Sub tbEditTextChanged(sender As Object, args As EventArgs)
        TempProfile.CommandLines = EditControl.rtbEdit.Text
        EditControl.UpdatePreview()
    End Sub

    Sub tbDelay_TextChanged() Handles tbDelay.TextChanged
        TempProfile.Delay = tbDelay.Text.ToInt
        EditControl.UpdatePreview()
    End Sub

    Sub tbProfileName_TextChanged(sender As Object, e As EventArgs) Handles tbProfileName.TextChanged
        TempProfile.Name = tbProfileName.Text
    End Sub

    Sub ShowHelp()
        Dim form As New HelpForm()

        form.Doc.WriteStart(Text)
        form.Doc.WriteParagraph("Each line is executed separately. Global macros are passed to the process as environment variables.")
        form.Doc.WriteH2("Options")
        form.Doc.WriteTips(TipProvider.GetTips, EditControl.TipProvider.GetTips)

        Dim macroList = Macro.GetTips(False, False, False, False, True)

        form.Doc.WriteTable("Command Line Audio Macros",
                            "The following macros are available in the command line audio dialog and override global macros with the same name.",
                            macroList)

        form.Doc.WriteTable("Global Macros", "Global macros are passed to the process as environment variables.", Macro.GetTips(True, False, True, True, False))
        form.Show()
    End Sub

    Sub CommandLineAudioEncoderForm_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        g.PopulateLanguages(mbLanguage)

        mbLanguage.Value = TempProfile.Language
    End Sub

    Sub cbDefault_CheckedChanged(sender As Object, e As EventArgs) Handles cbDefault.CheckedChanged
        TempProfile.Default = cbDefault.Checked
        EditControl.UpdatePreview()
    End Sub

    Sub cbForced_CheckedChanged(sender As Object, e As EventArgs) Handles cbForced.CheckedChanged
        TempProfile.Forced = cbForced.Checked
        EditControl.UpdatePreview()
    End Sub

    Sub cbCommentary_CheckedChanged(sender As Object, e As EventArgs) Handles cbCommentary.CheckedChanged
        TempProfile.Commentary = cbCommentary.Checked
        EditControl.UpdatePreview()
    End Sub

    Sub CommandLineAudioEncoderForm_HelpRequested(sender As Object, e As EventArgs) Handles Me.HelpRequested
        ShowHelp()
    End Sub
End Class
