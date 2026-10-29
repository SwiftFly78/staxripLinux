
Imports System.Collections.ObjectModel

Namespace UI
    Public Enum AutoCheckMode
        None
        SingleClick
        DoubleClick
    End Enum

    Public Enum View
        Details
        List
        Tile
        LargeIcon
        SmallIcon
    End Enum

    Public Enum ColumnHeaderStyle
        None
        Nonclickable
        Clickable
    End Enum

    Public Class ListViewSubItem
        Friend Item As ListViewItem
        Private TextValue As String = ""

        Sub New()
        End Sub

        Sub New(text As String)
            TextValue = If(text, "")
        End Sub

        Property Text As String
            Get
                Return TextValue
            End Get
            Set(value As String)
                TextValue = If(value, "")
                Item?.Refresh()
            End Set
        End Property

        Property Tag As Object
        Property ForeColor As Color
        Property BackColor As Color
        Property Font As Font

        Overrides Function ToString() As String
            Return Text
        End Function
    End Class

    Public Class ListViewSubItemCollection
        Inherits Collection(Of ListViewSubItem)

        Private ReadOnly OwnerItem As ListViewItem

        Sub New(item As ListViewItem)
            OwnerItem = item
        End Sub

        Overloads Function Add(text As String) As ListViewSubItem
            Dim ret As New ListViewSubItem(text)
            Add(ret)
            Return ret
        End Function

        Sub AddRange(texts As IEnumerable(Of String))
            For Each i In texts
                Add(i)
            Next
        End Sub

        Protected Overrides Sub InsertItem(index As Integer, value As ListViewSubItem)
            value.Item = OwnerItem
            MyBase.InsertItem(index, value)
            OwnerItem.Refresh()
        End Sub

        Protected Overrides Sub SetItem(index As Integer, value As ListViewSubItem)
            value.Item = OwnerItem
            MyBase.SetItem(index, value)
            OwnerItem.Refresh()
        End Sub
    End Class

    Public Class ListViewItem
        Friend Owner As ListViewEx
        Private CheckedValue As Boolean

        Sub New()
            SubItems = New ListViewSubItemCollection(Me)
            SubItems.Add("")
        End Sub

        Sub New(text As String)
            Me.New()
            Me.Text = text
        End Sub

        Sub New(items As String())
            Me.New()

            If items.Length > 0 Then
                Text = items(0)
            End If

            For x = 1 To items.Length - 1
                SubItems.Add(items(x))
            Next
        End Sub

        ReadOnly Property SubItems As ListViewSubItemCollection
        Property Tag As Object
        Property Name As String
        Property ForeColor As Color
        Property BackColor As Color
        Property Font As Font
        Property ImageIndex As Integer = -1

        Property Text As String
            Get
                Return SubItems(0).Text
            End Get
            Set(value As String)
                SubItems(0).Text = value
            End Set
        End Property

        Property Checked As Boolean
            Get
                Return CheckedValue
            End Get
            Set(value As Boolean)
                If value <> CheckedValue Then
                    CheckedValue = value
                    Owner?.OnItemCheckedChanged(Me)
                    Refresh()
                End If
            End Set
        End Property

        Friend Sub SetCheckedSilent(value As Boolean)
            CheckedValue = value
        End Sub

        Property Selected As Boolean
            Get
                Return Owner IsNot Nothing AndAlso Owner.SelectedRows.Contains(Index)
            End Get
            Set(value As Boolean)
                If Owner Is Nothing OrElse Index < 0 Then
                    Exit Property
                End If

                If value Then
                    Owner.SelectRow(Index)
                Else
                    Owner.UnselectRow(Index)
                End If

                Owner.CheckSelectionChanged()
            End Set
        End Property

        ReadOnly Property Index As Integer
            Get
                If Owner Is Nothing Then
                    Return -1
                End If

                Return Owner.Items.IndexOf(Me)
            End Get
        End Property

        ReadOnly Property ListView As ListViewEx
            Get
                Return Owner
            End Get
        End Property

        Sub Remove()
            Owner?.Items.Remove(Me)
        End Sub

        Sub EnsureVisible()
            If Owner IsNot Nothing AndAlso Index > -1 Then
                Owner.ScrollToRow(Index)
            End If
        End Sub

        Friend Sub Refresh()
            If Owner IsNot Nothing AndAlso Not Owner.IsUpdating Then
                Dim i = Index

                If i > -1 Then
                    Owner.ReloadData(i)
                End If
            End If
        End Sub

        Function GetSubItemText(index As Integer) As String
            If index < SubItems.Count Then
                Return SubItems(index).Text
            End If

            Return ""
        End Function

        Overrides Function ToString() As String
            Return Text
        End Function
    End Class

    Public Class ListViewItemCollection
        Inherits ObservableCollection(Of ListViewItem)

        Private ReadOnly Owner As ListViewEx

        Sub New(owner As ListViewEx)
            Me.Owner = owner
        End Sub

        Overloads Function Add(text As String) As ListViewItem
            Dim ret As New ListViewItem(text)
            Add(ret)
            Return ret
        End Function

        Sub AddRange(items As IEnumerable(Of ListViewItem))
            For Each i In items.ToArray
                Add(i)
            Next
        End Sub

        Protected Overrides Sub InsertItem(index As Integer, item As ListViewItem)
            item.Owner = Owner
            MyBase.InsertItem(index, item)
        End Sub

        Protected Overrides Sub SetItem(index As Integer, item As ListViewItem)
            item.Owner = Owner
            MyBase.SetItem(index, item)
        End Sub
    End Class

    Public Class ColumnHeader
        Friend Owner As ListViewEx
        Friend GridColumn As GridColumn
        Private TextValue As String = ""
        Private WidthValue As Integer = -1

        Property Text As String
            Get
                Return TextValue
            End Get
            Set(value As String)
                TextValue = If(value, "")

                If GridColumn IsNot Nothing Then
                    GridColumn.HeaderText = TextValue
                End If
            End Set
        End Property

        Property Width As Integer
            Get
                If GridColumn IsNot Nothing AndAlso GridColumn.Width > 0 Then
                    Return GridColumn.Width
                End If

                Return WidthValue
            End Get
            Set(value As Integer)
                WidthValue = value

                If GridColumn IsNot Nothing AndAlso value > 0 Then
                    GridColumn.AutoSize = False
                    GridColumn.Width = value
                    Owner?.InvalidateColumnSizes()
                End If
            End Set
        End Property

        Property TextAlign As HorizontalAlignment = HorizontalAlignment.Left
        Property Tag As Object

        ReadOnly Property Index As Integer
            Get
                Return If(Owner Is Nothing, -1, Owner.Columns.IndexOf(Me))
            End Get
        End Property

        Sub AutoResize(style As Object)
            If GridColumn IsNot Nothing Then
                GridColumn.AutoSize = True
                Owner?.InvalidateColumnSizes()
            End If
        End Sub
    End Class

    Public Class ColumnHeaderCollection
        Inherits Collection(Of ColumnHeader)

        Private ReadOnly Owner As ListViewEx

        Sub New(owner As ListViewEx)
            Me.Owner = owner
        End Sub

        Overloads Function Add(text As String) As ColumnHeader
            Return Add(text, -1)
        End Function

        Overloads Function Add(text As String, width As Integer) As ColumnHeader
            Dim ret As New ColumnHeader With {.Text = text, .Width = width}
            Add(ret)
            Return ret
        End Function

        Overloads Function Add(text As String, width As Integer, align As HorizontalAlignment) As ColumnHeader
            Dim ret = Add(text, width)
            ret.TextAlign = align
            Return ret
        End Function

        Sub AddRange(headers As IEnumerable(Of ColumnHeader))
            For Each i In headers
                Add(i)
            Next
        End Sub

        Protected Overrides Sub InsertItem(index As Integer, item As ColumnHeader)
            item.Owner = Owner
            MyBase.InsertItem(index, item)
            Owner.RebuildColumns()
        End Sub

        Protected Overrides Sub RemoveItem(index As Integer)
            MyBase.RemoveItem(index)
            Owner.RebuildColumns()
        End Sub

        Protected Overrides Sub ClearItems()
            MyBase.ClearItems()
            Owner.RebuildColumns()
            Owner.CheckSelectionChanged()
        End Sub
    End Class

    'Grid based list with a WinForms ListView like API
    Public Class ListViewEx
        Inherits GridView
        Implements IContextMenuOwner

        Private CheckBoxesValue As Boolean
        Private ContextMenuValue As ContextMenuStripEx
        Private UpdateCount As Integer
        Private BlockItemChanged As Boolean
        Private HeaderStyleValue As ColumnHeaderStyle = ColumnHeaderStyle.Clickable
        Private SortColumn As Integer = -1
        Private SortAscending As Boolean = True
        Private IsReloading As Boolean
        Private LastSelection As ListViewItem() = {}

        ReadOnly Property Items As ListViewItemCollection
        Shadows ReadOnly Property Columns As ColumnHeaderCollection

        Property UpButton As Button
        Property DownButton As Button
        Property RemoveButton As Button
        Property SingleSelectionButtons As Button()
        Property MultiSelectionButtons As Button()
        Property ItemCheckProperty As String
        Property ShowContextMenuOnLeftClick As Boolean
        Property RightClickOnlyForMenu As Boolean
        Property AutoCheckMode As AutoCheckMode = AutoCheckMode.DoubleClick
        Property View As View = View.Details
        Property FullRowSelect As Boolean = True
        Property HideSelection As Boolean
        Property SortByColumnClick As Boolean

        'text of the items instead of their ToString
        Property ItemText As Func(Of Object, String)

        Event ItemsChanged(indices As Integer())
        Event ItemRemoved(item As ListViewItem)
        Event UpdateContextMenu()
        Event ControlsUpdated()
        Event SelectedIndexChanged(sender As Object, e As EventArgs)
        Event ItemChecked(sender As Object, e As ItemCheckedEventArgs)
        Event DoubleClick(sender As Object, e As EventArgs)

        Sub New()
            MyBase.New()
            Items = New ListViewItemCollection(Me)
            Columns = New ColumnHeaderCollection(Me)
            DataStore = Items
            AllowMultipleSelection = True
            GridLines = GridLines.None
            AllowColumnReordering = False
            RowHeight = CInt(FontHeight * 1.45)

            AddHandler SelectionChanged, Sub() CheckSelectionChanged()

            AddHandler CellFormatting, AddressOf FormatCell
            AddHandler ColumnHeaderClick, AddressOf OnColumnHeaderClick
            RebuildColumns()
        End Sub

        Property MultiSelect As Boolean
            Get
                Return AllowMultipleSelection
            End Get
            Set(value As Boolean)
                AllowMultipleSelection = value
            End Set
        End Property

        Property CheckBoxes As Boolean
            Get
                Return CheckBoxesValue
            End Get
            Set(value As Boolean)
                CheckBoxesValue = value
                RebuildColumns()
            End Set
        End Property

        Property HeaderStyle As ColumnHeaderStyle
            Get
                Return HeaderStyleValue
            End Get
            Set(value As ColumnHeaderStyle)
                HeaderStyleValue = value
                ShowHeader = value <> ColumnHeaderStyle.None
            End Set
        End Property

        Property ContextMenuStrip As ContextMenuStripEx Implements IContextMenuOwner.ContextMenuStrip
            Get
                Return ContextMenuValue
            End Get
            Set(value As ContextMenuStripEx)
                ContextMenuValue = value
                ContextMenu = value

                If value IsNot Nothing Then
                    AddHandler value.Opening, Sub() RaiseEvent UpdateContextMenu()
                End If
            End Set
        End Property

        ReadOnly Property IsUpdating As Boolean
            Get
                Return UpdateCount > 0
            End Get
        End Property

        Shadows ReadOnly Property SelectedItems As List(Of ListViewItem)
            Get
                Return SelectedIndices.Select(Function(i) Items(i)).ToList()
            End Get
        End Property

        ReadOnly Property SelectedIndices As List(Of Integer)
            Get
                Return SelectedRows.Where(Function(i) i >= 0 AndAlso i < Items.Count).OrderBy(Function(i) i).ToList()
            End Get
        End Property

        'Eto doesn't report a selection change when the selected row index stays the same
        'but the items were replaced, WinForms does, so the selected items are compared
        Friend Sub CheckSelectionChanged()
            If IsReloading Then
                Exit Sub
            End If

            Dim current = SelectedItems.ToArray

            If Not current.SequenceEqual(LastSelection) Then
                LastSelection = current
                UpdateControls()
                RaiseEvent SelectedIndexChanged(Me, EventArgs.Empty)
            End If
        End Sub

        ReadOnly Property CheckedItems As List(Of ListViewItem)
            Get
                Return Items.Where(Function(i) i.Checked).ToList()
            End Get
        End Property

        ReadOnly Property CheckedIndices As List(Of Integer)
            Get
                Return Items.Where(Function(i) i.Checked).Select(Function(i) i.Index).ToList()
            End Get
        End Property

        ReadOnly Property FocusedItem As ListViewItem
            Get
                Dim indices = SelectedIndices
                Return If(indices.Count > 0, Items(indices(0)), Nothing)
            End Get
        End Property

        Friend Sub RebuildColumns()
            If Columns Is Nothing Then
                Exit Sub
            End If

            MyBase.Columns.Clear()

            If CheckBoxes Then
                Dim checkColumn As New GridColumn With {
                    .DataCell = New CheckBoxCell With {
                        .Binding = Binding.Delegate(Of ListViewItem, Boolean?)(
                            Function(item) item.Checked,
                            Sub(item, value) item.Checked = value.GetValueOrDefault)},
                    .Editable = True,
                    .Width = CInt(FontHeight * 1.8),
                    .AutoSize = False,
                    .Resizable = False
                }

                MyBase.Columns.Add(checkColumn)
            End If

            If Columns.Count = 0 Then
                Dim textColumn As New GridColumn With {
                    .DataCell = New TextBoxCell With {.Binding = Binding.Delegate(Of ListViewItem, String)(Function(item) item.Text)},
                    .Expand = True,
                    .Editable = False,
                    .MinWidth = FontHeight
                }

                MyBase.Columns.Add(textColumn)
            Else
                For x = 0 To Columns.Count - 1
                    Dim index = x
                    Dim header = Columns(x)

                    Dim cell As New TextBoxCell With {.Binding = Binding.Delegate(Of ListViewItem, String)(Function(item) item.GetSubItemText(index))}

                    If header.TextAlign = HorizontalAlignment.Right Then
                        cell.TextAlignment = TextAlignment.Right
                    ElseIf header.TextAlign = HorizontalAlignment.Center Then
                        cell.TextAlignment = TextAlignment.Center
                    End If

                    Dim column As New GridColumn With {
                        .HeaderText = header.Text,
                        .DataCell = cell,
                        .Editable = False,
                        .Sortable = SortByColumnClick,
                        .Expand = x = Columns.Count - 1,
                        .MinWidth = FontHeight
                    }

                    If header.Width > 0 Then
                        column.AutoSize = False
                        column.Width = header.Width
                    End If

                    header.GridColumn = column
                    MyBase.Columns.Add(column)
                Next
            End If
        End Sub

        Sub FormatCell(sender As Object, e As GridCellFormatEventArgs)
            Dim item = TryCast(e.Item, ListViewItem)

            If item Is Nothing Then
                Exit Sub
            End If

            If item.ForeColor.A > 0 Then
                e.ForegroundColor = item.ForeColor
            End If

            If item.BackColor.A > 0 Then
                e.BackgroundColor = item.BackColor
            End If

            If item.Font IsNot Nothing Then
                e.Font = item.Font
            End If
        End Sub

        Overloads Sub OnColumnHeaderClick(sender As Object, e As GridColumnEventArgs)
            If Not SortByColumnClick Then
                Exit Sub
            End If

            Dim index = MyBase.Columns.IndexOf(e.Column) - If(CheckBoxes, 1, 0)

            If index < 0 Then
                Exit Sub
            End If

            If SortColumn = index Then
                SortAscending = Not SortAscending
            Else
                SortAscending = True
            End If

            SortColumn = index
            Dim comparer As New StringLogicalComparer
            Dim sorted = Items.OrderBy(Function(i) i.GetSubItemText(index), comparer).ToList()

            If Not SortAscending Then
                sorted.Reverse()
            End If

            BeginUpdate()
            Items.Clear()
            Items.AddRange(sorted)
            EndUpdate()
        End Sub

        Friend Sub OnItemCheckedChanged(item As ListViewItem)
            If ItemCheckProperty <> "" AndAlso item.Tag IsNot Nothing Then
                item.Tag.GetType.GetProperty(ItemCheckProperty).SetValue(item.Tag, item.Checked)
            End If

            RaiseEvent ItemChecked(Me, New ItemCheckedEventArgs(item))

            If ItemCheckProperty <> "" AndAlso Not BlockItemChanged Then
                OnItemsChanged()
            End If
        End Sub

        Sub BeginUpdate()
            UpdateCount += 1
        End Sub

        Sub EndUpdate()
            UpdateCount = Math.Max(0, UpdateCount - 1)

            If UpdateCount = 0 Then
                Invalidate()
            End If
        End Sub

        Sub OnItemsChanged(ParamArray indices As Integer())
            RaiseEvent ItemsChanged(indices)
        End Sub

        Sub SelectFirst()
            If Items.Count > 0 Then
                SelectRow(0)
            End If

            UpdateControls()
        End Sub

        Shadows Function SelectedItem(Of T)() As T
            Dim indices = SelectedIndices

            If indices.Count > 0 Then
                Return DirectCast(Items(indices(0)).Tag, T)
            End If
        End Function

        Shadows Function SelectedItem() As Object
            Dim indices = SelectedIndices

            If indices.Count > 0 Then
                Return Items(indices(0)).Tag
            End If
        End Function

        Function AddItem(item As Object) As ListViewItem
            Dim listItem As New ListViewItem
            listItem.Tag = item
            Items.Add(listItem)
            RefreshItem(listItem.Index)

            If Not BlockItemChanged Then
                OnItemsChanged()
            End If

            Return listItem
        End Function

        Sub AddItems(items As IEnumerable)
            BlockItemChanged = True

            For Each item In items
                AddItem(item)
            Next

            BlockItemChanged = False
        End Sub

        Sub RefreshItem(index As Integer)
            Dim item = Items(index)

            If ItemCheckProperty <> "" Then
                item.SetCheckedSilent(CBool(item.Tag.GetType.GetProperty(ItemCheckProperty).GetValue(item.Tag)))
            End If

            item.Text = If(ItemText IsNot Nothing, ItemText(item.Tag), item.Tag.ToString)
            ReloadData(index)
        End Sub

        Sub RefreshSelection()
            For Each index In SelectedIndices
                RefreshItem(index)
            Next
        End Sub

        Sub EnableListBoxMode()
            HeaderStyle = ColumnHeaderStyle.None
        End Sub

        Sub UpdateControls()
            Dim indices = SelectedIndices

            If UpButton IsNot Nothing Then
                UpButton.Enabled = CanMoveUp()
            End If

            If DownButton IsNot Nothing Then
                DownButton.Enabled = CanMoveDown()
            End If

            If RemoveButton IsNot Nothing Then
                RemoveButton.Enabled = indices.Count > 0
            End If

            If SingleSelectionButtons IsNot Nothing Then
                For Each button In SingleSelectionButtons
                    button.Enabled = indices.Count = 1
                Next
            End If

            If MultiSelectionButtons IsNot Nothing Then
                For Each button In MultiSelectionButtons
                    button.Enabled = indices.Count > 0
                Next
            End If

            RaiseEvent ControlsUpdated()
        End Sub

        Function CanMoveUp() As Boolean
            Dim indices = SelectedIndices
            Return indices.Count > 0 AndAlso indices(0) > 0
        End Function

        Function CanMoveDown() As Boolean
            Dim indices = SelectedIndices
            Return indices.Count > 0 AndAlso indices(indices.Count - 1) < Items.Count - 1
        End Function

        Private Sub SelectItems(selected As IEnumerable(Of ListViewItem))
            UnselectAll()

            For Each i In selected
                SelectRow(i.Index)
            Next
        End Sub

        Sub MoveSelectionTop()
            If CanMoveUp() Then
                Dim selected = SelectedItems

                For Each i In selected
                    Items.Remove(i)
                Next

                For x = 0 To selected.Count - 1
                    Items.Insert(x, selected(x))
                Next

                SelectItems(selected)
                UpdateControls()
                OnItemsChanged()
            End If
        End Sub

        Sub MoveSelectionBottom()
            If CanMoveDown() Then
                Dim selected = SelectedItems

                For Each item In selected
                    Items.Remove(item)
                Next

                Items.AddRange(selected)
                SelectItems(selected)
                UpdateControls()
                OnItemsChanged()
            End If
        End Sub

        Sub MoveSelectionUp()
            If CanMoveUp() Then
                Dim indices = SelectedIndices
                Dim selected = SelectedItems
                Dim indexAbove = indices(0) - 1
                Dim movedIndices = indices.Union({indexAbove}).ToArray()
                Dim itemAbove = Items(indexAbove)
                Dim indexInsert = indices(indices.Count - 1)

                Items.RemoveAt(indexAbove)
                Items.Insert(indexInsert, itemAbove)
                SelectItems(selected)
                UpdateControls()
                OnItemsChanged(movedIndices)
                ScrollToRow(indexAbove)
            End If
        End Sub

        Sub MoveSelectionDown()
            If CanMoveDown() Then
                Dim indices = SelectedIndices
                Dim selected = SelectedItems
                Dim indexBelow = indices(indices.Count - 1) + 1
                Dim movedIndices = indices.Union({indexBelow}).ToArray()
                Dim itemBelow = Items(indexBelow)
                Dim indexInsert = indices(0)

                Items.RemoveAt(indexBelow)
                Items.Insert(indexInsert, itemBelow)
                SelectItems(selected)
                UpdateControls()
                OnItemsChanged(movedIndices)
                ScrollToRow(indexBelow)
            End If
        End Sub

        Sub RemoveSelection()
            Dim indices = SelectedIndices

            If indices.Count = 0 Then
                Exit Sub
            End If

            Const maxToDisplay As Integer = 15
            Dim names = indices.Take(maxToDisplay).Select(Function(i) Items(i).Text).Join(BR, True)

            If indices.Count > maxToDisplay AndAlso names.Length > 4 Then
                names = names + BR + "... and more ..."
            End If

            If Not MsgConfirm("Remove the selected items?", names, "Remove") Then
                Exit Sub
            End If

            Dim first = indices(0)

            For i = indices.Count - 1 To 0 Step -1
                Dim removedItem = Items(indices(i))
                Items.RemoveAt(indices(i))
                RaiseEvent ItemRemoved(removedItem)
            Next

            If Items.Count > 0 Then
                Dim index = If(first > Items.Count - 1, Items.Count - 1, first)
                SelectRow(index)
                ScrollToRow(index)
            End If

            UpdateControls()
            OnItemsChanged()
        End Sub

        Sub SortItems()
            Dim sortedItems = Items.OrderBy(Function(item) item.Text).ToArray
            Items.Clear()
            Items.AddRange(sortedItems)
            UpdateControls()
            OnItemsChanged()
        End Sub

        Sub EnsureVisible(index As Integer)
            If index > -1 AndAlso index < Items.Count Then
                ScrollToRow(index)
            End If
        End Sub

        Sub ShowMenu()
            RaiseEvent UpdateContextMenu()
            ContextMenuStrip?.Show(Me)
        End Sub

        Sub AutoResizeColumns(lastAtListViewWidth As Boolean)
            For Each i In MyBase.Columns
                If i.DataCell IsNot Nothing AndAlso TypeOf i.DataCell Is TextBoxCell AndAlso Not i.Expand Then
                    i.AutoSize = True
                End If
            Next

            InvalidateColumnSizes()
        End Sub

        Private ColumnSizesPending As Boolean

        'GTK only updates the space of expanding columns when the list width or its rows change
        Friend Sub InvalidateColumnSizes()
            If ColumnSizesPending OrElse Not Loaded Then
                Exit Sub
            End If

            ColumnSizesPending = True

            Application.Instance.AsyncInvoke(Sub()
                                                 ColumnSizesPending = False

                                                 If Not IsDisposed Then
                                                     TryCast(ControlObject, Global.Gtk.TreeView)?.ColumnsAutosize()
                                                 End If
                                             End Sub)
        End Sub

        'GTK measures rows added before loading too small, reloading measures them again
        Protected Overrides Sub OnLoadComplete(e As EventArgs)
            MyBase.OnLoadComplete(e)

            If Items.Count = 0 Then
                Exit Sub
            End If

            Dim selected = SelectedRows.ToArray
            IsReloading = True

            Try
                DataStore = Nothing
                DataStore = Items
                SelectedRows = selected
            Finally
                IsReloading = False
            End Try
        End Sub

        Protected Overrides Sub OnCellDoubleClick(e As GridCellMouseEventArgs)
            MyBase.OnCellDoubleClick(e)

            If CheckBoxes AndAlso AutoCheckMode = AutoCheckMode.DoubleClick AndAlso e.Row > -1 AndAlso e.Row < Items.Count Then
                Items(e.Row).Checked = Not Items(e.Row).Checked
            End If

            RaiseEvent DoubleClick(Me, EventArgs.Empty)
        End Sub

        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            MyBase.OnMouseUp(e)

            If ShowContextMenuOnLeftClick AndAlso e.Buttons = MouseButtons.Primary AndAlso ContextMenuStrip IsNot Nothing Then
                RaiseEvent UpdateContextMenu()
                ContextMenuStrip.Show(Me, e.Location)
            End If
        End Sub

        Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
            MyBase.OnKeyDown(e)

            If e.Handled Then
                Exit Sub
            End If

            If CheckBoxes AndAlso e.KeyData = Keys.Space Then
                Dim selected = SelectedItems

                If selected.Count > 0 Then
                    Dim value = Not selected(0).Checked

                    For Each i In selected
                        i.Checked = value
                    Next

                    e.Handled = True
                End If
            End If
        End Sub
    End Class

    Public Class ItemCheckedEventArgs
        Inherits EventArgs

        Property Item As ListViewItem

        Sub New(item As ListViewItem)
            Me.Item = item
        End Sub
    End Class
End Namespace
