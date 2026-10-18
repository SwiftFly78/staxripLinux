
Imports StaxRip.UI

Public Class MacroEditorDialog
    Inherits DialogBase

    Public WithEvents MacroEditorControl As MacroEditorControl
    Public WithEvents bnContext As ButtonEx
    Public WithEvents bnCancel As ButtonEx
    Public WithEvents bnOK As ButtonEx

    Private HelpText As String

    Sub New()
        MyBase.New()
        MacroEditorControl = New MacroEditorControl
        bnContext = New ButtonEx With {.Visible = False}
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        AbortButton = bnCancel
        Resizable = True

        Content = PaddedTable(
            ScaledRow(MacroEditorControl),
            Row(SplitRow({bnContext}, {bnOK, bnCancel})))

        SetMacroDefaults()
    End Sub

    Sub SetBatchDefaults()
        Text = $"Batch Editor - {g.DefaultCommands.GetApplicationDetails()}"
        HelpText = "Editor for batch script editing."
        MacroEditorControl.SetCommandLineDefaults()
        ScaleClientSize(45, 30)
    End Sub

    Sub SetMacroDefaults()
        Text = $"Macro Editor - {g.DefaultCommands.GetApplicationDetails()}"
        HelpText = "Editor for strings that may contain macros."
        MacroEditorControl.SetMacroDefaults()
        ScaleClientSize(45, 30)
    End Sub

    Sub SetScriptDefaults()
        Text = $"Script Editor - {g.DefaultCommands.GetApplicationDetails()}"
        HelpText = "Editor for scripts that may contain macros."
        MacroEditorControl.SetScriptDefaults()
        ScaleClientSize(45, 30)
    End Sub

    Sub UniversalEditor_HelpRequested(sender As Object, e As EventArgs) Handles Me.HelpRequested
        Dim form As New HelpForm()
        form.Doc.WriteStart(Text)
        form.Doc.WriteParagraph(HelpText)
        form.Doc.WriteTable("Macros", Macro.GetTips(True, False, True, True, False))
        form.Show()
    End Sub
End Class
