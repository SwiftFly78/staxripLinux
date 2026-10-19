
Imports System.ComponentModel
Imports System.Globalization
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks

Imports StaxRip.UI

Public Class eac3toForm
    Inherits SizeSavingDialogBase

    Public WithEvents cbVideoOutput, cbVideoStream, cbAudioOutput As ComboBoxEx
    Public WithEvents cmdlOptions As CommandLineControl
    Public WithEvents bnBrowse, bnCancel, bnOK, bnMenu As ButtonEx
    Public WithEvents lvAudio, lvSubtitles As ListViewEx
    Public WithEvents cbChapters As CheckBoxEx
    Public gbAudio, gbSubtitles As GroupBox
    Public WithEvents cms As ContextMenuStripEx
    Public WithEvents bnAudioAll, bnAudioNone, bnAudioEnglish, bnAudioNative As ButtonEx
    Public WithEvents bnSubtitleAll, bnSubtitleNone, bnSubtitleEnglish, bnSubtitleNative As ButtonEx
    Public WithEvents teTempDir As TextEdit
    Public flpSubtitleLinks As Control
    Private MainLayout As TableLayout

    Private Sub InitializeComponent()
        cbVideoOutput = New ComboBoxEx
        cbVideoStream = New ComboBoxEx
        cbAudioOutput = New ComboBoxEx
        cmdlOptions = New CommandLineControl With {.Height = FH(2.5)}
        bnBrowse = New ButtonEx("...")
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        cms = New ContextMenuStripEx
        bnMenu = New ButtonEx With {.ShowMenuSymbol = True, .ContextMenuStrip = cms}
        lvAudio = New ListViewEx
        lvSubtitles = New ListViewEx
        cbChapters = New CheckBoxEx With {.Text = "Extract Chapters"}
        bnAudioAll = New ButtonEx("All")
        bnAudioNone = New ButtonEx("None")
        bnAudioEnglish = New ButtonEx("English")
        bnAudioNative = New ButtonEx("Native")
        bnSubtitleAll = New ButtonEx("All")
        bnSubtitleNone = New ButtonEx("None")
        bnSubtitleEnglish = New ButtonEx("English")
        bnSubtitleNative = New ButtonEx("Native")
        teTempDir = New TextEdit

        Dim video As New TableLayout(Row(New LabelEx With {.Text = "Output:"}, Scaled(cbVideoOutput), New LabelEx With {.Text = "Stream:"}, Scaled(cbVideoStream))) With {.Spacing = DefaultSpacing}
        Dim audioOptions As New TableLayout(Row(New LabelEx With {.Text = "Output:"}, cbAudioOutput, New LabelEx With {.Text = "Options:"}, Scaled(cmdlOptions))) With {.Spacing = DefaultSpacing}

        gbAudio = New GroupBox With {
            .Text = "Audio",
            .Content = New TableLayout(ScaledRow(lvAudio), Row(HRow(bnAudioAll, bnAudioNone, bnAudioEnglish, bnAudioNative)), Row(audioOptions)) With {.Spacing = DefaultSpacing}}

        flpSubtitleLinks = HRow(bnSubtitleAll, bnSubtitleNone, bnSubtitleEnglish, bnSubtitleNative)
        gbSubtitles = New GroupBox With {
            .Text = "Subtitles",
            .Content = New TableLayout(ScaledRow(lvSubtitles), Row(flpSubtitleLinks)) With {.Spacing = DefaultSpacing}}

        Dim target As New TableLayout(Row(New LabelEx With {.Text = "Target Directory:"}, Scaled(teTempDir), bnBrowse)) With {.Spacing = DefaultSpacing}

        MainLayout = PaddedTable(
            Row(video),
            ScaledRow(gbAudio),
            ScaledRow(gbSubtitles),
            Row(target),
            Row(SplitRow({cbChapters}, {bnMenu, bnOK, bnCancel})))

        Content = MainLayout
        AbortButton = bnCancel
        Text = $"eac3to Demuxing - {g.DefaultCommands.GetApplicationDetails()}"
    End Sub

    Sub SetControlsEnabled(value As Boolean)
        MainLayout.Enabled = value
    End Sub

    Property M2TSFile As String
    Property PlaylistFolder As String
    Property OutputFolder As String
    Property PlaylistID As Integer
    Property Streams As New BindingList(Of M2TSStream)

    Private Output As String
    Private ReadOnly AudioOutputFormats As String() = {"m4a", "ac3", "dts", "flac", "pcm", "wav", "dtsma", "dtshr", "eac3", "thd"}

    'eac3to 3.60 and newer use the "DTS-HD" names
    Private ReadOnly DtsHdCodecs As String() = {"DTS Master Audio", "DTS-HD Master Audio", "DTS-HD Master Audio X", "DTS Hi-Res", "DTS-HD High Resolution"}

    Private Project As Project

    Sub New(proj As Project)
        MyBase.New()
        InitializeComponent()
        RestoreClientSize(40, 30)
        Project = proj

        cbAudioOutput.Items.AddRange(AudioOutputFormats.OrderBy(Function(i) i).ToArray)
        SetControlsEnabled(False)

        cbChapters.Checked = s.Storage.GetBool("demux Blu-ray chapters", True)

        For Each lv In {lvAudio, lvSubtitles}
            lv.View = View.Details
            lv.Columns.Add("")
            lv.CheckBoxes = True
            lv.HeaderStyle = ColumnHeaderStyle.None
            lv.FullRowSelect = True
            lv.MultiSelect = False
            lv.AutoCheckMode = AutoCheckMode.SingleClick
            lv.Visible = False
        Next

        cmdlOptions.Presets = If(s.CommandLinePresets.ContainsKey(CommandLinePresetType.Eac3to), s.CommandLinePresets(CommandLinePresetType.Eac3to), "")
        cmdlOptions.RestoreFunc = Function() ApplicationSettings.GetDefaultEac3toMenu.FormatColumn("=")

        bnAudioNative.Visible = False
        bnAudioEnglish.Visible = False
        bnSubtitleNative.Visible = False
        bnSubtitleEnglish.Visible = False

        If CultureInfo.CurrentCulture.TwoLetterISOLanguageName <> "en" Then
            Try
                bnAudioNative.Text = New CultureInfo(CultureInfo.CurrentCulture.TwoLetterISOLanguageName).EnglishName
                bnSubtitleNative.Text = bnAudioNative.Text
            Catch ex As Exception
                g.ShowException(ex)
                bnAudioNative.Visible = False
                bnSubtitleNative.Visible = False
            End Try
        End If

        cms.Items.Add(New MenuItemEx("Audio Stream Profiles...", AddressOf ShowAudioStreamProfiles))
        cms.Items.Add(New MenuItemEx("Show eac3to wikibook", Sub() g.ShellExecute("https://en.wikibooks.org/wiki/Eac3to")))
        cms.Items.Add(New MenuItemEx("Show eac3to support forum", Sub() g.ShellExecute("https://forum.doom9.org/showthread.php?t=125966")))
        cms.Items.Add(New MenuItemEx("Execute eac3to -test", Sub() DesktopHelp.RunInTerminal(ShellHelp.Quote(Package.eac3to.Path) + " -test")))
    End Sub

    Sub ShowAudioStreamProfilesHelp()
        Dim form As New HelpForm
        form.Doc.WriteStart("Audio Stream Profiles")
        form.Doc.WriteParagraph("Allows to automatically apply default values for audio streams.")
        form.Doc.WriteTable({New StringPair("Match All", "space separated, if all match then the Output Format and Options are applied"),
                             New StringPair("Output Format", "applied to the stream if Match All succeeds"),
                             New StringPair("Options", "applied to the stream if Match All succeeds")})
        form.Show()
    End Sub

    Sub ShowAudioStreamProfiles()
        Using form As New DataForm
            form.Text = $"Audio Stream Profiles - {g.DefaultCommands.GetApplicationDetails()}"
            form.HelpAction = AddressOf ShowAudioStreamProfilesHelp

            Dim items As New ObjectModel.ObservableCollection(Of eac3toProfile)(ObjectHelp.GetCopy(s.eac3toProfiles))
            form.NewItemFunc = Function() New eac3toProfile("", AudioOutputFormats(0), "")

            form.Grid.Columns.Add(New GridColumn With {.HeaderText = "Match All", .Editable = True, .Expand = True, .DataCell = New TextBoxCell("Match")})
            form.Grid.Columns.Add(New GridColumn With {.HeaderText = "Output Format", .Editable = True, .DataCell = New ComboBoxCell("Output") With {.DataStore = AudioOutputFormats}})
            form.Grid.Columns.Add(New GridColumn With {.HeaderText = "Options", .Editable = True, .Expand = True, .DataCell = New TextBoxCell("Options")})
            form.Grid.DataStore = items

            If form.ShowDialog = DialogResult.OK Then
                s.eac3toProfiles = items.ToList
            End If
        End Using
    End Sub

    'eac3to applies -minDuration only together with -showall, which changes the playlist numbers,
    'so listing, analyzing and demuxing must use the same options
    Shared Function GetPlaylistOptions(proj As Project) As String
        Return If(proj.MinBdmvPlaylistDuration >= 0, $" -showall -minDuration={proj.MinBdmvPlaylistDuration}", "")
    End Function

    Sub StartAnalyze()
        Dim args = ""

        If File.Exists(M2TSFile) Then
            args = WineHelp.ToWinePath(M2TSFile).Escape + " -progressnumbers"
            Project.Log.Write("Process M2TS file using eac3to", Package.eac3to.Path.Escape + " " + args + BR2)
        ElseIf Directory.Exists(PlaylistFolder) Then
            args = WineHelp.ToWinePath(PlaylistFolder).Escape + " " & PlaylistID & ") -progressnumbers" + GetPlaylistOptions(Project)
            Project.Log.Write("Process playlist file using eac3to", Package.eac3to.Path.Escape + " " + args + BR2)
        End If

        Using pr As New Process
            AddHandler pr.OutputDataReceived, AddressOf OutputDataReceived
            pr.StartInfo.FileName = Package.eac3to.Path
            ShellHelp.SetArguments(pr.StartInfo, args)
            pr.StartInfo.CreateNoWindow = True
            pr.StartInfo.UseShellExecute = False
            pr.StartInfo.RedirectStandardOutput = True
            pr.Start()
            pr.BeginOutputReadLine()
            pr.WaitForExit()

            If pr.ExitCode <> 0 Then
                Dim exitCode = pr.ExitCode

                BeginInvoke(Sub()
                                MsgError("eac3to failed with error code " & exitCode, Output)
                                Cancel()
                            End Sub)
            Else
                BeginInvoke(Sub() Init())
            End If
        End Using
    End Sub

    Sub Cancel()
        SetControlsEnabled(True)
        Close(DialogResult.Cancel)
    End Sub

    Sub OutputDataReceived(sender As Object, e As DataReceivedEventArgs)
        If e.Data IsNot Nothing Then
            If e.Data.StartsWith("analyze: ") Then
                BeginInvoke(Sub() Me.Text = e.Data)
            Else
                Output += e.Data + BR
                BeginInvoke(Sub() Me.Text = $"eac3to Demuxing - {g.DefaultCommands.GetApplicationDetails()}")
            End If
        End If
    End Sub

    Sub Init()
        SetControlsEnabled(True)

        If Output = "" Then
            MsgWarn("eac3to output was empty")
            Cancel()
        ElseIf Output.ContainsAll("left eye", "right eye") Then
            MsgError("3D demuxing isn't supported.")
            Cancel()
        ElseIf Output <> "" Then
            Project.Log.WriteLine(Output)

            If Output.Contains(BR + "   (embedded: ") Then
                Output = Output.Replace(BR + "   (embedded: ", "(embedded: ")
            End If

            While Output.Contains("  (embedded: ")
                Output = Output.Replace("  (embedded: ", " (embedded: ")
            End While

            If Output.Contains(BR + "   (core: ") Then
                Output = Output.Replace(BR + "   (core: ", "(core: ")
            End If

            While Output.Contains("  (core: ")
                Output = Output.Replace("  (core: ", " (core: ")
            End While

            Output = Output.Replace(" channels, ", "ch, ").Replace(" bits, ", "bits, ").Replace("dialnorm", "dn")
            Output = Output.Replace("(core: ", "(").Replace("(embedded: ", "(")

            Dim aid = 1
            Dim sid = 1

            For Each line In Output.SplitLinesNoEmpty
                If line.Contains("Subtitle (DVB)") Then Continue For

                Dim match = Regex.Match(line, "^(\d+): (.+)$")

                If match.Success Then
                    Dim ms As New M2TSStream With {
                        .Text = line.Trim,
                        .ID = match.Groups(1).Value.ToInt,
                        .Codec = match.Groups(2).Value
                    }

                    ms.IsVideoEnhancementLayer = ms.Codec.StartsWith("*") AndAlso ms.Codec.ContainsAll("h265/HEVC", "1080p")

                    If ms.Codec.Contains(",") Then ms.Codec = ms.Codec.Left(",")
                    If ms.Codec.StartsWith("*") Then ms.Codec = ms.Codec.Right("*")

                    Dim audioNamesAndExtensions = New Dictionary(Of String, String) From {
                        {"AC3", "ac3"},
                        {"AC3 EX", "ac3"},
                        {"AC3 Surround", "ac3"},
                        {"AC3 Headphone", "ac3"},
                        {"E-AC3", "eac3"},
                        {"EAC3", "eac3"},
                        {"E-AC3 EX", "eac3"},
                        {"E-AC3 Surround", "eac3"},
                        {"TrueHD/AC3 (Atmos)", "thd"},
                        {"TrueHD/AC3", "thd"},
                        {"TrueHD (Atmos)", "thd"},
                        {"DTS", "dts"},
                        {"DTS-ES", "dts"},
                        {"DTS Express", "dts"},
                        {"DTS Master Audio", "dtsma"},
                        {"DTS-HD Master Audio", "dtsma"},
                        {"DTS-HD Master Audio X", "dtsma"},
                        {"DTS-HD High Resolution", "dtshr"},
                        {"DTS Hi-Res", "dtshr"},
                        {"RAW/PCM", "flac"},
                        {"AAC", "m4a"},
                        {"MP2", "mp2"},
                        {"MP3", "mp3"},
                        {"Subtitle (ASS)", "ass"}
                    }

                    ms.IsVideo = ms.Codec.EqualsAny("h264/AVC", "h265/HEVC", "VC-1", "MPEG2")
                    ms.IsAudio = ms.Codec.EqualsAny(audioNamesAndExtensions.Keys.ToArray())
                    ms.IsSubtitle = ms.Codec.StartsWith("Subtitle")
                    ms.IsChapters = ms.Codec.StartsWith("Chapters")

                    If ms.IsAudio Then
                        ms.TypeID = aid
                        aid += 1

                        If Not lvAudio.Visible Then
                            lvAudio.Visible = True
                        End If
                    End If

                    If ms.IsSubtitle Then
                        ms.TypeID = sid
                        sid += 1

                        If Not lvSubtitles.Visible Then
                            lvSubtitles.Visible = True
                        End If
                    End If

                    If ms.IsAudio OrElse ms.IsSubtitle Then
                        'eac3to 3.54 and older show the English name instead of the code
                        Dim lang = If(Language.Languages.FirstOrDefault(Function(lng) ms.Text.Contains(", [" + lng.CultureInfo.ThreeLetterISOLanguageName)),
                                      Language.Languages.FirstOrDefault(Function(lng) ms.Text.Contains(", " + lng.CultureInfo.EnglishName + ",") OrElse
                                                                                      ms.Text.EndsWith(", " + lng.CultureInfo.EnglishName)))

                        If lang IsNot Nothing Then
                            ms.Language = lang
                        End If

                        If ms.IsAudio Then
                            Dim ot As String
                            If audioNamesAndExtensions.TryGetValue(ms.Codec, ot)
                                ms.OutputType = ot
                            Else
                                ms.OutputType = ms.Codec.ToLower.Replace("-", "")
                            End If
                        End If

                        If  ms.IsSubtitle Then
                            Select Case ms.Codec
                                Case "Subtitle (ASS)"
                                    ms.OutputType = "ass"
                                Case Else
                                    ms.OutputType = ms.Codec.ToLower.Replace("-", "")
                            End Select
                        End If
                    End If

                    For Each pro In s.eac3toProfiles
                        Dim searchWords = pro.Match.SplitNoEmptyAndWhiteSpace(" ")

                        If searchWords.NothingOrEmpty Then
                            Continue For
                        End If

                        If ms.Text.ContainsAll(searchWords) Then
                            ms.OutputType = pro.Output
                            ms.Options = pro.Options
                        End If
                    Next

                    If Not ms.IsVideo AndAlso Not ms.IsAudio AndAlso
                        Not ms.IsSubtitle AndAlso Not ms.IsChapters Then

                        Throw New Exception("Failed to detect stream: " + line)
                    End If

                    Streams.Add(ms)
                End If
            Next

            For Each stream In Streams
                If stream.IsAudio Then
                    stream.ListViewItem = lvAudio.Items.Add(stream.ToString)
                    stream.ListViewItem.Tag = stream

                    Dim autoCode = Project.PreferredAudio.ToLowerInvariant.SplitNoEmptyAndWhiteSpace(",", ";", " ")
                    stream.ListViewItem.Checked = autoCode.ContainsAny("all", stream.Language.TwoLetterCode, stream.Language.ThreeLetterCode)
                ElseIf stream.IsVideo Then
                    cbVideoStream.Items.Add(stream)
                ElseIf stream.IsSubtitle Then
                    Dim item = lvSubtitles.Items.Add(stream.Language.ToString)
                    item.Tag = stream

                    Dim autoCode = Project.PreferredSubtitles.ToLowerInvariant.SplitNoEmptyAndWhiteSpace(",", ";", " ")
                    item.Checked = autoCode.ContainsAny("all", stream.Language.TwoLetterCode, stream.Language.ThreeLetterCode)
                ElseIf stream.IsChapters Then
                    cbChapters.Visible = True
                End If
            Next

            If cbVideoStream.Items.Count < 2 Then
                cbVideoStream.Enabled = False
            End If

            If cbVideoStream.Items.Count > 0 Then
                cbVideoStream.SelectedIndex = 0
            Else
                cbVideoOutput.Enabled = False
            End If

            If lvAudio.Items.Count > 0 Then
                lvAudio.Items(0).Selected = True
            Else
                gbAudio.Enabled = False
            End If

            If lvSubtitles.Items.Count = 0 Then
                lvSubtitles.Enabled = False
                flpSubtitleLinks.Enabled = False
            End If

            lvAudio.AutoResizeColumns(True)
            lvSubtitles.AutoResizeColumns(True)
        End If
    End Sub

    Sub AddCmdlControl_PresetsChanged(presets As String) Handles cmdlOptions.PresetsChanged
        cmdlOptions.Presets = presets
    End Sub

    Function GetArgs(src As String, baseName As String, ByRef outFiles As List(Of String)) As String
        Dim ret = src

        Dim videoStream = TryCast(cbVideoStream.SelectedItem, M2TSStream)

        If videoStream IsNot Nothing AndAlso Not cbVideoOutput.Text = "Nothing" Then
            Dim outFile = Path.Combine(OutputFolder, baseName + "." + cbVideoOutput.Text.ToLowerInvariant)
            If Not outFile.FileExists() Then
                ret += " " & videoStream.ID & ": " + WineHelp.ToWinePath(outFile).Escape
                outFiles.Add(outFile)
            End If
        End If

        If p.ExtractHdrmetadata = HdrmetadataMode.DolbyVision OrElse p.ExtractHdrmetadata = HdrmetadataMode.All Then
            Dim el = Streams?.Where(Function(x) x.IsVideoEnhancementLayer)?.FirstOrDefault()
            If el IsNot Nothing Then
                Dim outFile = Path.Combine(OutputFolder, baseName + "_EL.h265")
                If Not outFile.FileExists() Then
                    ret += " " & el.ID & ": " + WineHelp.ToWinePath(outFile).Escape
                    outFiles.Add(outFile)
                End If
            End If
        End If

        For Each stream In Streams
            If stream.IsAudio AndAlso stream.Checked Then
                Dim outFile = Path.Combine(OutputFolder, baseName + " ID" & stream.TypeID)

                If stream.Language.CultureInfo.TwoLetterISOLanguageName <> "iv" Then
                    outFile += " " + stream.Language.CultureInfo.EnglishName
                End If

                outFile += "." + stream.OutputType
                outFiles.Add(outFile)
                ret += " " & stream.ID & ": " + WineHelp.ToWinePath(outFile).Escape

                If stream.Options <> "" Then
                    ret += " " + stream.Options.Trim
                End If
            End If
        Next

        For Each stream In Streams
            If stream.IsSubtitle AndAlso stream.Checked Then
                Dim outFile = Path.Combine(OutputFolder, baseName + " ID" & stream.TypeID)

                If stream.Language.CultureInfo.TwoLetterISOLanguageName <> "iv" Then
                    outFile += " " + stream.Language.CultureInfo.EnglishName
                End If

                outFile += ".sup"
                outFiles.Add(outFile)
                ret += " " & stream.ID & ": " + WineHelp.ToWinePath(outFile).Escape
            End If

            If stream.IsChapters AndAlso cbChapters.Checked Then
                Dim outFile = Path.Combine(OutputFolder, baseName + "_chapters.txt")
                outFiles.Add(outFile)
                ret += " " & stream.ID & ": " + WineHelp.ToWinePath(outFile).Escape
            End If
        Next

        Return ret + " -progressnumbers"
    End Function

    Sub bnBrowse_Click() Handles bnBrowse.Click
        Using dialog As New FolderBrowserDialog
            dialog.SetSelectedPath(teTempDir.Text)

            If dialog.ShowDialog = DialogResult.OK Then
                teTempDir.Text = dialog.SelectedPath
            End If
        End Using
    End Sub

    Sub lvSubtitles_ItemChecked(sender As Object, e As ItemCheckedEventArgs) Handles lvSubtitles.ItemChecked
        DirectCast(e.Item.Tag, M2TSStream).Checked = e.Item.Checked
    End Sub

    Sub cbVideoStream_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbVideoStream.SelectedIndexChanged
        cbVideoOutput.Items.Clear()
        cbVideoOutput.Items.Add("Nothing")
        cbVideoOutput.Enabled = True

        Dim stream = TryCast(cbVideoStream.SelectedItem, M2TSStream)

        If stream Is Nothing Then
            Exit Sub
        End If

        Select Case stream.Codec
            Case "h264/AVC"
                cbVideoOutput.Items.Add("H264")
                cbVideoOutput.Items.Add("MKV")
                cbVideoOutput.Text = If(M2TSFile = "", "H264", "Nothing")
            Case "h265/HEVC"
                cbVideoOutput.Items.Add("H265")
                cbVideoOutput.Text = If(M2TSFile = "", "H265", "Nothing")
            Case "VC-1"
                cbVideoOutput.Items.Add("MKV")
                cbVideoOutput.Items.Add("VC1")
                cbVideoOutput.Text = If(M2TSFile = "", "VC1", "Nothing")
            Case "MPEG2"
                cbVideoOutput.Items.Add("M2V")
                cbVideoOutput.Items.Add("MKV")
                cbVideoOutput.Text = If(M2TSFile = "", "M2V", "Nothing")
        End Select
    End Sub

    Sub cbAudioOutput_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbAudioOutput.SelectedIndexChanged
        If Not cbAudioOutput.SelectedItem Is Nothing AndAlso Not GetSelectedStream() Is Nothing Then
            Dim ms = GetSelectedStream()
            ms.OutputType = cbAudioOutput.SelectedItem.ToString
            ms.ListViewItem.Text = ms.ToString

            If ms.OutputType = "dts" AndAlso DtsHdCodecs.Contains(ms.Codec) AndAlso Not ms.Options.Contains("-core") Then
                If ms.Options = "" Then
                    ms.Options = "-core"
                End If

                ms.UpdateListViewItem()
                cmdlOptions.tb.Text = ms.Options
            ElseIf {"dtsma", "dtshr"}.Contains(ms.OutputType) AndAlso ms.Options?.Contains("-core") Then
                ms.Options = ms.Options.Replace(" -core ", "").Replace(" -core", "").Replace("-core ", "").Replace("-core", "")
                ms.UpdateListViewItem()
                cmdlOptions.tb.Text = ms.Options
            ElseIf ms.OutputType <> "dts" AndAlso ms.Options?.Contains("-core") Then
                ms.Options = ms.Options.Replace(" -core ", "").Replace(" -core", "").Replace("-core ", "").Replace("-core", "")
                ms.UpdateListViewItem()
                cmdlOptions.tb.Text = ms.Options
            End If
        End If
    End Sub

    Sub cmdlOptions_ValueChanged(value As String) Handles cmdlOptions.ValueChanged
        If cmdlOptions.tb.Focused OrElse cmdlOptions.bn.HasFocus Then
            Dim ms = GetSelectedStream()

            If Not ms Is Nothing Then
                ms.Options = value

                If ms.Options <> "" Then
                    ms.ListViewItem.Checked = True
                End If

                If ms.Options = "-core" AndAlso ms.Codec.StartsWith("DTS") Then
                    cbAudioOutput.SelectedItem = "dts"
                End If

                If ms.Options.Contains("-quality=") Then
                    cbAudioOutput.SelectedItem = "m4a"
                End If

                lvAudio.SelectedItems(0).Text = ms.ToString
            End If
        End If
    End Sub

    Sub lvAudio_ItemChecked(sender As Object, e As ItemCheckedEventArgs) Handles lvAudio.ItemChecked
        DirectCast(e.Item.Tag, M2TSStream).Checked = e.Item.Checked
    End Sub

    Function GetSelectedStream() As M2TSStream
        If lvAudio.SelectedItems.Count > 0 Then
            Return DirectCast(lvAudio.SelectedItems(0).Tag, M2TSStream)
        End If
    End Function

    Sub lvAudio_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lvAudio.SelectedIndexChanged
        Dim ms = GetSelectedStream()

        If Not ms Is Nothing Then
            cmdlOptions.tb.Text = ms.Options
            cbAudioOutput.SelectedItem = ms.OutputType
        End If
    End Sub

    Sub bnAudioAll_Click(sender As Object, e As EventArgs) Handles bnAudioAll.Click
        For Each item As ListViewItem In lvAudio.Items
            item.Checked = True
        Next
    End Sub

    Sub bnAudioNone_Click(sender As Object, e As EventArgs) Handles bnAudioNone.Click
        For Each item As ListViewItem In lvAudio.Items
            item.Checked = False
        Next
    End Sub

    Sub bnAudioEnglish_Click(sender As Object, e As EventArgs) Handles bnAudioEnglish.Click
        For Each item As ListViewItem In lvAudio.Items
            Dim stream = DirectCast(item.Tag, M2TSStream)

            If stream.Language.TwoLetterCode = "en" Then
                item.Checked = True
            End If
        Next
    End Sub

    Sub bnAudioNative_Click(sender As Object, e As EventArgs) Handles bnAudioNative.Click
        For Each item As ListViewItem In lvAudio.Items
            Dim stream = DirectCast(item.Tag, M2TSStream)

            If stream.Language.TwoLetterCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName Then
                item.Checked = True
            End If
        Next
    End Sub

    Sub bnSubtitleAll_Click(sender As Object, e As EventArgs) Handles bnSubtitleAll.Click
        For Each item As ListViewItem In lvSubtitles.Items
            item.Checked = True
        Next
    End Sub

    Sub bnSubtitleNone_Click(sender As Object, e As EventArgs) Handles bnSubtitleNone.Click
        For Each item As ListViewItem In lvSubtitles.Items
            item.Checked = False
        Next
    End Sub

    Sub bnSubtitleEnglish_Click(sender As Object, e As EventArgs) Handles bnSubtitleEnglish.Click
        For Each item As ListViewItem In lvSubtitles.Items
            Dim stream = DirectCast(item.Tag, M2TSStream)

            If stream.Language.TwoLetterCode = "en" Then
                item.Checked = True
            End If
        Next
    End Sub

    Sub bnSubtitleNative_Click(sender As Object, e As EventArgs) Handles bnSubtitleNative.Click
        For Each item As ListViewItem In lvSubtitles.Items
            Dim stream = DirectCast(item.Tag, M2TSStream)

            If stream.Language.TwoLetterCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName Then
                item.Checked = True
            End If
        Next
    End Sub

    Sub teTempDir_TextChanged() Handles teTempDir.TextChanged
        OutputFolder = teTempDir.Text.FixDir
    End Sub

    Protected Overrides Sub OnClosing(args As ComponentModel.CancelEventArgs)
        Dim hdCounter As Integer

        For Each str In Streams
            If str.Checked AndAlso str.IsAudio Then
                If {"dtsma", "dtshr"}.Contains(str.OutputType) Then
                    hdCounter += 1
                ElseIf str.OutputType = "dts" AndAlso DtsHdCodecs.Contains(str.Codec) Then
                    hdCounter -= 1
                End If
            End If
        Next

        s.CommandLinePresets(CommandLinePresetType.Eac3to) = cmdlOptions.Presets

        If Not bnOK.Enabled Then
            args.Cancel = True
        End If

        If DialogResult = DialogResult.OK Then
            If cbVideoOutput.Text = "MKV" AndAlso Not Package.Haali.VerifyOK(True) Then
                args.Cancel = True
            End If

            s.Storage.SetBool("demux Blu-ray chapters", cbChapters.Checked)
        End If

        MyBase.OnClosing(args)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        Task.Run(AddressOf StartAnalyze)
    End Sub
End Class
