
Namespace UI
    Public Class MenuTemplateForm
        Inherits DialogBase

        Public WithEvents tv As TreeViewEx
        Public WithEvents bnCancel As ButtonEx
        Public WithEvents bnOK As ButtonEx

        Public TreeNode As TreeNode

        Sub New(item As CustomMenuItem)
            tv = New TreeViewEx
            'GTK selects no item initially, so OK is enabled for selected items only
            bnOK = CreateButton("OK", DialogResult.OK)
            bnOK.Enabled = False
            bnCancel = CreateButton("Cancel", DialogResult.Cancel)
            DefaultButton = bnOK
            AbortButton = bnCancel
            Text = $"Default Menu - {g.DefaultCommands.GetApplicationDetails()}"

            Content = DialogContent(ButtonRow(bnOK, bnCancel), ScaledRow(tv))

            ScaleClientSize(28, 32)
            PopulateTreeView(item, Nothing)
            tv.ExpandAll()
        End Sub

        Sub PopulateTreeView(item As CustomMenuItem, node As TreeNode)
            Dim newNode As New TreeNode(item.Text)
            newNode.Tag = item

            If node Is Nothing Then
                tv.Nodes.Add(newNode)
            Else
                node.Nodes.Add(newNode)
            End If

            For Each i As CustomMenuItem In item.SubItems
                PopulateTreeView(i, newNode)
            Next
        End Sub

        Sub tv_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles tv.AfterSelect
            If Not tv.SelectedNode Is Nothing Then
                TreeNode = tv.SelectedNode
                bnOK.Enabled = Not TreeNode.Parent Is Nothing
            End If
        End Sub

        Sub MenuTemplateForm_HelpRequested(sender As Object, e As EventArgs) Handles Me.HelpRequested
            Dim form As New HelpForm()
            form.Doc.WriteStart(Text)
            form.Doc.WriteParagraph("The new item will be a clone of the selected item.")
            form.Show()
        End Sub
    End Class
End Namespace
