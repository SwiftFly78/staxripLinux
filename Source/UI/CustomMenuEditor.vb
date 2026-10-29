
Imports System.Reflection

Namespace UI
    Public Class CustomMenuEditor
        Inherits DialogBase

        Public lParameters, laText, laHotkey, laCommand As LabelEx
        Public WithEvents pg As PropertyGridEx
        Private WithEvents tbText As TextEdit
        Public TipProvider As TipProvider
        Public WithEvents tv As TreeViewEx
        Public WithEvents tbHotkey As TextEdit
        Public WithEvents tbCommand As TextEdit
        Public WithEvents bnCommand As ButtonEx
        Public WithEvents cmsCommand As ContextMenuStripEx
        Public WithEvents bnCancel As ButtonEx
        Public WithEvents bnOK As ButtonEx
        Public WithEvents tsbNew, tsbCut, tsbCopy, tsbPaste, tsbMoveLeft, tsbMoveRight, tsbMoveUp, tsbMoveDown, tsbRemove, bnTools As ButtonEx
        Public WithEvents cmsTools As ContextMenuStripEx
        Public WithEvents NewFromDefaultsToolStripMenuItem As MenuItemEx
        Public WithEvents ResetToolStripMenuItem As MenuItemEx

        Private Sub InitializeComponent()
            laText = New LabelEx With {.Text = "Text:"}
            tbText = New TextEdit
            laHotkey = New LabelEx With {.Text = "Hotkey:"}
            tbHotkey = New TextEdit With {.ReadOnly = True}
            laCommand = New LabelEx With {.Text = "Command:"}
            tbCommand = New TextEdit
            cmsCommand = New ContextMenuStripEx
            bnCommand = New ButtonEx With {.ShowMenuSymbol = True}
            lParameters = New LabelEx With {.Text = "Parameters:"}
            pg = New PropertyGridEx
            tv = New TreeViewEx
            TipProvider = New TipProvider
            bnOK = CreateButton("OK", DialogResult.OK)
            bnCancel = CreateButton("Cancel", DialogResult.Cancel)

            tsbNew = New ButtonEx With {.Text = "New", .ToolTip = "New item"}
            tsbCut = New ButtonEx With {.Text = "Cut"}
            tsbCopy = New ButtonEx With {.Text = "Copy"}
            tsbPaste = New ButtonEx With {.Text = "Paste"}
            tsbMoveLeft = New ButtonEx With {.Symbol = Symbol.Back, .ToolTip = "Move left"}
            tsbMoveRight = New ButtonEx With {.Symbol = Symbol.Forward, .ToolTip = "Move right"}
            tsbMoveUp = New ButtonEx With {.Symbol = Symbol.Up, .ToolTip = "Move up"}
            tsbMoveDown = New ButtonEx With {.Symbol = Symbol.Down, .ToolTip = "Move down"}
            tsbRemove = New ButtonEx With {.Text = "Remove"}
            cmsTools = New ContextMenuStripEx
            NewFromDefaultsToolStripMenuItem = New MenuItemEx("New From Defaults...")
            ResetToolStripMenuItem = New MenuItemEx("Reset Everything")
            cmsTools.Items.Add(NewFromDefaultsToolStripMenuItem)
            cmsTools.Items.Add(ResetToolStripMenuItem)
            bnTools = New ButtonEx With {.Text = "Tools", .ShowMenuSymbol = True, .ContextMenuStrip = cmsTools}

            Dim toolbar = HRow(tsbNew, tsbCut, tsbCopy, tsbPaste, tsbMoveLeft, tsbMoveRight, tsbMoveUp, tsbMoveDown, tsbRemove, bnTools)

            Dim right As New TableLayout(
                Row(laText), Row(tbText),
                Row(laHotkey), Row(tbHotkey),
                Row(laCommand), Row(New TableLayout(Row(Scaled(tbCommand), bnCommand)) With {.Spacing = DefaultSpacing}),
                Row(lParameters),
                ScaledRow(pg)) With {.Spacing = DefaultSpacing}

            Content = DialogContent(ButtonRow(bnOK, bnCancel),
                Row(toolbar),
                ScaledRow(New TableLayout(ScaledRow(Scaled(tv), Scaled(right))) With {.Spacing = New Size(FH(0.8), 0)}))

            Text = $"Menu Editor - {g.DefaultCommands.GetApplicationDetails()}"
        End Sub

        Private Block As Boolean
        Private GridTypeDescriptor As GridTypeDescriptor
        Private ClipboardNode As TreeNode
        Private IsClosing As Boolean

        Property GenericMenu As CustomMenu

        Sub New(menu As CustomMenu)
            MyBase.New()
            InitializeComponent()
            ScaleClientSize(50, 36)

            GenericMenu = menu
            GridTypeDescriptor = New GridTypeDescriptor
            PopulateTreeView(menu.MenuItem.GetClone, Nothing)
            tv.ExpandAll()

            Command.PopulateCommandMenu(cmsCommand.Items, GenericMenu.CommandManager.Commands.Values.ToList, AddressOf SetCommand)

            tv.SelectedNode = tv.Nodes(0)

            TipProvider.SetTip("Parameters used when the command is executed. Please make a feature request if useful parameters are missing.", pg, lParameters)
            TipProvider.SetTip("Text to be displayed. Enter minus to create a separator.", tbText, laText)
            TipProvider.SetTip("A key can be deleted by pressing it two times.", tbHotkey, laHotkey)
            TipProvider.SetTip("Command to be executed. Please make a feature request if useful commands are missing.", tbCommand, laCommand)
        End Sub

        Sub SetCommand(c As Command)
            tbCommand.Text = c.MethodInfo.Name
        End Sub

        Sub UpdateControls()
            Dim n As TreeNode = tv.SelectedNode

            If Not Block AndAlso n IsNot Nothing Then
                Block = True
                Dim item = CType(n.Tag, CustomMenuItem)
                tbText.Text = item.Text
                tbHotkey.Text = KeysHelp.GetKeyString(item.KeyData)
                Dim found As Boolean

                For Each i As Command In GenericMenu.CommandManager.Commands.Values
                    If i.MethodInfo.Name = item.MethodName Then
                        If tbCommand.Text = i.MethodInfo.Name Then
                            tbCommand_TextChanged()
                        Else
                            tbCommand.Text = i.MethodInfo.Name
                        End If

                        found = True
                    End If
                Next

                If Not found Then
                    tbCommand.Text = ""
                End If

                Dim notRoot = n.Parent IsNot Nothing

                bnCommand.Enabled = notRoot
                tbCommand.Enabled = notRoot
                tbHotkey.Enabled = notRoot
                tsbCopy.Enabled = notRoot
                tsbCut.Enabled = notRoot
                tsbMoveDown.Enabled = notRoot
                tsbMoveLeft.Enabled = notRoot
                tsbMoveRight.Enabled = notRoot
                tsbMoveUp.Enabled = notRoot
                tsbPaste.Enabled = ClipboardNode IsNot Nothing
                tsbRemove.Enabled = notRoot
                tbText.Enabled = notRoot

                Block = False
            End If
        End Sub

        Sub tbText_TextChanged() Handles tbText.TextChanged
            If Not Block AndAlso tv.SelectedNode IsNot Nothing Then
                Dim item = DirectCast(tv.SelectedNode.Tag, CustomMenuItem)

                tbCommand.Enabled = tbText.Text <> "-"
                bnCommand.Enabled = tbText.Text <> "-"
                tbHotkey.Enabled = tbText.Text <> "-"

                If tbText.Text = "-" Then
                    tbCommand.Text = ""
                    item.KeyData = Keys.None
                End If

                item.Text = tbText.Text
                tv.SelectedNode.Text = item.Text
            End If
        End Sub

        Sub PopulateGrid(item As CustomMenuItem)
            GridTypeDescriptor.Items.Clear()

            If GenericMenu.CommandManager.HasCommand(item.MethodName) Then
                Dim c = GenericMenu.CommandManager.GetCommand(item.MethodName)

                If c.MethodInfo IsNot Nothing Then
                    Dim params = c.MethodInfo.GetParameters

                    For i = 0 To params.Length - 1
                        Dim gp As New GridProperty
                        gp.Name = DispNameAttribute.GetValue(params(i).GetCustomAttributes(False))
                        If gp.Name Is Nothing Then gp.Name = params(i).Name.ToTitleCase
                        gp.Value = c.FixParameters(item.Parameters)(i)
                        gp.Description = DescriptionAttributeHelp.GetDescription(params(i).GetCustomAttributes(False))
                        gp.TypeEditor = EditorAttributeHelp.GetEditor(params(i).GetCustomAttributes(False))

                        GridTypeDescriptor.Add(gp)
                    Next
                End If
            End If

            pg.SelectedObject = GridTypeDescriptor
        End Sub

        Sub pg_PropertyValueChanged() Handles pg.PropertyValueChanged
            If tv.SelectedNode IsNot Nothing Then
                Dim item = DirectCast(tv.SelectedNode.Tag, CustomMenuItem)
                item.Parameters.Clear()

                For Each i As DictionaryEntry In GridTypeDescriptor.Items
                    item.Parameters.Add(DirectCast(i.Value, GridProperty).Value)
                Next
            End If
        End Sub

        Sub AddNodes(node As TreeNode, list As ArrayList)
            For Each i As TreeNode In node.Nodes
                list.Add(i)
                AddNodes(i, list)
            Next
        End Sub

        Sub PopulateTreeView(item As CustomMenuItem, node As TreeNode)
            Dim newNode As New TreeNode(item.Text) With {
                .Tag = item
            }

            If node Is Nothing Then
                tv.Nodes.Add(newNode)
            Else
                node.Nodes.Add(newNode)
            End If

            For Each i As CustomMenuItem In item.SubItems
                PopulateTreeView(i, newNode)
            Next
        End Sub

        Function GetState() As CustomMenuItem
            Dim item = DirectCast(tv.Nodes(0).Tag, CustomMenuItem)
            BuildState(item, tv.Nodes(0))
            Return item
        End Function

        Sub BuildState(item As CustomMenuItem, node As TreeNode)
            item.SubItems.Clear()

            For Each i As TreeNode In node.Nodes
                Dim subItem As CustomMenuItem = CType(i.Tag, CustomMenuItem)
                item.SubItems.Add(subItem)
                BuildState(subItem, i)
            Next
        End Sub

        Protected Overridable Function GetTemplateForm(item As CustomMenuItem) As MenuTemplateForm
            Return New MenuTemplateForm(item)
        End Function

        Sub NewFromDefaultsToolStripMenuItem_Click() Handles NewFromDefaultsToolStripMenuItem.Click
            If tv.SelectedNode IsNot Nothing Then
                Dim f = GetTemplateForm(GenericMenu.DefaultMenu.Invoke)

                If f.ShowDialog = DialogResult.OK Then
                    Dim n = CloneNode(f.TreeNode)
                    tv.SelectedNode.Nodes.Add(n)
                    tv.SelectedNode.ExpandAll()
                    tv.SelectedNode = n
                End If
            End If
        End Sub

        Sub RemoveSelectedItem()
            If tv.SelectedNode IsNot Nothing AndAlso
                tv.SelectedNode.Parent IsNot Nothing Then

                tv.SelectedNode.Remove()
            End If
        End Sub

        Sub tv_KeyUp(sender As Object, e As KeyEventArgs) Handles tv.KeyUp
            UpdateControls()
        End Sub

        Shared Function CreateNode(item As CustomMenuItem) As TreeNode
            Dim ret As New TreeNode(item.Text) With {.Tag = item}

            For Each i As CustomMenuItem In item.SubItems
                ret.Nodes.Add(CreateNode(i))
            Next

            Return ret
        End Function

        Function CloneNode(node As TreeNode) As TreeNode
            Dim item = DirectCast(node.Tag, CustomMenuItem)
            BuildState(item, node)
            Return CreateNode(item.GetClone)
        End Function

        Sub tbHotkey_KeyDown(sender As Object, e As KeyEventArgs) Handles tbHotkey.KeyDown
            Dim isModifier = e.Key = Keys.LeftControl OrElse e.Key = Keys.RightControl OrElse
                e.Key = Keys.LeftAlt OrElse e.Key = Keys.RightAlt OrElse
                e.Key = Keys.LeftShift OrElse e.Key = Keys.RightShift OrElse
                e.Key = Keys.LeftApplication OrElse e.Key = Keys.RightApplication

            If Not Block AndAlso tv.SelectedNode IsNot Nothing AndAlso Not isModifier Then

                Dim item = DirectCast(tv.SelectedNode.Tag, CustomMenuItem)

                If item.KeyData = e.KeyData Then
                    item.KeyData = Keys.None
                    tbHotkey.Text = KeysHelp.GetKeyString(item.KeyData)
                Else
                    item.KeyData = e.KeyData
                    tbHotkey.Text = KeysHelp.GetKeyString(item.KeyData)

                    For Each i In tv.GetNodes
                        If TypeOf i.Tag Is CustomMenuItem Then
                            Dim current = DirectCast(i.Tag, CustomMenuItem)

                            If current.KeyData = item.KeyData AndAlso item IsNot current Then
                                current.KeyData = Keys.None
                                MsgInfo(KeysHelp.GetKeyString(item.KeyData) + " detached from " + current.Text.TrimEnd("."c) + " and assigned to " + item.Text.TrimEnd("."c) + " instead.")
                            End If
                        End If
                    Next
                End If
            End If

            e.Handled = True
        End Sub

        Sub tv_AfterSelect() Handles tv.AfterSelect
            UpdateControls()
        End Sub

        Sub tsbCut_Click() Handles tsbCut.Click
            If tv.SelectedNode IsNot Nothing Then
                tsbCopy.PerformClick()
                RemoveSelectedItem()
            End If
        End Sub

        Sub tsbCopy_Click() Handles tsbCopy.Click
            If tv.SelectedNode IsNot Nothing Then
                ClipboardNode = CloneNode(tv.SelectedNode)
                UpdateControls()
            End If
        End Sub

        Sub tsbPaste_Click() Handles tsbPaste.Click
            If tv.SelectedNode IsNot Nothing Then
                tv.SelectedNode.Nodes.Add(CloneNode(ClipboardNode))
                tv.SelectedNode.ExpandAll()
            End If
        End Sub

        Sub tsbRemove_Click() Handles tsbRemove.Click
            RemoveSelectedItem()
        End Sub

        Sub ResetToolStripMenuItem_Click() Handles ResetToolStripMenuItem.Click
            If MsgOK("Please confirm to reset the entire menu.") Then
                tv.BeginUpdate()
                tv.Nodes.Clear()
                PopulateTreeView(GenericMenu.DefaultMenu.Invoke, Nothing)
                tv.EndUpdate()
                tv.ExpandAll()
                tv.SelectedNode = tv.Nodes(0)
            End If
        End Sub

        Sub tsbMoveLeft_Click() Handles tsbMoveLeft.Click
            Dim n = tv.SelectedNode

            If n IsNot Nothing AndAlso n.Parent.Parent Is Nothing Then
                Exit Sub
            End If

            Block = True
            tv.MoveSelectedNodeLeft()
            Block = False
        End Sub

        Sub tsbMoveRight_Click() Handles tsbMoveRight.Click
            Block = True
            tv.MoveSelectedNodeRight()
            Block = False
        End Sub

        Sub tsbMoveUp_Click() Handles tsbMoveUp.Click
            Dim n = tv.SelectedNode

            If n IsNot Nothing AndAlso
                n.Parent IsNot Nothing AndAlso
                n.Parent.Parent Is Nothing AndAlso
                n.Index = 0 Then

                Exit Sub
            End If

            Block = True
            tv.MoveSelectedNodeUp()
            Block = False
        End Sub

        Sub tsbMoveDown_Click() Handles tsbMoveDown.Click
            Dim n = tv.SelectedNode

            If n IsNot Nothing AndAlso
                n.Parent.Parent Is Nothing AndAlso
                n.NextNode Is Nothing Then

                Exit Sub
            End If

            Block = True
            tv.MoveSelectedNodeDown()
            Block = False
        End Sub

        Sub bCommand_Click() Handles bnCommand.Click
            cmsCommand.Show(bnCommand, 0, bnCommand.Height)
        End Sub

        Sub tbCommand_TextChanged() Handles tbCommand.TextChanged
            If tv.SelectedNode IsNot Nothing Then
                Dim item = DirectCast(tv.SelectedNode.Tag, CustomMenuItem)

                If Not Block Then
                    Dim selectedCommand As Command = Nothing

                    For Each i As Command In GenericMenu.CommandManager.Commands.Values
                        If i.MethodInfo.Name = tbCommand.Text Then
                            selectedCommand = i
                        End If
                    Next

                    If selectedCommand Is Nothing OrElse selectedCommand.MethodInfo Is Nothing Then
                        item.MethodName = ""
                        item.Parameters = Nothing
                    Else
                        item.MethodName = selectedCommand.MethodInfo.Name
                        item.Parameters = selectedCommand.GetDefaultParameters
                    End If
                End If

                Dim mi As MethodInfo = Nothing

                If GenericMenu.CommandManager.HasCommand(item.MethodName) Then
                    mi = GenericMenu.CommandManager.GetCommand(item.MethodName).MethodInfo
                End If

                pg.Visible = mi IsNot Nothing AndAlso mi.GetParameters.Length > 0
                lParameters.Visible = pg.Visible
                PopulateGrid(item)
            End If
        End Sub

        Sub tsbNew_Click(sender As Object, e As EventArgs) Handles tsbNew.Click
            If tv.SelectedNode IsNot Nothing Then
                Dim newNode As New TreeNode With {
                    .Text = "???",
                    .Tag = New CustomMenuItem("???")
                }
                tv.SelectedNode.Nodes.Add(newNode)
                tv.SelectedNode = newNode
            End If
        End Sub

        Protected Overrides Sub OnClosing(args As ComponentModel.CancelEventArgs)
            MyBase.OnClosing(args)
            IsClosing = True
        End Sub

        Sub bnOK_Click(sender As Object, e As EventArgs) Handles bnOK.Click
            IsClosing = True
        End Sub

        Sub bnCancel_Click(sender As Object, e As EventArgs) Handles bnCancel.Click
            IsClosing = True
        End Sub

        Sub CustomMenuEditor_HelpRequested(sender As Object, e As EventArgs) Handles Me.HelpRequested
            Dim form As New HelpForm()
            form.Doc.WriteStart(Text)
            form.Doc.WriteParagraph("The menu editor allows to customize the text, location, shortcut key and command of a menu item. Menu items can be rearranged with the move buttons.")
            form.Doc.WriteTable("Commands", GenericMenu.CommandManager.GetTips)
            form.Show()
        End Sub
    End Class
End Namespace
