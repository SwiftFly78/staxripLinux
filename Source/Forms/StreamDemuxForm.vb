
Imports System.Globalization

Imports StaxRip.UI

Public Class StreamDemuxForm
    Inherits DialogBase

    Public WithEvents lvAudio, lvSubtitles, lvAttachments As ListViewEx
    Public WithEvents bnAudioAll, bnAudioNone, bnAudioEnglish, bnAudioNative As ButtonEx
    Public WithEvents bnSubtitleAll, bnSubtitleNone, bnSubtitleEnglish, bnSubtitleNative As ButtonEx
    Public WithEvents bnAllAttachments, bnNoneAttachments As ButtonEx
    Public WithEvents cbDemuxVideo, cbDemuxChapters As CheckBoxEx
    Public WithEvents bnOK, bnCancel As ButtonEx

    Private Sub InitializeComponent()
        lvAudio = New ListViewEx
        lvSubtitles = New ListViewEx
        lvAttachments = New ListViewEx
        bnAudioAll = New ButtonEx("All")
        bnAudioNone = New ButtonEx("None")
        bnAudioEnglish = New ButtonEx("English")
        bnAudioNative = New ButtonEx("Native")
        bnSubtitleAll = New ButtonEx("All")
        bnSubtitleNone = New ButtonEx("None")
        bnSubtitleEnglish = New ButtonEx("English")
        bnSubtitleNative = New ButtonEx("Native")
        bnAllAttachments = New ButtonEx("All")
        bnNoneAttachments = New ButtonEx("None")
        cbDemuxVideo = New CheckBoxEx With {.Text = "Demux Video"}
        cbDemuxChapters = New CheckBoxEx With {.Text = "Demux Chapters"}
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)

        Dim group = Function(title As String, lv As ListViewEx, buttons As Control()) As GroupBox
                        Return New GroupBox With {
                            .Text = title,
                            .Content = New TableLayout(ScaledRow(lv), Row(HRow(buttons))) With {.Spacing = DefaultSpacing}}
                    End Function

        Content = PaddedTable(
            ScaledRow(group("Audio", lvAudio, {bnAudioAll, bnAudioNone, bnAudioEnglish, bnAudioNative})),
            ScaledRow(group("Subtitles", lvSubtitles, {bnSubtitleAll, bnSubtitleNone, bnSubtitleEnglish, bnSubtitleNative})),
            ScaledRow(group("Attachments", lvAttachments, {bnAllAttachments, bnNoneAttachments})),
            Row(SplitRow({cbDemuxVideo, cbDemuxChapters}, {bnOK, bnCancel})))

        DefaultButton = bnOK
        AbortButton = bnCancel
        Text = $"Demux - {g.DefaultCommands.GetApplicationDetails()}"
    End Sub

    Private _audioStreams As New List(Of AudioStream)
    Private _subtitles As New List(Of Subtitle)

    ReadOnly Property AudioStreams As List(Of AudioStream)
        Get
            Return lvAudio.Items.OfType(Of ListViewItem).Where(Function(x) x.Checked).Select(Function(x) DirectCast(x.Tag, AudioStream)).ToList()
        End Get
    End Property

    ReadOnly Property Subtitles As List(Of Subtitle)
        Get
            Return lvSubtitles.Items.OfType(Of ListViewItem).Where(Function(x) x.Checked).Select(Function(x) DirectCast(x.Tag, Subtitle)).ToList()
        End Get
    End Property

    Sub New(sourceFile As String, attachments As List(Of Attachment))
        InitializeComponent()
        ScaleClientSize(42, 30)

        For Each lv In {lvAudio, lvSubtitles, lvAttachments}
            lv.View = View.Details
            lv.Columns.Add("")
            lv.CheckBoxes = True
            lv.HeaderStyle = ColumnHeaderStyle.None
            lv.FullRowSelect = True
            lv.MultiSelect = False
            lv.AutoCheckMode = AutoCheckMode.SingleClick
        Next

        _audioStreams = MediaInfo.GetAudioStreams(sourceFile)
        _subtitles = MediaInfo.GetSubtitles(sourceFile)

        bnAudioEnglish.Enabled = _audioStreams.Where(Function(stream) stream.Language.TwoLetterCode = "en").Count > 0
        bnAudioNative.Visible = CultureInfo.CurrentCulture.TwoLetterISOLanguageName <> "en"
        bnAudioNative.Text = CultureInfo.CurrentCulture.NeutralCulture.EnglishName
        bnAudioNative.Enabled = _audioStreams.Where(Function(stream) stream.Language.TwoLetterCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName).Count > 0

        bnSubtitleEnglish.Enabled = _subtitles.Where(Function(stream) stream.Language.TwoLetterCode = "en").Count > 0
        bnSubtitleNative.Visible = CultureInfo.CurrentCulture.TwoLetterISOLanguageName <> "en"
        bnSubtitleNative.Text = CultureInfo.CurrentCulture.NeutralCulture.EnglishName
        bnSubtitleNative.Enabled = _subtitles.Where(Function(stream) stream.Language.TwoLetterCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName).Count > 0

        For Each audioStream In _audioStreams
            Dim item = lvAudio.Items.Add(audioStream.Name)
            item.Tag = audioStream
            item.Checked = audioStream.Enabled
        Next

        For Each subtitle In _subtitles
            Dim text = subtitle.Language.ToString + " (" + subtitle.TypeName + ")" + If(subtitle.Title <> "", " - " + subtitle.Title, "")
            Dim item = lvSubtitles.Items.Add(text)
            item.Tag = subtitle
            item.Checked = subtitle.Enabled
        Next

        If attachments IsNot Nothing Then
            For Each attachment In attachments
                Dim item = lvAttachments.Items.Add(attachment.Name)
                item.Tag = attachment
                item.Checked = attachment.Enabled
            Next
        End If

        cbDemuxChapters.Checked = p.DemuxChapters
        cbDemuxVideo.Checked = p.DemuxVideo
        IsInitialized = True
    End Sub

    Private IsInitialized As Boolean

    Sub lvAudio_ItemChecked(sender As Object, e As ItemCheckedEventArgs) Handles lvAudio.ItemChecked
        If IsInitialized Then
            DirectCast(e.Item.Tag, AudioStream).Enabled = e.Item.Checked
        End If
    End Sub

    Sub lvSubtitles_ItemChecked(sender As Object, e As ItemCheckedEventArgs) Handles lvSubtitles.ItemChecked
        If IsInitialized Then
            DirectCast(e.Item.Tag, Subtitle).Enabled = e.Item.Checked
        End If
    End Sub

    Sub lvAttachments_ItemChecked(sender As Object, e As ItemCheckedEventArgs) Handles lvAttachments.ItemChecked
        If IsInitialized Then
            DirectCast(e.Item.Tag, Attachment).Enabled = e.Item.Checked
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
            Dim stream = DirectCast(item.Tag, AudioStream)

            If stream.Language.TwoLetterCode = "en" Then
                item.Checked = True
            End If
        Next
    End Sub

    Sub bnAudioNative_Click(sender As Object, e As EventArgs) Handles bnAudioNative.Click
        For Each item As ListViewItem In lvAudio.Items
            Dim stream = DirectCast(item.Tag, AudioStream)

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
            Dim stream = DirectCast(item.Tag, Subtitle)

            If stream.Language.TwoLetterCode = "en" Then
                item.Checked = True
            End If
        Next
    End Sub

    Sub bnSubtitleNative_Click(sender As Object, e As EventArgs) Handles bnSubtitleNative.Click
        For Each item As ListViewItem In lvSubtitles.Items
            Dim stream = DirectCast(item.Tag, Subtitle)

            If stream.Language.TwoLetterCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName Then
                item.Checked = True
            End If
        Next
    End Sub

    Sub bnAllAttachments_Click(sender As Object, e As EventArgs) Handles bnAllAttachments.Click
        For Each item As ListViewItem In lvAttachments.Items
            item.Checked = True
        Next
    End Sub

    Sub bnNoneAttachments_Click(sender As Object, e As EventArgs) Handles bnNoneAttachments.Click
        For Each item As ListViewItem In lvAttachments.Items
            item.Checked = False
        Next
    End Sub
End Class
