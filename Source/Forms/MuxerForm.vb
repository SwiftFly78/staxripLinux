
Imports System.ComponentModel
Imports System.Globalization
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks

Imports StaxRip.UI

Public Class MuxerForm
    Inherits SizeSavingDialogBase

    Public TipProvider As TipProvider
    Public WithEvents bnCommandLinePreview, bnCancel, bnOK As ButtonEx
    Public WithEvents clcGeneral, clcVideo As CommandLineControl
    Public WithEvents tcMain As TabControlEx
    Public tpCommandLine, tpOptions, tpSubtitles, tpAudio, tpAttachments, tpTags As TabPage
    Public Label1, Label2 As LabelEx
    Public WithEvents SimpleUI As SimpleUI
    Public WithEvents bnAudioPlay, bnAudioDown, bnAudioUp, bnAudioRemove, bnAudioAdd, bnAudioEdit As ButtonEx
    Public WithEvents dgvAudio, dgvTags, dgvSubtitles As DataGridViewEx
    Public WithEvents bnAttachmentAdd, bnAttachmentRemove As ButtonEx
    Public WithEvents lbAttachments As ListBoxEx
    Public WithEvents bnSubtitleAdd, bnSubtitleRemove, bnSubtitleUp, bnSubtitleDown, bnSubtitleSetNames, bnSubtitlePlay, bnSubtitleBDSup2SubPP, bnSubtitleEdit As ButtonEx

    Private Sub InitializeComponent()
        TipProvider = New TipProvider
        bnCommandLinePreview = New ButtonEx("Command Line...")
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        clcGeneral = New CommandLineControl With {.Height = FH(4)}
        clcVideo = New CommandLineControl With {.Height = FH(4)}
        Label1 = New LabelEx With {.Text = "General:"}
        Label2 = New LabelEx With {.Text = "Video:"}
        SimpleUI = New SimpleUI

        bnAudioAdd = New ButtonEx("Add...")
        bnAudioRemove = New ButtonEx("Remove")
        bnAudioUp = New ButtonEx("Up")
        bnAudioDown = New ButtonEx("Down")
        bnAudioPlay = New ButtonEx("Play")
        bnAudioEdit = New ButtonEx("Edit...")
        dgvAudio = New DataGridViewEx

        bnSubtitleAdd = New ButtonEx("Add...")
        bnSubtitleRemove = New ButtonEx("Remove")
        bnSubtitleUp = New ButtonEx("Up")
        bnSubtitleDown = New ButtonEx("Down")
        bnSubtitleSetNames = New ButtonEx("Set Names...")
        bnSubtitlePlay = New ButtonEx("Play")
        bnSubtitleBDSup2SubPP = New ButtonEx("BDSup2Sub++")
        bnSubtitleEdit = New ButtonEx("Subtitle Edit")
        dgvSubtitles = New DataGridViewEx With {.AllowDrop = True}

        bnAttachmentAdd = New ButtonEx("Add...")
        bnAttachmentRemove = New ButtonEx("Remove")
        lbAttachments = New ListBoxEx With {.AllowDrop = True}

        dgvTags = New DataGridViewEx

        Dim buttonColumn = Function(buttons As Control()) As TableLayout
                               Dim rows = buttons.Select(Function(b) New TableRow(b)).ToList
                               rows.Add(New TableRow With {.ScaleHeight = True})
                               Return New TableLayout(rows) With {.Spacing = DefaultSpacing}
                           End Function

        tpAudio = New TabPage With {.Text = "Audio", .Padding = DefaultPadding,
            .Content = New TableLayout(ScaledRow(Scaled(dgvAudio), buttonColumn({bnAudioAdd, bnAudioRemove, bnAudioUp, bnAudioDown, bnAudioPlay, bnAudioEdit}))) With {.Spacing = DefaultSpacing}}
        tpSubtitles = New TabPage With {.Text = "Subtitles", .Padding = DefaultPadding,
            .Content = New TableLayout(ScaledRow(Scaled(dgvSubtitles), buttonColumn({bnSubtitleAdd, bnSubtitleRemove, bnSubtitleUp, bnSubtitleDown, bnSubtitleSetNames, bnSubtitlePlay, bnSubtitleBDSup2SubPP, bnSubtitleEdit}))) With {.Spacing = DefaultSpacing}}
        tpAttachments = New TabPage With {.Text = "Attachments", .Padding = DefaultPadding,
            .Content = New TableLayout(ScaledRow(Scaled(lbAttachments), buttonColumn({bnAttachmentAdd, bnAttachmentRemove}))) With {.Spacing = DefaultSpacing}}
        tpTags = New TabPage With {.Text = "Tags", .Padding = DefaultPadding, .Content = dgvTags}
        tpOptions = New TabPage With {.Text = "Options", .Padding = DefaultPadding, .Content = SimpleUI}
        tpCommandLine = New TabPage With {.Text = "Command Line", .Padding = DefaultPadding,
            .Content = New TableLayout(Row(Label1), Row(clcGeneral), Row(Label2), Row(clcVideo), New TableRow With {.ScaleHeight = True}) With {.Spacing = DefaultSpacing}}

        tcMain = New TabControlEx

        For Each page In {tpAudio, tpSubtitles, tpAttachments, tpTags, tpOptions, tpCommandLine}
            tcMain.Pages.Add(page)
        Next

        Content = PaddedTable(
            ScaledRow(tcMain),
            Row(SplitRow({bnCommandLinePreview}, {bnOK, bnCancel})))

        AbortButton = bnCancel
    End Sub

    Private ReadOnly Muxer As Muxer
    Private AudioItems As ObjectModel.ObservableCollection(Of AudioProfile)
    Private ReadOnly SubtitleItems As New ObjectModel.ObservableCollection(Of SubtitleItem)

    Sub New(muxer As Muxer)
        MyBase.New()
        InitializeComponent()
        SetMinimumSize(30, 21)
        RestoreClientSize(45, 22)
        Text = $"Container - {muxer.Name} - {g.DefaultCommands.GetApplicationDetails()}"
        Me.Muxer = muxer
        AddSubtitles(muxer.Subtitles)

        clcGeneral.tb.Text = muxer.AdditionalGeneralSwitches
        clcVideo.tb.Text = muxer.AdditionalVideoSwitches
        clcVideo.Visible = TypeOf muxer Is MP4Muxer
        Label2.Visible = TypeOf muxer Is MP4Muxer

        tcMain.SelectedIndex = s.Storage.GetInt("last selected muxer tab")

        lbAttachments.Items.AddRange(muxer.Attachments.Select(Function(val) New AttachmentContainer With {.Filepath = val}).ToArray)
        lbAttachments.RemoveButton = bnAttachmentRemove

        AudioItems = New ObjectModel.ObservableCollection(Of AudioProfile)(ObjectHelp.GetCopy(p.AudioFiles))
        dgvAudio.AddTextBoxColumn("Profile", "Name")
        dgvAudio.AddTextBoxColumn("Track", "DisplayName").Expand = True
        dgvAudio.DataStore = AudioItems

        If AudioItems.Count > 0 Then
            dgvAudio.SelectRow(0)
        End If

        dgvTags.AddTextBoxColumn("Name", "Name", True).Width = FH(10)
        dgvTags.AddTextBoxColumn("Value", "Value", True).Expand = True
        dgvTags.DataStore = muxer.Tags

        dgvSubtitles.AddCheckBoxColumn("Enabled", "Enabled")
        dgvSubtitles.AddComboBoxColumn("Language", "LanguageName",
            Language.Languages.OrderByDescending(Function(x) x.IsCommon).ThenBy(Function(x) x.EnglishName).Distinct().Select(Function(x) x.ToString))
        dgvSubtitles.AddTextBoxColumn("Name", "Title", True)
        dgvSubtitles.AddCheckBoxColumn("Default", "Default")
        dgvSubtitles.AddCheckBoxColumn("Forced", "Forced")

        If TypeOf muxer Is MkvMuxer Then
            dgvSubtitles.AddCheckBoxColumn("Commentary", "Commentary")
            dgvSubtitles.AddCheckBoxColumn("Hearingimpaired", "Hearingimpaired")
        End If

        dgvSubtitles.AddTextBoxColumn("ID", "ID")
        dgvSubtitles.AddTextBoxColumn("Type", "TypeName")
        dgvSubtitles.AddTextBoxColumn("Size", "Size")
        dgvSubtitles.AddTextBoxColumn("Filename", "Filename").Expand = True
        dgvSubtitles.DataStore = SubtitleItems

        TipProvider.SetTip("Additional command line switches that may contain macros.", tpCommandLine)
    End Sub

    Public Class AttachmentContainer
        Property Filepath As String

        Public Overrides Function ToString() As String
            Return If(Filepath.Contains("_attachment_"), Filepath.Right("_attachment_"), Path.GetFileName(Filepath))
        End Function
    End Class

    Public Class SubtitleItem
        Property Enabled As Boolean
        Property Language As Language

        Property LanguageName As String
            Get
                Return Language?.ToString
            End Get
            Set(value As String)
                Dim match = StaxRip.Language.Languages.FirstOrDefault(Function(i) i.ToString = value)

                If match IsNot Nothing Then
                    Language = match
                End If
            End Set
        End Property

        Property Title As String = ""
        Property [Default] As Boolean
        Property Forced As Boolean
        Property Commentary As Boolean
        Property Hearingimpaired As Boolean
        Property ID As Integer
        Property TypeName As String
        Property Size As String
        Property Filename As String
        Property Subtitle As Subtitle
    End Class

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)

        Dim UI = SimpleUI
        UI.Store = Muxer

        Dim page = UI.CreateFlowPage("main page")

        Dim tb = UI.AddTextButton()
        tb.Text = "Cover"
        tb.Expanded = True
        tb.Property = NameOf(Muxer.CoverFile)
        tb.BrowseFile("jpg, png, bmp|*.jpg;*.png;*.bmp")

        Dim mb = UI.AddTextMenu()
        mb.Text = "Chapters"
        mb.Expanded = True
        mb.Help = "Chapter file to be muxed."
        mb.Property = NameOf(Muxer.ChapterFile)
        mb.AddMenu("Browse File...", Function() g.BrowseFile("txt, xml|*.txt;*.xml"))
        mb.AddMenu("Edit with chapterEditor...", Sub() g.ShellExecute(Package.chapterEditor.Path, Muxer.ChapterFile.Escape))

        If TypeOf Muxer Is MkvMuxer Then
            clcGeneral.Presets = If(s.CommandLinePresets.ContainsKey(CommandLinePresetType.MuxerMkvGeneral), s.CommandLinePresets(CommandLinePresetType.MuxerMkvGeneral), "")

            mb = UI.AddTextMenu()
            mb.Text = "Tags"
            mb.Expanded = True
            mb.Help = "Tag file to be muxed."
            mb.Property = NameOf(Muxer.TagFile)
            mb.AddMenu("Browse File...", Function() g.BrowseFile("xml|*.xml"))
            mb.AddMenu("Edit File...", Sub() g.ShellExecute(g.GetAppPathForExtension("xml", "txt"), Muxer.TagFile.Escape))

            tb = UI.AddTextButton()
            tb.Text = "Timestamps"
            tb.Help = "txt or mkv file"
            tb.Expanded = True
            tb.Property = NameOf(Muxer.TimestampsFile)
            tb.BrowseFile("txt, mkv|*.txt;*.mkv")

            tb = UI.AddTextButton()
            tb.Text = "Title"
            tb.Expanded = True
            tb.Property = NameOf(MkvMuxer.Title)
            tb.Button.Text = "%"
            tb.Button.ClickAction = Sub() MacrosForm.ShowDialogForm()

            Dim t = UI.AddText()
            t.Text = "Video Track Name"
            t.Help = "Optional name of the video stream that may contain macros."
            t.Expanded = True
            t.Property = NameOf(Muxer.VideoTrackName)

            Dim tm = UI.AddTextMenu()
            tm.Text = "Display Aspect Ratio"
            tm.Help = "Display Aspect Ratio to be applied by mkvmerge. By default and best practice the aspect ratio should be signalled to the encoder and not to the muxer, use this setting at your own risk."
            tm.Property = NameOf(MkvMuxer.DAR)
            tm.AddMenu(s.DarMenu)

            Dim ml = UI.AddMenu(Of Language)()
            ml.Text = "Video Track Language"
            ml.Help = "Optional language of the video stream."
            ml.Property = NameOf(MkvMuxer.VideoTrackLanguage)

            g.PopulateLanguagesAsync(ml.Button)

            Dim compression = UI.AddMenu(Of CompressionMode)()
            compression.Text = "Subtitle Compression"
            compression.Property = NameOf(MkvMuxer.Compression)
        ElseIf TypeOf Muxer Is MP4Muxer Then
            tpAttachments.Enabled = False

            clcGeneral.Presets = If(s.CommandLinePresets.ContainsKey(CommandLinePresetType.MuxerMP4General), s.CommandLinePresets(CommandLinePresetType.MuxerMP4General), "")
            clcVideo.Presets = If(s.CommandLinePresets.ContainsKey(CommandLinePresetType.MuxerMP4Video), s.CommandLinePresets(CommandLinePresetType.MuxerMP4Video), "")

            Dim txt = UI.AddText()
            txt.Text = "Video Track Name"
            txt.Help = "Optional name of the video stream that may contain macros."
            txt.Expanded = True
            txt.Property = NameOf(Muxer.VideoTrackName)

            Dim textMenu = UI.AddTextMenu()
            textMenu.Text = "Pixel Aspect Ratio:"
            textMenu.Help = "Display Aspect Ratio to be applied by MP4Box. By default and best practice the aspect ratio should be signalled to the encoder and not to the muxer, use this setting at your own risk."
            textMenu.Property = NameOf(MP4Muxer.PAR)
            textMenu.AddMenu(s.ParMenu)
        End If

        UpdateControls()
    End Sub

    Protected Overrides Sub OnClosed(e As EventArgs)
        MyBase.OnClosed(e)

        If TypeOf Muxer Is MkvMuxer Then
            s.CommandLinePresets(CommandLinePresetType.MuxerMkvGeneral) = clcGeneral.Presets
        ElseIf TypeOf Muxer Is MP4Muxer Then
            s.CommandLinePresets(CommandLinePresetType.MuxerMP4General) = clcGeneral.Presets
            s.CommandLinePresets(CommandLinePresetType.MuxerMP4Video) = clcVideo.Presets
        End If

        s.Storage.SetInt("last selected muxer tab", tcMain.SelectedIndex)
        SetValues()

        If DialogResult = DialogResult.OK Then
            p.AudioFiles = AudioItems.ToList
            Muxer.Attachments.Clear()
            Muxer.Attachments.AddRange(lbAttachments.Items.OfType(Of AttachmentContainer).Select(Function(val) val.Filepath))
        End If
    End Sub

    Sub SetValues()
        Muxer.Subtitles.Clear()

        For Each subtitle In SubtitleItems
            subtitle.Subtitle.Language = subtitle.Language
            subtitle.Subtitle.Enabled = subtitle.Enabled
            subtitle.Subtitle.Title = subtitle.Title
            subtitle.Subtitle.Default = subtitle.Default
            subtitle.Subtitle.Forced = subtitle.Forced
            subtitle.Subtitle.Commentary = subtitle.Commentary
            subtitle.Subtitle.Hearingimpaired = subtitle.Hearingimpaired

            If subtitle.Subtitle.Title Is Nothing Then
                subtitle.Subtitle.Title = ""
            End If

            Muxer.Subtitles.Add(subtitle.Subtitle)
        Next

        SimpleUI.Save()
        Muxer.AdditionalGeneralSwitches = clcGeneral.tb.Text
        Muxer.AdditionalVideoSwitches = clcVideo.tb.Text
    End Sub

    Sub AddAttachment(paths As String())
        Dim attachmentItems = lbAttachments.Items.OfType(Of AttachmentContainer).Select(Function(val) val.Filepath).ToList
        attachmentItems.AddRange(paths)
        attachmentItems.Sort()
        lbAttachments.Items.Clear()
        lbAttachments.Items.AddRange(attachmentItems.Select(Function(val) New AttachmentContainer With {.Filepath = val}).ToArray)
        UpdateControls()
    End Sub

    Sub UpdateControls()
        bnAudioRemove.Enabled = dgvAudio.HasSelection
        bnAudioPlay.Enabled = dgvAudio.HasSelection
        bnAudioEdit.Enabled = dgvAudio.HasSelection
        bnAudioUp.Enabled = dgvAudio.CanMoveUp
        bnAudioDown.Enabled = dgvAudio.CanMoveDown

        Dim selectedSubtitles = dgvSubtitles.HasSelection
        Dim subtitlePath = If(selectedSubtitles, SubtitleItems(dgvSubtitles.SelectedIndex).Subtitle.Path, "")
        bnSubtitleBDSup2SubPP.Enabled = selectedSubtitles AndAlso {"idx", "sup"}.Contains(subtitlePath.Ext)
        bnSubtitleEdit.Enabled = selectedSubtitles
        bnSubtitlePlay.Enabled = p.SourceFile <> "" AndAlso selectedSubtitles
        bnSubtitleUp.Enabled = dgvSubtitles.CanMoveUp
        bnSubtitleDown.Enabled = dgvSubtitles.CanMoveDown
        bnSubtitleSetNames.Enabled = selectedSubtitles
        bnSubtitleRemove.Enabled = selectedSubtitles

        lbAttachments.UpdateControls()
    End Sub

    Sub AddAudio(path As String)
        Dim profileSelection As New SelectionBox(Of AudioProfile) With {
            .Title = "Audio Profile",
            .Text = "Please select an audio profile."
        }

        For Each audioProfile In s.AudioProfiles
            profileSelection.AddItem(audioProfile)
        Next

        profileSelection.SelectedText = "Copy/Mux"

        If profileSelection.Show <> DialogResult.OK Then
            Exit Sub
        End If

        Dim ap = ObjectHelp.GetCopy(profileSelection.SelectedValue)
        ap.File = path

        If Not p.Script.GetFilter("Source").Script.Contains("DirectShowSource") Then
            ap.Delay += g.ExtractDelay(ap.File)
        End If

        Dim trackname = g.ExtractTrackNameFromFilename(path)
        ap.StreamName = If(trackname, ap.StreamName)

        If FileTypes.VideoAudio.Contains(ap.File.Ext) Then
            ap.Streams = MediaInfo.GetAudioStreams(ap.File)
        End If

        ap.SetStreamOrLanguage()

        If ap.Stream IsNot Nothing Then
            Dim streamSelection As New SelectionBox(Of AudioStream) With {
                .Title = "Stream Selection",
                .Text = "Please select an audio stream."
            }

            For Each stream In ap.Streams
                streamSelection.AddItem(stream)
            Next

            If streamSelection.Show <> DialogResult.OK Then
                Exit Sub
            End If

            ap.Stream = streamSelection.SelectedValue
        End If

        g.MainForm.UpdateSizeOrBitrate()
        AudioItems.Add(ap)
        dgvAudio.SelectRow(AudioItems.Count - 1)
        UpdateControls()
    End Sub

    Sub AddSubtitles(subtitles As List(Of Subtitle))
        For Each i In subtitles
            If SubtitleItems.Where(Function(item) item.Default).Count > 0 Then
                i.Default = False
            End If

            If File.Exists(i.Path) Then
                Dim sizeText = ""

                If i.Size > 0 Then
                    sizeText = If(i.Size > PrefixedSize(2).Factor,
                        $"{i.Size / PrefixedSize(2).Factor:f1} {PrefixedSize(2).Unit}",
                        $"{i.Size / PrefixedSize(1).Factor:f1} {PrefixedSize(1).Unit}")
                End If

                Dim _option As String = ""

                If i.Default Then _option &= ", default"
                If i.Forced Then _option &= ", forced"
                If i.Commentary Then _option &= ", comment"
                If i.Hearingimpaired Then _option &= ", hearingimpaired"
                _option = _option.TrimStart(","c).Trim()

                Dim match = Regex.Match(i.Path, " ID(\d+)")
                Dim id = If(match.Success, CInt(match.Groups(1).Value), i.StreamOrder + 1)

                Dim item As New SubtitleItem With {
                    .Enabled = i.Enabled,
                    .Language = i.Language,
                    .Title = i.Title,
                    .Default = i.Default,
                    .Forced = i.Forced,
                    .Commentary = i.Commentary,
                    .Hearingimpaired = i.Hearingimpaired,
                    .ID = id,
                    .TypeName = i.TypeName,
                    .Size = sizeText,
                    .Filename = i.Path.FileName,
                    .Subtitle = i
                }

                SubtitleItems.Add(item)
            End If
        Next
    End Sub

    Sub bnSubtitlePlay_Click(sender As Object, e As EventArgs) Handles bnSubtitlePlay.Click
        Package.mpvnet.VerifyOK(True)
        Dim st = SubtitleItems(dgvSubtitles.SelectedIndex).Subtitle
        Dim filepath = st.Path

        If st.Path.Ext = "idx" Then
            filepath = Path.Combine(p.TempDir, p.TargetFile.Base + "_play.idx")
            Regex.Replace(st.Path.ReadAllText, "langidx: \d+", "langidx: " +
                          st.IndexIDX.ToString).WriteFileSystemEncoding(filepath)
            FileHelp.Copy(st.Path.DirAndBase + ".sub", filepath.DirAndBase + ".sub")
        End If

        If FileTypes.SubtitleExludingContainers.Contains(filepath.Ext) Then
            Dim count = MediaInfo.GetSubtitleCount(p.FirstOriginalSourceFile)
            g.ShellExecute(Package.mpvnet.Path, "--sub-file=" + filepath.Escape + " " +
                p.FirstOriginalSourceFile.Escape + $" --sid={count + 1}")
        ElseIf p.FirstOriginalSourceFile = filepath Then
            g.ShellExecute(Package.mpvnet.Path, "--sid=" & (st.Index + 1) & " " + filepath.Escape)
        End If
    End Sub

    Sub bnCommandLinePreview_Click(sender As Object, e As EventArgs) Handles bnCommandLinePreview.Click
        SetValues()
        g.ShowCommandLinePreview("Command Line", Muxer.GetCommandLine, False)
    End Sub

    Sub bnAudioAdd_Click(sender As Object, e As EventArgs) Handles bnAudioAdd.Click
        Using dialog As New OpenFileDialog
            dialog.Multiselect = True
            dialog.SetFilter(FileTypes.Audio.Union(FileTypes.VideoAudio))
            dialog.SetInitDir(p.TempDir)

            If dialog.ShowDialog = DialogResult.OK Then
                For Each path In dialog.FileNames
                    AddAudio(path)
                Next
            End If
        End Using
    End Sub

    Sub bnAudioRemove_Click(sender As Object, e As EventArgs) Handles bnAudioRemove.Click
        dgvAudio.RemoveSelection()
        UpdateControls()
    End Sub

    Sub bnAudioUp_Click(sender As Object, e As EventArgs) Handles bnAudioUp.Click
        dgvAudio.MoveSelectionUp
        UpdateControls()
    End Sub

    Sub bnAudioDown_Click(sender As Object, e As EventArgs) Handles bnAudioDown.Click
        dgvAudio.MoveSelectionDown
        UpdateControls()
    End Sub

    Sub bnAudioPlay_Click(sender As Object, e As EventArgs) Handles bnAudioPlay.Click
        g.Play(AudioItems(dgvAudio.SelectedIndex).File)
    End Sub

    Sub bnAudioEdit_Click(sender As Object, e As EventArgs) Handles bnAudioEdit.Click
        Dim ap = AudioItems(dgvAudio.SelectedIndex)
        ap.EditProject()
        g.MainForm.UpdateAudioMenus()
        g.MainForm.UpdateSizeOrBitrate()
        dgvAudio.RefreshData()
    End Sub

    Sub bnAttachmentAdd_Click(sender As Object, e As EventArgs) Handles bnAttachmentAdd.Click
        Using dialog As New OpenFileDialog
            dialog.Multiselect = True
            dialog.SetFilter({"ttf", "txt", "jpg", "png", "otf", "jpeg", "xml", "nfo"})
            dialog.SetInitDir(p.TempDir)

            If dialog.ShowDialog = DialogResult.OK Then
                AddAttachment(dialog.FileNames)
            End If
        End Using
    End Sub

    Sub lbAttachments_DragDrop(sender As Object, e As DragEventArgs) Handles lbAttachments.DragDrop
        Dim files = FormBase.GetDroppedFiles(e)

        If Not files.NothingOrEmpty Then
            AddAttachment(files)
        End If
    End Sub

    Sub lbAttachments_DragOver(sender As Object, e As DragEventArgs) Handles lbAttachments.DragOver
        e.Effects = DragEffects.Copy
    End Sub

    Sub dgvAudio_SelectionChanged(sender As Object, e As EventArgs) Handles dgvAudio.SelectionChanged
        UpdateControls()
    End Sub

    Sub dgvAudio_DragOver(sender As Object, e As DragEventArgs) Handles dgvAudio.DragOver
        e.Effects = DragEffects.Copy
    End Sub

    Sub dgvAudio_DragDrop(sender As Object, e As DragEventArgs) Handles dgvAudio.DragDrop
        Dim files = FormBase.GetDroppedFiles(e)

        If Not files.NothingOrEmpty Then
            For Each filePath In files
                If FileTypes.VideoAudio.Contains(filePath.Ext) Then
                    AddAudio(filePath)
                End If
            Next
        End If
    End Sub

    Sub MuxerForm_HelpRequested(sender As Object, ea As EventArgs) Handles Me.HelpRequested
        Dim form As New HelpForm()
        form.Doc.WriteStart(Text)
        form.Doc.WriteParagraph(
            "A muxer merges different video, audio and subtitle files " +
            "into a single container file which is the actual output file.")
        form.Doc.WriteTips(TipProvider.GetTips, SimpleUI.ActivePage.TipProvider.GetTips)
        form.Doc.WriteTable("Macros", Macro.GetTips(True, False, True, True, False))
        form.Show()
    End Sub

    Sub dgvSubtitles_DragOver(sender As Object, ea As DragEventArgs) Handles dgvSubtitles.DragOver
        ea.Effects = DragEffects.Copy
    End Sub

    Sub dgvSubtitles_DragDrop(sender As Object, ea As DragEventArgs) Handles dgvSubtitles.DragDrop
        Dim files = FormBase.GetDroppedFiles(ea)

        If Not files.NothingOrEmpty Then
            For Each filePath In files
                If FileTypes.SubtitleIncludingContainers.Contains(filePath.Ext) Then
                    AddSubtitles(Subtitle.Create(filePath))
                End If
            Next
        End If
    End Sub

    Sub dgvSubtitles_CellEdited(sender As Object, e As GridViewCellEventArgs) Handles dgvSubtitles.CellEdited
        UpdateControls()
    End Sub

    Sub bnSubtitleAdd_Click(sender As Object, e As EventArgs) Handles bnSubtitleAdd.Click
        Using dialog As New OpenFileDialog
            dialog.Multiselect = True
            dialog.SetFilter(FileTypes.SubtitleIncludingContainers)
            dialog.SetInitDir(s.LastSourceDir)

            If dialog.ShowDialog = DialogResult.OK Then
                For Each fn In dialog.FileNames
                    AddSubtitles(Subtitle.Create(fn))
                Next

                UpdateControls()
            End If
        End Using
    End Sub

    Sub bnSubtitleRemove_Click(sender As Object, e As EventArgs) Handles bnSubtitleRemove.Click
        dgvSubtitles.RemoveSelection()
        UpdateControls()
    End Sub

    Sub bnSubtitleUp_Click(sender As Object, e As EventArgs) Handles bnSubtitleUp.Click
        dgvSubtitles.MoveSelectionUp()
        UpdateControls()
    End Sub

    Sub bnSubtitleDown_Click(sender As Object, e As EventArgs) Handles bnSubtitleDown.Click
        dgvSubtitles.MoveSelectionDown()
        UpdateControls()
    End Sub

    Sub bnSubtitleSetNames_Click(sender As Object, e As EventArgs) Handles bnSubtitleSetNames.Click
        Using td As New TaskDialog(Of Integer)
            td.Title = "Set names for all streams."
            td.AddCommand("Set language in English", 1)

            If CultureInfo.CurrentCulture.NeutralCulture.TwoLetterISOLanguageName <> "en" Then
                td.AddCommand("Set language in " + CultureInfo.CurrentCulture.NeutralCulture.DisplayName, 2)
            End If

            Select Case td.Show
                Case 1
                    For Each i In SubtitleItems
                        i.Title = i.Language.EnglishName

                        If i.Forced Then i.Title += " (forced)"
                        If i.Commentary Then i.Title += " (commentary)"
                        If i.Hearingimpaired Then i.Title += " (hearingimpaired)"
                    Next

                    dgvSubtitles.RefreshData()
                Case 2
                    For Each i In SubtitleItems
                        i.Title = If(i.Language.IsDetermined, i.Language.CultureInfo.NeutralCulture.DisplayName, i.Language.LocalName)

                        If i.Forced Then i.Title += " (forced)"
                        If i.Commentary Then i.Title += " (commentary)"
                        If i.Hearingimpaired Then i.Title += " (hearingimpaired)"
                    Next

                    dgvSubtitles.RefreshData()
            End Select
        End Using
    End Sub

    Sub bnSubtitleBDSup2SubPP_Click(sender As Object, e As EventArgs) Handles bnSubtitleBDSup2SubPP.Click
        Package.BDSup2SubPP.VerifyOK(True)

        Try
            Dim st = SubtitleItems(dgvSubtitles.SelectedIndex).Subtitle
            Dim fp = st.Path

            If fp.Ext = "idx" Then
                fp = Path.Combine(p.TempDir, p.TargetFile.Base + "_temp.idx")
                Regex.Replace(st.Path.ReadAllText, "langidx: \d+", "langidx: " + st.IndexIDX.ToString).WriteFileSystemEncoding(fp)
                FileHelp.Copy(st.Path.DirAndBase + ".sub", fp.DirAndBase + ".sub")
            End If

            g.ShellExecute(Package.BDSup2SubPP.Path, fp.Escape)
        Catch ex As Exception
            g.ShowException(ex)
        End Try
    End Sub

    Sub bnSubtitleEdit_Click(sender As Object, e As EventArgs) Handles bnSubtitleEdit.Click
        Package.SubtitleEdit.VerifyOK(True)

        Try
            Dim st = SubtitleItems(dgvSubtitles.SelectedIndex).Subtitle
            Dim fp = st.Path

            If fp.ExtFull = ".idx" Then
                fp = Path.Combine(p.TempDir, p.TargetFile.Base + "_temp.idx")
                Regex.Replace(st.Path.ReadAllText, "langidx: \d+", "langidx: " + st.IndexIDX.ToString).WriteFileSystemEncoding(fp)
                FileHelp.Copy(st.Path.DirAndBase + ".sub", fp.DirAndBase + ".sub")
            End If

            g.ShellExecute(Package.SubtitleEdit.Path, fp.Escape)
        Catch ex As Exception
            g.ShowException(ex)
        End Try
    End Sub


    Sub dgvSubtitles_SelectionChanged(sender As Object, e As EventArgs) Handles dgvSubtitles.SelectionChanged
        UpdateControls()
    End Sub
End Class
