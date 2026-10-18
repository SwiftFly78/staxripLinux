
Imports System.Text.RegularExpressions

Imports StaxRip.UI

Public Class LogForm
    Inherits FormBase

    Public WithEvents lb As ListBoxEx
    Public rtb As RichTextBoxEx

    Sub New()
        lb = New ListBoxEx With {.Width = FH(12)}
        rtb = New RichTextBoxEx With {.ReadOnly = True, .Font = FontManager.GetCodeFont()}

        Content = New TableLayout(New TableRow(New TableCell(lb), New TableCell(rtb, True)) With {.ScaleHeight = True}) With {
            .Spacing = DefaultSpacing,
            .Padding = New Padding(FH(0.3))}

        RestoreClientSize(50, 35)
        Text = $"Log File Viewer - {g.DefaultCommands.GetApplicationDetails()} - {p.Log.GetPath}"
        rtb.Text = p.Log.GetPath.ReadAllText

        For Each match As Match In Regex.Matches(rtb.Text, "^-+ (.+) -+", RegexOptions.Multiline)
            Dim val = match.Groups(1).Value
            Dim match2 = Regex.Match(val, " \d+\.+.+")

            If match2.Success Then
                val = val.Substring(0, val.Length - match2.Value.Length)
            End If

            lb.Items.Add(val)
        Next

        Dim cms = rtb.ContextMenuStrip
        cms.Form = Me

        cms.Add("-")
        cms.Add("Save As...", Sub()
                                  Using dialog As New SaveFileDialog
                                      dialog.Title = "Save As..."
                                      dialog.FileName = p.Log.GetPath.FileName
                                      dialog.Filter = FileTypes.GetFilter(FileTypes.Logs)

                                      If dialog.ShowDialog = DialogResult.OK Then
                                          rtb.Text.FixBreak.WriteFileUTF8(dialog.FileName)
                                      End If
                                  End Using
                              End Sub, Keys.Control Or Keys.S)
        cms.Add("Save Obfuscated As...", Sub()
                                             Using dialog As New SaveFileDialog
                                                 dialog.Title = "Save Obfuscated As..."
                                                 dialog.FileName = "Source" & p.Log.GetPath.ExtFull
                                                 dialog.Filter = FileTypes.GetFilter(FileTypes.Logs)

                                                 If dialog.ShowDialog = DialogResult.OK Then
                                                     g.ObfuscateLogFile(rtb.Text.FixBreak, p).WriteFileUTF8(dialog.FileName)
                                                 End If
                                             End Using
                                         End Sub, Keys.Control Or Keys.Alt Or Keys.S)
        cms.Add("Open in Text Editor", Sub() g.ShellExecute(g.GetTextEditorPath, p.Log.GetPath.Escape), Keys.Control Or Keys.T)
        cms.Add("Show in File Manager", Sub() g.SelectFileWithExplorer(p.Log.GetPath), Keys.Control Or Keys.E)
        cms.Add("Show History", Sub() g.ShellExecute(Path.Combine(Folder.Settings, "Log Files")), Keys.Control Or Keys.H)
    End Sub

    Sub lb_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lb.SelectedIndexChanged
        If lb.SelectedItem IsNot Nothing Then
            rtb.Find("- " + lb.SelectedItem.ToString + " -")
        End If
    End Sub
End Class
