
Imports StaxRip.UI

'dialog with an editable grid, rows are added and removed with buttons
Public Class DataForm
    Inherits DialogBase

    Property HelpAction As Action
    Property NewItemFunc As Func(Of Object)

    Public Grid As GridView
    Public WithEvents bnOK As ButtonEx
    Public WithEvents bnCancel As ButtonEx
    Public WithEvents bnAdd As ButtonEx
    Public WithEvents bnRemove As ButtonEx

    Sub New()
        Grid = New GridView With {.AllowMultipleSelection = False, .GridLines = GridLines.Both}
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        bnAdd = New ButtonEx("Add")
        bnRemove = New ButtonEx("Remove")
        AbortButton = bnCancel
        Resizable = True

        Content = DialogContent(SplitRow({bnAdd, bnRemove}, {bnOK, bnCancel}), ScaledRow(Grid))

        ScaleClientSize(40, 22)
    End Sub

    Sub bnAdd_Click(sender As Object, e As EventArgs) Handles bnAdd.Click
        Dim list = TryCast(Grid.DataStore, IList)

        If list IsNot Nothing AndAlso NewItemFunc IsNot Nothing Then
            list.Add(NewItemFunc())
        End If
    End Sub

    Sub bnRemove_Click(sender As Object, e As EventArgs) Handles bnRemove.Click
        Dim list = TryCast(Grid.DataStore, IList)

        If list IsNot Nothing AndAlso Grid.SelectedItem IsNot Nothing Then
            list.Remove(Grid.SelectedItem)
        End If
    End Sub

    Sub DataForm_HelpRequested(sender As Object, e As EventArgs) Handles Me.HelpRequested
        HelpAction?.Invoke
    End Sub
End Class
