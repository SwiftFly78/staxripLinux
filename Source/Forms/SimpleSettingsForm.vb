
Imports StaxRip.UI

Public Class SimpleSettingsForm
    Inherits DialogBase

    Public WithEvents bnOK As ButtonEx
    Public WithEvents bnCancel As ButtonEx
    Public WithEvents SimpleUI As SimpleUI
    Public LineControl As LineControl

    Private HelpParagraphs As String()

    Sub New(title As String, ParamArray helpParagraphs As String())
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        SimpleUI = New SimpleUI
        LineControl = New LineControl

        DefaultButton = bnOK
        AbortButton = bnCancel

        Content = PaddedTable(
            ScaledRow(SimpleUI),
            Row(LineControl),
            Row(ButtonRow(bnOK, bnCancel)))

        SetMinimumSize(33, 25)
        ScaleClientSize(40, 32.75)
        Text = $"{title} - {g.DefaultCommands.GetApplicationDetails()}"
        Me.HelpParagraphs = helpParagraphs
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)

        If SimpleUI.Tree.Visible Then
            SimpleUI.Tree.Focus()
        End If
    End Sub

    Sub SimpleSettingsForm_HelpRequested(sender As Object, e As EventArgs) Handles Me.HelpRequested
        Dim form As New HelpForm()
        form.Doc.WriteStart(Text)

        If Not HelpParagraphs Is Nothing Then
            For Each i As String In HelpParagraphs
                form.Doc.WriteParagraph(i)
            Next
        End If

        If SimpleUI.ActivePage?.TipProvider IsNot Nothing Then
            form.Doc.WriteTips(SimpleUI.ActivePage.TipProvider.GetTips)
        End If

        form.Show()
    End Sub
End Class
