
Imports StaxRip.UI

Public Class CommandLineVideoEncoderForm
    Inherits DialogBase

    Public WithEvents EncodingControl As MacroEditorControl
    Public WithEvents CompCheckControl As MacroEditorControl
    Public WithEvents cbQualityMode As CheckBoxEx
    Public WithEvents numPercent As NumEdit
    Public WithEvents bnCancel As ButtonEx
    Public WithEvents bnOK As ButtonEx
    Public WithEvents tbType As TextEdit
    Public laType As LabelEx
    Public laPercent As LabelEx
    Public TipProvider As TipProvider

    Private ReadOnly EditorPanel As New Panel
    Private Encoder As BatchEncoder

    Sub New(encoder As BatchEncoder)
        MyBase.New()
        laType = New LabelEx With {.Text = "Output File Type:"}
        tbType = New TextEdit With {.Width = FH(5), .TextAlign = HorizontalAlignment.Center}
        cbQualityMode = New CheckBoxEx With {.Text = "Quality Mode"}
        laPercent = New LabelEx With {.Text = "Aimed Quality (%):"}
        numPercent = New NumEdit With {.Increment = 5, .MaxValue = 200}
        EncodingControl = New MacroEditorControl With {.Text = "Command Line"}
        CompCheckControl = New MacroEditorControl With {.Text = "Compressibility Check"}
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        AbortButton = bnCancel
        TipProvider = New TipProvider

        Content = DialogContent(ButtonRow(bnOK, bnCancel),
            Row(HRow(laType, tbType, cbQualityMode, laPercent, numPercent)),
            ScaledRow(EditorPanel))

        Text = $"Command Line Video Encoder - {g.DefaultCommands.GetApplicationDetails()}"
        TipProvider.SetTipText(cbQualityMode, "In Quality Mode bitrate related features are disabled.")
        RestoreClientSize(50, 22)
        Me.Encoder = encoder

        cbQualityMode.Checked = encoder.QualityMode
        tbType.Text = encoder.OutputExt

        EncodingControl.SetCommandLineDefaults()
        EncodingControl.Value = encoder.CommandLines

        CompCheckControl.SetCommandLineDefaults()
        CompCheckControl.Value = encoder.CompCheckCommandLines

        numPercent.Value = encoder.AutoCompCheckValue

        TipProvider.SetTip("Adjusts the target file size or image size after the compressibility check accordingly.", numPercent, laPercent)
        UpdateEditorLayout()
    End Sub

    Sub UpdateEditorLayout()
        If cbQualityMode.Checked Then
            EditorPanel.Content = EncodingControl
        Else
            EditorPanel.Content = New TableLayout(ScaledRow(EncodingControl), ScaledRow(CompCheckControl)) With {.Spacing = DefaultSpacing}
        End If

        laPercent.Visible = Not cbQualityMode.Checked
        numPercent.Visible = Not cbQualityMode.Checked
    End Sub

    Protected Overrides Sub OnClosed(e As EventArgs)
        If DialogResult = DialogResult.OK Then
            Encoder.OutputFileTypeValue = tbType.Text
            Encoder.CommandLines = EncodingControl.Value
            Encoder.CompCheckCommandLines = CompCheckControl.Value
            Encoder.QualityMode = cbQualityMode.Checked
            Encoder.AutoCompCheckValue = CInt(numPercent.Value)
        End If

        MyBase.OnClosed(e)
    End Sub

    Sub cbQualityMode_CheckedChanged(sender As Object, e As EventArgs) Handles cbQualityMode.CheckedChanged
        UpdateEditorLayout()
    End Sub
End Class
