
Imports System.Collections.ObjectModel

Namespace UI
    Public Enum SelectionMode
        None
        One
        MultiSimple
        MultiExtended
    End Enum

    'list box with a WinForms ListBox like API, based on a one column grid to support multi selection
    Public Class ListBoxEx
        Inherits GridView

        Private SelectionModeValue As SelectionMode = SelectionMode.One
        Private SavedSelection As New List(Of Object)
        Private BlockEvents As Boolean

        Property UpButton As Button
        Property DownButton As Button
        Property RemoveButton As Button
        Property Button1 As Button
        Property Button2 As Button
        Property DisplayMember As String
        Property Sorted As Boolean

        Shadows ReadOnly Property Items As New ObservableCollection(Of Object)

        Event SelectedIndexChanged(sender As Object, e As EventArgs)
        Event DoubleClick(sender As Object, e As EventArgs)

        Protected Overrides Sub OnCellDoubleClick(e As GridCellMouseEventArgs)
            MyBase.OnCellDoubleClick(e)
            RaiseEvent DoubleClick(Me, EventArgs.Empty)
        End Sub

        Sub New()
            MyBase.New()
            ShowHeader = False
            RowHeight = CInt(FontHeight * 1.45)
            GridLines = GridLines.None
            AllowMultipleSelection = False

            Columns.Add(New GridColumn With {
                .DataCell = New TextBoxCell With {.Binding = Binding.Delegate(Of Object, String)(AddressOf GetItemText)},
                .Expand = True,
                .Editable = False,
                .MinWidth = FontHeight
            })

            DataStore = Items

            AddHandler SelectionChanged, Sub()
                                             If Not BlockEvents Then
                                                 UpdateControls()
                                                 RaiseEvent SelectedIndexChanged(Me, EventArgs.Empty)
                                             End If
                                         End Sub
        End Sub

        Function GetItemText(item As Object) As String
            If item Is Nothing Then
                Return ""
            End If

            If DisplayMember <> "" Then
                Try
                    Return item.GetType.GetProperty(DisplayMember).GetValue(item, Nothing).ToString
                Catch
                End Try
            End If

            Return item.ToString
        End Function

        Property SelectionMode As SelectionMode
            Get
                Return SelectionModeValue
            End Get
            Set(value As SelectionMode)
                SelectionModeValue = value
                AllowMultipleSelection = value = SelectionMode.MultiExtended OrElse value = SelectionMode.MultiSimple
            End Set
        End Property

        Property SelectedIndex As Integer
            Get
                Dim rows = SelectedRows.OrderBy(Function(x) x).ToList()
                Return If(rows.Count > 0, rows(0), -1)
            End Get
            Set(value As Integer)
                If value < 0 OrElse value >= Items.Count Then
                    UnselectAll()
                Else
                    SelectRow(value)
                    ScrollToRow(value)
                End If
            End Set
        End Property

        Shadows Property SelectedItem As Object
            Get
                Dim i = SelectedIndex
                Return If(i > -1, Items(i), Nothing)
            End Get
            Set(value As Object)
                SelectedIndex = Items.IndexOf(value)
            End Set
        End Property

        Shadows ReadOnly Property SelectedItems As List(Of Object)
            Get
                Return SelectedRows.OrderBy(Function(x) x).Where(Function(x) x < Items.Count).Select(Function(x) Items(x)).ToList()
            End Get
        End Property

        ReadOnly Property SelectedIndices As List(Of Integer)
            Get
                Return SelectedRows.OrderBy(Function(x) x).ToList()
            End Get
        End Property

        Property Text As String
            Get
                Return GetItemText(SelectedItem)
            End Get
            Set(value As String)
                For x = 0 To Items.Count - 1
                    If GetItemText(Items(x)) = value Then
                        SelectedIndex = x
                        Exit For
                    End If
                Next
            End Set
        End Property

        Function GetSelected(index As Integer) As Boolean
            Return SelectedRows.Contains(index)
        End Function

        Sub SetSelected(index As Integer, value As Boolean)
            If value Then
                SelectRow(index)
            Else
                UnselectRow(index)
            End If
        End Sub

        Sub ClearSelected()
            UnselectAll()
        End Sub

        Function FindStringExact(text As String) As Integer
            For x = 0 To Items.Count - 1
                If GetItemText(Items(x)) = text Then
                    Return x
                End If
            Next

            Return -1
        End Function

        Sub UpdateSelection()
            Dim i = SelectedIndex

            If i > -1 Then
                ReloadData(i)

                If Sorted Then
                    SortItems()
                End If
            End If
        End Sub

        Sub SortItems()
            Dim selected = SelectedItem
            Dim sorted = Items.OrderBy(Function(x) GetItemText(x)).ToList()
            BlockEvents = True
            Items.Clear()

            For Each i In sorted
                Items.Add(i)
            Next

            BlockEvents = False
            SelectedItem = selected
        End Sub

        Sub SaveSelection()
            SavedSelection = SelectedItems
        End Sub

        Sub RestoreSelection()
            For Each i In SavedSelection
                Dim index = Items.IndexOf(i)

                If index > -1 Then
                    SelectRow(index)
                End If
            Next
        End Sub

        Sub DeleteItem(text As String)
            Dim index = FindStringExact(text)

            If index > -1 Then
                Items.RemoveAt(index)
            End If
        End Sub

        Sub RemoveSelection()
            Dim indices = SelectedIndices

            If indices.Count = 0 Then
                Exit Sub
            End If

            Dim first = indices(0)

            For i = indices.Count - 1 To 0 Step -1
                Items.RemoveAt(indices(i))
            Next

            If Items.Count > 0 Then
                SelectedIndex = Math.Min(first, Items.Count - 1)
            End If

            UpdateControls()
        End Sub

        Function CanMoveUp() As Boolean
            Dim indices = SelectedIndices
            Return indices.Count > 0 AndAlso indices(0) > 0
        End Function

        Function CanMoveDown() As Boolean
            Dim indices = SelectedIndices
            Return indices.Count > 0 AndAlso indices(indices.Count - 1) < Items.Count - 1
        End Function

        Sub MoveSelectionUp()
            If CanMoveUp() Then
                Dim selected = SelectedItems
                Dim indices = SelectedIndices
                Dim itemAbove = Items(indices(0) - 1)
                Items.RemoveAt(indices(0) - 1)
                Items.Insert(indices(indices.Count - 1), itemAbove)
                UnselectAll()

                For Each i In selected
                    SelectRow(Items.IndexOf(i))
                Next

                UpdateControls()
            End If
        End Sub

        Sub MoveSelectionDown()
            If CanMoveDown() Then
                Dim selected = SelectedItems
                Dim indices = SelectedIndices
                Dim itemBelow = Items(indices(indices.Count - 1) + 1)
                Items.RemoveAt(indices(indices.Count - 1) + 1)
                Items.Insert(indices(0), itemBelow)
                UnselectAll()

                For Each i In selected
                    SelectRow(Items.IndexOf(i))
                Next

                UpdateControls()
            End If
        End Sub

        Sub UpdateControls()
            Dim hasSelection = SelectedIndex > -1

            If RemoveButton IsNot Nothing Then RemoveButton.Enabled = hasSelection
            If UpButton IsNot Nothing Then UpButton.Enabled = CanMoveUp()
            If DownButton IsNot Nothing Then DownButton.Enabled = CanMoveDown()
            If Button1 IsNot Nothing Then Button1.Enabled = hasSelection
            If Button2 IsNot Nothing Then Button2.Enabled = hasSelection
        End Sub

        Protected Overrides Sub OnLoadComplete(e As EventArgs)
            MyBase.OnLoadComplete(e)

            'GTK measures rows added before loading too small, reloading measures them again
            If Items.Count > 0 Then
                Dim selected = SelectedRows.ToArray
                BlockEvents = True

                Try
                    DataStore = Nothing
                    DataStore = Items
                    SelectedRows = selected
                Finally
                    BlockEvents = False
                End Try
            End If

            If UpButton IsNot Nothing Then AddHandler UpButton.Click, Sub() MoveSelectionUp()
            If DownButton IsNot Nothing Then AddHandler DownButton.Click, Sub() MoveSelectionDown()
            If RemoveButton IsNot Nothing Then AddHandler RemoveButton.Click, Sub() RemoveSelection()

            If SelectedIndex = -1 AndAlso Items.Count > 0 Then
                SelectedIndex = 0
            End If

            UpdateControls()
        End Sub
    End Class
End Namespace
