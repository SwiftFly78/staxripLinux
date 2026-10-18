
Namespace UI
    'grid bound to an IList data store with helpers to move and remove rows
    Public Class DataGridViewEx
        Inherits GridView

        Sub New()
            AllowMultipleSelection = False
            RowHeight = CInt(FontHeight * 1.6)
            GridLines = GridLines.Both
        End Sub

        ReadOnly Property List As IList
            Get
                Return TryCast(DataStore, IList)
            End Get
        End Property

        ReadOnly Property SelectedIndex As Integer
            Get
                Return SelectedRow
            End Get
        End Property

        ReadOnly Property HasSelection As Boolean
            Get
                Return SelectedRow > -1 AndAlso List IsNot Nothing AndAlso SelectedRow < List.Count
            End Get
        End Property

        Function CanMoveUp() As Boolean
            Return HasSelection AndAlso SelectedRow > 0
        End Function

        Function CanMoveDown() As Boolean
            Return HasSelection AndAlso SelectedRow < List.Count - 1
        End Function

        Sub MoveSelectionUp()
            MoveSelection(-1)
        End Sub

        Sub MoveSelectionDown()
            MoveSelection(1)
        End Sub

        Private Sub MoveSelection(offset As Integer)
            If Not HasSelection Then
                Exit Sub
            End If

            Dim index = SelectedRow
            Dim newIndex = index + offset

            If newIndex < 0 OrElse newIndex >= List.Count Then
                Exit Sub
            End If

            Dim item = List(index)
            List.RemoveAt(index)
            List.Insert(newIndex, item)
            RefreshData()
            SelectRow(newIndex)
        End Sub

        Sub RemoveSelection()
            If Not HasSelection Then
                Exit Sub
            End If

            Dim index = SelectedRow
            List.RemoveAt(index)
            RefreshData()

            If List.Count > 0 Then
                SelectRow(Math.Min(index, List.Count - 1))
            End If
        End Sub

        'GTK measures rows added before loading too small, reloading measures them again
        Protected Overrides Sub OnLoadComplete(e As EventArgs)
            MyBase.OnLoadComplete(e)

            If List IsNot Nothing AndAlso List.Count > 0 Then
                RefreshData()
            End If
        End Sub

        'reloads the rows, needed for lists without change notification
        Sub RefreshData()
            Dim store = DataStore
            Dim index = SelectedRow
            DataStore = Nothing
            DataStore = store

            If index > -1 AndAlso List IsNot Nothing AndAlso index < List.Count Then
                SelectRow(index)
            End If
        End Sub

        Function AddTextBoxColumn(header As String, propertyName As String, Optional editable As Boolean = False) As GridColumn
            Dim ret As New GridColumn With {
                .HeaderText = header,
                .Editable = editable,
                .DataCell = New TextBoxCell(propertyName)}

            Columns.Add(ret)
            Return ret
        End Function

        Function AddCheckBoxColumn(header As String, propertyName As String) As GridColumn
            Dim ret As New GridColumn With {
                .HeaderText = header,
                .Editable = True,
                .DataCell = New CheckBoxCell(propertyName)}

            Columns.Add(ret)
            Return ret
        End Function

        Function AddComboBoxColumn(header As String, propertyName As String, items As IEnumerable(Of String)) As GridColumn
            Dim ret As New GridColumn With {
                .HeaderText = header,
                .Editable = True,
                .DataCell = New ComboBoxCell(propertyName) With {.DataStore = items.ToList}}

            Columns.Add(ret)
            Return ret
        End Function
    End Class
End Namespace
