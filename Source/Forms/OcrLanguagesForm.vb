Imports StaxRip.UI

'selection of the OCR languages to download
Public Class OcrLanguagesForm
    Inherits DialogBase

    Public lvLanguages As ListViewEx
    Public laInstalled, laSource As LabelEx
    Public bnOK, bnCancel As ButtonEx

    Sub New()
        Dim installed = SubtitleOcr.GetInstalledLanguages()

        laInstalled = New LabelEx With {
            .Wrap = WrapMode.Word,
            .Text = "Installed: " + If(installed.Count > 0, installed.Select(AddressOf SubtitleOcr.GetLanguageName).Join(", "), "none")}

        laSource = New LabelEx With {
            .Wrap = WrapMode.Word,
            .Text = "The checked languages are downloaded from the tessdata_fast repository of Tesseract on GitHub to " + SubtitleOcr.DataDir}

        lvLanguages = New ListViewEx With {
            .View = View.Details,
            .CheckBoxes = True,
            .HeaderStyle = ColumnHeaderStyle.None,
            .FullRowSelect = True,
            .MultiSelect = False,
            .AutoCheckMode = AutoCheckMode.SingleClick}

        lvLanguages.Columns.Add("")

        For Each i In SubtitleOcr.Languages.Keys.Except(installed).OrderBy(Function(x) SubtitleOcr.Languages(x))
            lvLanguages.Items.Add(SubtitleOcr.GetLanguageName(i)).Tag = i
        Next

        bnOK = CreateButton("Download", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        Content = PaddedTable(Row(laInstalled), ScaledRow(lvLanguages), Row(laSource), Row(ButtonRow(bnOK, bnCancel)))
        DefaultButton = bnOK
        AbortButton = bnCancel
        Text = $"OCR Languages - {g.DefaultCommands.GetApplicationDetails()}"
        ScaleClientSize(32, 30)
    End Sub

    ReadOnly Property SelectedLanguages As List(Of String)
        Get
            Return lvLanguages.Items.OfType(Of ListViewItem).Where(Function(i) i.Checked).Select(Function(i) CStr(i.Tag)).ToList
        End Get
    End Property
End Class
