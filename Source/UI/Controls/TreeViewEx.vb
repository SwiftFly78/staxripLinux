
Imports System.Collections.ObjectModel

Namespace UI
    Public Enum TreeNodeExpandMode
        Disabled
        Normal
        InclusiveChilds
    End Enum

    'tree node with a WinForms TreeNode like API
    Public Class TreeNode
        Inherits TreeGridItem

        Private TextValue As String = ""

        Sub New()
            MyBase.New()
            Nodes = New TreeNodeCollection(Me)
            Values = New Object() {""}
        End Sub

        Sub New(text As String)
            Me.New()
            Me.Text = text
        End Sub

        ReadOnly Property Nodes As TreeNodeCollection
        Property Name As String

        Property Text As String
            Get
                Return TextValue
            End Get
            Set(value As String)
                TextValue = If(value, "")
                Values = New Object() {TextValue}
                TreeView?.RefreshNode(Me)
            End Set
        End Property

        Shadows ReadOnly Property Parent As TreeNode
            Get
                Return TryCast(MyBase.Parent, TreeNode)
            End Get
        End Property

        Property OwnerTree As TreeViewEx

        ReadOnly Property TreeView As TreeViewEx
            Get
                Dim n = Me

                While n IsNot Nothing
                    If n.OwnerTree IsNot Nothing Then
                        Return n.OwnerTree
                    End If

                    n = n.Parent
                End While

                Return Nothing
            End Get
        End Property

        ReadOnly Property ParentCollection As TreeNodeCollection
            Get
                If Parent IsNot Nothing Then
                    Return Parent.Nodes
                End If

                Return TreeView?.Nodes
            End Get
        End Property

        ReadOnly Property Index As Integer
            Get
                Dim col = ParentCollection
                Return If(col Is Nothing, -1, col.IndexOf(Me))
            End Get
        End Property

        ReadOnly Property Level As Integer
            Get
                Dim ret = 0
                Dim n = Parent

                While n IsNot Nothing
                    ret += 1
                    n = n.Parent
                End While

                Return ret
            End Get
        End Property

        ReadOnly Property NextNode As TreeNode
            Get
                Dim col = ParentCollection
                Dim i = Index

                If col IsNot Nothing AndAlso i > -1 AndAlso i < col.Count - 1 Then
                    Return col(i + 1)
                End If

                Return Nothing
            End Get
        End Property

        ReadOnly Property PrevNode As TreeNode
            Get
                Dim col = ParentCollection
                Dim i = Index

                If col IsNot Nothing AndAlso i > 0 Then
                    Return col(i - 1)
                End If

                Return Nothing
            End Get
        End Property

        ReadOnly Property FullPath As String
            Get
                If Parent Is Nothing Then
                    Return Text
                End If

                Return Parent.FullPath + "\" + Text
            End Get
        End Property

        ReadOnly Property IsExpanded As Boolean
            Get
                Return Expanded
            End Get
        End Property

        ReadOnly Property IsSelected As Boolean
            Get
                Return TreeView IsNot Nothing AndAlso TreeView.SelectedNode Is Me
            End Get
        End Property

        Sub Expand()
            Expanded = True
            TreeView?.Reload()
        End Sub

        Sub ExpandAll()
            SetExpanded(Me, True)
            TreeView?.Reload()
        End Sub

        Sub Collapse()
            Expanded = False
            TreeView?.Reload()
        End Sub

        Shared Sub SetExpanded(node As TreeNode, value As Boolean)
            node.Expanded = value

            For Each i In node.Nodes
                SetExpanded(i, value)
            Next
        End Sub

        Sub Remove()
            Dim tree = TreeView
            ParentCollection?.Remove(Me)
            tree?.Reload()
        End Sub

        Sub EnsureVisible()
            Dim n = Parent

            While n IsNot Nothing
                n.Expanded = True
                n = n.Parent
            End While

            TreeView?.Reload()
        End Sub

        Overrides Function ToString() As String
            Return Text
        End Function
    End Class

    Public Class TreeNodeCollection
        Inherits Collection(Of TreeNode)

        Private ReadOnly OwnerNode As TreeNode
        Private ReadOnly OwnerTree As TreeViewEx

        Sub New(owner As TreeNode)
            OwnerNode = owner
        End Sub

        Sub New(owner As TreeViewEx)
            OwnerTree = owner
        End Sub

        Overloads Function Add(text As String) As TreeNode
            Dim ret As New TreeNode(text)
            Add(ret)
            Return ret
        End Function

        Sub AddRange(nodes As IEnumerable(Of TreeNode))
            For Each i In nodes.ToArray
                Add(i)
            Next
        End Sub

        Private Function GetChildren() As TreeGridItemCollection
            If OwnerNode IsNot Nothing Then
                Return OwnerNode.Children
            End If

            Return OwnerTree.RootItems
        End Function

        Private Sub Changed()
            Dim tree = If(OwnerTree, OwnerNode?.TreeView)
            tree?.Reload()
        End Sub

        Protected Overrides Sub InsertItem(index As Integer, item As TreeNode)
            MyBase.InsertItem(index, item)
            GetChildren().Insert(index, item)
            DirectCast(item, TreeGridItem).Parent = OwnerNode
            item.OwnerTree = OwnerTree
            Changed()
        End Sub

        Protected Overrides Sub RemoveItem(index As Integer)
            Dim item = Me(index)
            MyBase.RemoveItem(index)
            GetChildren().Remove(item)
            Changed()
        End Sub

        Protected Overrides Sub SetItem(index As Integer, item As TreeNode)
            MyBase.SetItem(index, item)
            GetChildren()(index) = item
            DirectCast(item, TreeGridItem).Parent = OwnerNode
            item.OwnerTree = OwnerTree
            Changed()
        End Sub

        Protected Overrides Sub ClearItems()
            MyBase.ClearItems()
            GetChildren().Clear()
            Changed()
        End Sub
    End Class

    Public Class TreeViewEventArgs
        Inherits EventArgs

        Property Node As TreeNode

        Sub New(node As TreeNode)
            Me.Node = node
        End Sub
    End Class

    'tree with a WinForms TreeView like API on top of Eto TreeGridView
    Public Class TreeViewEx
        Inherits TreeGridView

        Private UpdateCount As Integer
        Private BlockSelectEvent As Boolean

        Friend ReadOnly RootItems As New TreeGridItemCollection
        ReadOnly Property Nodes As TreeNodeCollection

        Property AutoCollaps As Boolean
        Property ExpandMode As TreeNodeExpandMode
        Property SelectOnMouseDown As Boolean
        Property ShowLines As Boolean
        Property ShowPlusMinus As Boolean = True
        Property FullRowSelect As Boolean = True
        Property HideSelection As Boolean
        Property Scrollable As Boolean = True
        Property Sorted As Boolean
        Property ItemHeight As Integer
        Property UseSystemScrollBars As Boolean = True

        Event AfterSelect(sender As Object, e As TreeViewEventArgs)
        Event AfterExpand(sender As Object, e As TreeViewEventArgs)
        Event AfterCollapse(sender As Object, e As TreeViewEventArgs)

        Sub New()
            MyBase.New()
            Nodes = New TreeNodeCollection(Me)
            ShowHeader = False
            AllowMultipleSelection = False
            GridLines = GridLines.None
            RowHeight = CInt(FontHeight * 1.45)

            Columns.Add(New GridColumn With {
                .DataCell = New TextBoxCell(0),
                .Expand = True,
                .Editable = False,
                .MinWidth = FontHeight
            })

            DataStore = RootItems

            AddHandler SelectedItemChanged, AddressOf OnSelectedItemChangedEx
            AddHandler Expanded, Sub(sender, e) RaiseEvent AfterExpand(Me, New TreeViewEventArgs(TryCast(e.Item, TreeNode)))
            AddHandler Collapsed, Sub(sender, e) RaiseEvent AfterCollapse(Me, New TreeViewEventArgs(TryCast(e.Item, TreeNode)))
        End Sub

        Private Sub OnSelectedItemChangedEx(sender As Object, e As EventArgs)
            If BlockSelectEvent Then
                Exit Sub
            End If

            Dim node = SelectedNode

            If node Is Nothing Then
                Exit Sub
            End If

            BlockSelectEvent = True

            Try
                Dim changed = False

                If AutoCollaps Then
                    Dim siblings = If(node.Parent Is Nothing, Nodes, node.Parent.Nodes)

                    For Each i In siblings
                        If i IsNot node AndAlso i.Expanded Then
                            TreeNode.SetExpanded(i, False)
                            changed = True
                        End If
                    Next
                End If

                Select Case ExpandMode
                    Case TreeNodeExpandMode.Normal
                        If Not node.Expanded AndAlso node.Nodes.Count > 0 Then
                            node.Expanded = True
                            changed = True
                        End If
                    Case TreeNodeExpandMode.InclusiveChilds
                        If node.Nodes.Count > 0 Then
                            TreeNode.SetExpanded(node, True)
                            changed = True
                        End If
                End Select

                If changed Then
                    ReloadData()
                    SelectedItem = node
                End If
            Finally
                BlockSelectEvent = False
            End Try

            RaiseEvent AfterSelect(Me, New TreeViewEventArgs(node))
        End Sub

        Property SelectedNode As TreeNode
            Get
                Return TryCast(SelectedItem, TreeNode)
            End Get
            Set(value As TreeNode)
                If value IsNot Nothing Then
                    Dim n = value.Parent
                    Dim changed = False

                    While n IsNot Nothing
                        If Not n.Expanded Then
                            n.Expanded = True
                            changed = True
                        End If

                        n = n.Parent
                    End While

                    If changed Then
                        ReloadData()
                    End If
                End If

                SelectedItem = value
            End Set
        End Property

        'GTK measures rows added before loading too small, reloading measures them again
        Protected Overrides Sub OnLoadComplete(e As EventArgs)
            MyBase.OnLoadComplete(e)

            If Nodes.Count > 0 Then
                Reload()
            End If
        End Sub

        Sub RefreshNode(node As TreeNode)
            If UpdateCount = 0 Then
                ReloadItem(node)
            End If
        End Sub

        Sub Reload()
            If UpdateCount = 0 Then
                Dim selected = SelectedItem
                BlockSelectEvent = True
                ReloadData()

                If selected IsNot Nothing Then
                    SelectedItem = selected
                End If

                BlockSelectEvent = False
            End If
        End Sub

        Sub BeginUpdate()
            UpdateCount += 1
        End Sub

        Sub EndUpdate()
            UpdateCount = Math.Max(0, UpdateCount - 1)

            If UpdateCount = 0 Then
                Reload()
            End If
        End Sub

        Sub ExpandAll()
            For Each i In Nodes
                TreeNode.SetExpanded(i, True)
            Next

            Reload()
        End Sub

        Sub CollapseAll()
            For Each i In Nodes
                TreeNode.SetExpanded(i, False)
            Next

            Reload()
        End Sub

        Function AddNode(path As String) As TreeNode
            Dim pathElements = path.SplitNoEmptyAndWhiteSpace("|")
            Dim currentNodeList = Nodes
            Dim ret As TreeNode = Nothing

            For Each iNodeName In pathElements
                Dim found = False

                For Each iNode As TreeNode In currentNodeList
                    If iNode.Text = iNodeName Then
                        ret = iNode
                        currentNodeList = iNode.Nodes
                        found = True
                    End If
                Next

                If Not found Then
                    ret = New TreeNode With {.Text = iNodeName}
                    currentNodeList.Add(ret)
                    currentNodeList = ret.Nodes
                End If
            Next

            Return ret
        End Function

        Function GetNodes() As List(Of TreeNode)
            Dim ret As New List(Of TreeNode)
            AddNodesRecursive(Nodes, ret)
            Return ret
        End Function

        Shared Sub AddNodesRecursive(searchList As TreeNodeCollection, returnList As List(Of TreeNode))
            For Each i As TreeNode In searchList
                returnList.Add(i)
                AddNodesRecursive(i.Nodes, returnList)
            Next
        End Sub

        Function GetAllVisibleNodes() As List(Of TreeNode)
            Dim list As New List(Of TreeNode)

            For Each node As TreeNode In Nodes
                GetVisibleNodes(list, node)
            Next

            Return list
        End Function

        Shared Sub GetVisibleNodes(list As List(Of TreeNode), node As TreeNode)
            list.Add(node)

            If node.IsExpanded Then
                For Each subNode As TreeNode In node.Nodes
                    GetVisibleNodes(list, subNode)
                Next
            End If
        End Sub

        Function GetParentNodes(n As TreeNode) As TreeNodeCollection
            Return If(n.Parent Is Nothing, Nodes, n.Parent.Nodes)
        End Function

        Function GetParentParentNodes(n As TreeNode) As TreeNodeCollection
            Dim parent = n.Parent

            If parent Is Nothing Then
                Return Nothing
            End If

            Return If(parent.Parent Is Nothing, Nodes, parent.Parent.Nodes)
        End Function

        Sub MoveSelectedNodeLeft()
            Dim n = SelectedNode

            If n IsNot Nothing AndAlso n.Parent IsNot Nothing Then
                Dim parentParentNodes = GetParentParentNodes(n)
                Dim parentIndex = n.Parent.Index

                If parentParentNodes IsNot Nothing Then
                    BeginUpdate()
                    n.Parent.Nodes.Remove(n)
                    parentParentNodes.Insert(parentIndex + 1, n)
                    EndUpdate()
                    SelectedNode = n
                End If
            End If
        End Sub

        Sub MoveSelectedNodeUp()
            Dim n = SelectedNode

            If n Is Nothing Then
                Exit Sub
            End If

            BeginUpdate()

            If n.Index = 0 Then
                Dim parentParentNodes = GetParentParentNodes(n)

                If parentParentNodes IsNot Nothing Then
                    Dim index = n.Parent.Index
                    n.Parent.Nodes.Remove(n)
                    parentParentNodes.Insert(index, n)
                End If
            Else
                Dim index = n.Index
                Dim parentNodes = GetParentNodes(n)
                parentNodes.Remove(n)
                parentNodes.Insert(index - 1, n)
            End If

            EndUpdate()
            SelectedNode = n
        End Sub

        Sub MoveSelectedNodeRight()
            Dim n = SelectedNode

            If n IsNot Nothing AndAlso n.Index > 0 Then
                Dim previousNode = n.PrevNode
                BeginUpdate()
                GetParentNodes(n).Remove(n)
                previousNode.Nodes.Add(n)
                previousNode.Expanded = True
                EndUpdate()
                SelectedNode = n
            End If
        End Sub

        Sub MoveSelectedNodeDown()
            Dim n = SelectedNode

            If n Is Nothing Then
                Exit Sub
            End If

            BeginUpdate()

            If n.NextNode Is Nothing Then
                Dim parentParentNodes = GetParentParentNodes(n)

                If parentParentNodes IsNot Nothing Then
                    Dim index = n.Parent.Index
                    n.Parent.Nodes.Remove(n)
                    parentParentNodes.Insert(index + 1, n)
                End If
            Else
                Dim index = n.Index
                Dim parentNodes = GetParentNodes(n)
                parentNodes.Remove(n)
                parentNodes.Insert(index + 1, n)
            End If

            EndUpdate()
            SelectedNode = n
        End Sub

        Sub MoveSelectionDown()
            If SelectedNode?.NextNode IsNot Nothing Then
                SelectedNode = SelectedNode.NextNode
            End If
        End Sub

        Sub MoveSelectionUp()
            If SelectedNode?.PrevNode IsNot Nothing Then
                SelectedNode = SelectedNode.PrevNode
            End If
        End Sub
    End Class
End Namespace
