
Namespace UI
    Public Class StringEditorForm
        Inherits DialogBase

        Public WithEvents rtb As RichTextBoxEx
        Public WithEvents bnOK As ButtonEx
        Public WithEvents bnCancel As ButtonEx
        Public WithEvents cbWrap As CheckBoxEx

        Sub New()
            MyBase.New()
            rtb = New RichTextBoxEx With {.AcceptsTab = True, .Font = FontManager.GetCodeFont()}
            bnOK = CreateButton("OK", DialogResult.OK)
            bnCancel = CreateButton("Cancel", DialogResult.Cancel)
            cbWrap = New CheckBoxEx With {.Text = "Wrap"}
            AbortButton = bnCancel
            Text = $"String Editor - {g.DefaultCommands.GetApplicationDetails()}"

            Content = PaddedTable(
                ScaledRow(rtb),
                Row(SplitRow({cbWrap}, {bnOK, bnCancel})))

            cbWrap.Checked = True
            rtb.WordWrap = True
            ScaleClientSize(41, 24)
        End Sub

        Sub tb_KeyDown(sender As Object, e As KeyEventArgs) Handles rtb.KeyDown
            If e.KeyData = (Keys.Enter Or Keys.Control) Then
                e.Handled = True
                bnOK.PerformClick()
            End If
        End Sub

        Sub cbWrap_CheckedChanged(sender As Object, e As EventArgs) Handles cbWrap.CheckedChanged
            rtb.WordWrap = cbWrap.Checked
        End Sub
    End Class
End Namespace
