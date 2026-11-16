Imports StaxRip.UI

'the languages of the text of the bitmap subtitles to convert to SRT
Public Class OcrSubtitlesForm
    Inherits DialogBase

    Public dgvSubtitles As DataGridViewEx
    Public laInfo As LabelEx
    Public bnOK, bnCancel As ButtonEx

    Private ReadOnly Items As New List(Of SubtitleLanguage)
    Private ReadOnly LanguageNames As New Dictionary(Of String, String)

    Sub New(subtitles As IEnumerable(Of (Language As Language, Name As String)))
        Dim installed = SubtitleOcr.GetInstalledLanguages()

        For Each i In installed
            LanguageNames(SubtitleOcr.GetLanguageName(i)) = i
        Next

        For Each i In SubtitleOcr.Languages.Keys.Except(installed).OrderBy(Function(x) SubtitleOcr.Languages(x))
            LanguageNames(SubtitleOcr.GetLanguageName(i) + " - download") = i
        Next

        For Each i In subtitles
            Dim language = SubtitleOcr.GetDefaultLanguage(i.Language)
            Items.Add(New SubtitleLanguage With {.Subtitle = i.Name, .Language = LanguageNames.First(Function(x) x.Value = language).Key})
        Next

        laInfo = New LabelEx With {
            .Wrap = WrapMode.Word,
            .Text = "The language of the text of the subtitles. Languages that aren't installed are downloaded once from " +
                "the tessdata_fast repository of Tesseract on GitHub."}

        dgvSubtitles = New DataGridViewEx
        dgvSubtitles.AddTextBoxColumn("Subtitle", "Subtitle").Expand = True
        dgvSubtitles.AddComboBoxColumn("Language", "Language", LanguageNames.Keys)
        dgvSubtitles.DataStore = Items

        bnOK = CreateButton("Convert", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        Content = DialogContent(ButtonRow(bnOK, bnCancel), Row(laInfo), ScaledRow(dgvSubtitles))
        DefaultButton = bnOK
        AbortButton = bnCancel
        Text = $"OCR to SRT - {g.DefaultCommands.GetApplicationDetails()}"
        ScaleClientSize(36, 18)
    End Sub

    ReadOnly Property SelectedLanguages As List(Of String)
        Get
            Return Items.Select(Function(i) LanguageNames(i.Language)).ToList
        End Get
    End Property

    Public Class SubtitleLanguage
        Property Subtitle As String
        Property Language As String
    End Class
End Class
