
Imports StaxRip.UI

Namespace UI
    Public Enum FlowDirection
        LeftToRight
        TopDown
        RightToLeft
        BottomUp
    End Enum

    'Panel that stacks controls vertically or horizontally, replacement for the WinForms FlowLayoutPanel
    Public Class FlowLayoutPanelEx
        Inherits Panel

        Protected Stack As New StackLayout
        Private FlowDirectionValue As FlowDirection = FlowDirection.LeftToRight
        Private MarginValue As Padding

        Shadows ReadOnly Property Controls As New FlowControlCollection(Me)

        Property UseParentWidth As Boolean
        Property AutomaticOffset As Boolean
        Property AutoSize As Boolean = True
        Property WrapContents As Boolean

        Sub New()
            Stack.Spacing = CInt(FontHeight / 4)
            UpdateOrientation()
            InitContent()
        End Sub

        Protected Overridable Sub InitContent()
            Content = Stack
        End Sub

        Property FlowDirection As FlowDirection
            Get
                Return FlowDirectionValue
            End Get
            Set(value As FlowDirection)
                FlowDirectionValue = value
                UpdateOrientation()
            End Set
        End Property

        Property Margin As Padding
            Get
                Return MarginValue
            End Get
            Set(value As Padding)
                MarginValue = value
                Padding = value
            End Set
        End Property

        ReadOnly Property IsVertical As Boolean
            Get
                Return FlowDirectionValue = FlowDirection.TopDown OrElse FlowDirectionValue = FlowDirection.BottomUp
            End Get
        End Property

        Sub UpdateOrientation()
            If IsVertical Then
                Stack.Orientation = Orientation.Vertical
                Stack.HorizontalContentAlignment = HorizontalAlignment.Stretch
            Else
                Stack.Orientation = Orientation.Horizontal
                Stack.VerticalContentAlignment = VerticalAlignment.Center
            End If
        End Sub

        Friend Sub AddItem(c As Control)
            Dim expand = False
            Dim simple = TryCast(c, ISimpleUIControl)

            If simple IsNot Nothing AndAlso simple.Expand AndAlso Not IsVertical Then
                expand = True
            End If

            Stack.Items.Add(New StackLayoutItem(c, expand))
            OnItemAdded()
        End Sub

        Protected Overridable Sub OnItemAdded()
        End Sub

        Friend Sub RemoveItem(c As Control)
            For Each i In Stack.Items.ToArray
                If i.Control Is c Then
                    Stack.Items.Remove(i)
                End If
            Next
        End Sub

        Friend Sub ClearItems()
            Stack.Items.Clear()
        End Sub

        'updates the expand state after the Expand property of a child was changed
        Sub UpdateExpand()
            Dim changed = False

            For Each i In Stack.Items
                Dim simple = TryCast(i.Control, ISimpleUIControl)

                If simple IsNot Nothing AndAlso i.Expand <> (simple.Expand AndAlso Not IsVertical) Then
                    i.Expand = simple.Expand AndAlso Not IsVertical
                    changed = True
                End If
            Next

            'a loaded stack layout ignores changes of its items, so they are added again
            If changed AndAlso Stack.Loaded Then
                Dim items = Stack.Items.Select(Function(i) New StackLayoutItem(i.Control, i.Expand) With {
                    .HorizontalAlignment = i.HorizontalAlignment, .VerticalAlignment = i.VerticalAlignment}).ToArray
                Stack.Items.Clear()

                For Each i In items
                    Stack.Items.Add(i)
                Next
            End If
        End Sub

        Sub PerformLayout()
        End Sub
    End Class

    Public Class FlowControlCollection
        Implements IEnumerable(Of Control)

        Private ReadOnly Owner As FlowLayoutPanelEx
        Private ReadOnly List As New List(Of Control)

        Sub New(owner As FlowLayoutPanelEx)
            Me.Owner = owner
        End Sub

        Sub Add(c As Control)
            List.Add(c)
            Owner.AddItem(c)
        End Sub

        Sub AddRange(controls As IEnumerable(Of Control))
            For Each i In controls
                Add(i)
            Next
        End Sub

        Sub Remove(c As Control)
            List.Remove(c)
            Owner.RemoveItem(c)
        End Sub

        Sub Clear()
            List.Clear()
            Owner.ClearItems()
        End Sub

        Function Contains(c As Control) As Boolean
            Return List.Contains(c)
        End Function

        Function IndexOf(c As Control) As Integer
            Return List.IndexOf(c)
        End Function

        ReadOnly Property Count As Integer
            Get
                Return List.Count
            End Get
        End Property

        Default ReadOnly Property Item(index As Integer) As Control
            Get
                Return List(index)
            End Get
        End Property

        Function GetEnumerator() As IEnumerator(Of Control) Implements IEnumerable(Of Control).GetEnumerator
            Return List.GetEnumerator()
        End Function

        Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
            Return List.GetEnumerator()
        End Function
    End Class

    Interface ISimpleUIControl
        Property Expand As Boolean
    End Interface
End Namespace

'Builds option dialogs from code: a navigation tree with pages of labeled controls
'that can be bound to fields or properties of the Store object via reflection.
Public Class SimpleUI
    Inherits Panel

    Public WithEvents Tree As New TreeViewEx
    Public Host As New Panel

    Public Event SaveValues()
    Public Event ValueChanged()

    Public Pages As New List(Of IPage)

    Public ActivePage As IPage
    Public Store As Object

    Property FormSizeScaleFactor As SizeF

    Private Layout As TableLayout
    Private TreeCell As TableCell

    Sub New()
        Tree.AutoCollaps = True
        Tree.ExpandMode = TreeNodeExpandMode.InclusiveChilds

        TreeCell = New TableCell(Tree)
        Layout = New TableLayout With {.Spacing = New Size(FontHeight \ 2, 0)}
        Layout.Rows.Add(New TableRow(TreeCell, New TableCell(Host, True)) With {.ScaleHeight = True})
        Content = Layout
    End Sub

    Protected Overrides Sub OnPreLoad(e As EventArgs)
        MyBase.OnPreLoad(e)
        UpdateTree()
    End Sub

    Sub UpdateTree()
        Dim nodes = Tree.GetNodes()
        Tree.Visible = nodes.Count > 1

        If nodes.Count > 1 Then
            Dim maxWidth = nodes.Max(Function(n) MeasureTextWidth(n.Text) + n.Level * FontHeight)
            Tree.Width = maxWidth + FontHeight * 3
        End If
    End Sub

    Sub Save()
        RaiseEvent SaveValues()
    End Sub

    Sub RaiseChangeEvent()
        RaiseEvent ValueChanged()
    End Sub

    Sub Tree_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles Tree.AfterSelect
        Dim node = e.Node
        Dim page = Pages.FirstOrDefault(Function(i) i.Node Is node)

        If page IsNot Nothing Then
            ShowPageControl(page)
        ElseIf node.Nodes.Count > 0 Then
            Tree.SelectedNode = node.Nodes(0)
        End If
    End Sub

    Private Sub ShowPageControl(page As IPage)
        ActivePage = page
        Dim flow = TryCast(page, FlowPage)
        flow?.AlignLabels()
        Host.Content = DirectCast(page, Control)
    End Sub

    Sub ShowPage(pagePath As String)
        For Each i In Pages
            If i.Path.Replace(" ", "") = pagePath.Replace(" ", "") Then
                ShowPage(i)
                Exit For
            End If
        Next
    End Sub

    Sub ShowPage(page As IPage)
        For Each i In Pages
            If page Is i Then
                ShowPageControl(i)

                If Tree.SelectedNode IsNot i.Node Then
                    Tree.SelectedNode = i.Node
                End If
            End If
        Next
    End Sub

    Sub SelectLast(id As String)
        Dim last = s.Storage.GetString(id)

        If last <> "" AndAlso Pages.Any(Function(x) x.Path = last) Then
            ShowPage(last)
        ElseIf Pages.Count > 0 Then
            ShowPage(Pages(0))
        End If
    End Sub

    Sub SaveLast(id As String)
        If ActivePage IsNot Nothing Then
            s.Storage.SetString(id, ActivePage.Path)
        End If
    End Sub

    Function GetActiveFlowPage() As FlowPage
        If ActivePage Is Nothing Then
            ActivePage = CreateFlowPage("main page")
        End If

        Return DirectCast(ActivePage, FlowPage)
    End Function

    Function GetFlowPage(path As String) As FlowPage
        If path = "" Then
            path = "unknown"
        End If

        Dim page = Pages.FirstOrDefault(Function(i) i.Path = path)

        If page IsNot Nothing Then
            Return DirectCast(page, FlowPage)
        End If

        Return CreateFlowPage(path)
    End Function

    Function AddBool(Optional parent As FlowLayoutPanelEx = Nothing) As SimpleUICheckBox
        If parent Is Nothing Then parent = GetActiveFlowPage()
        Dim ret As New SimpleUICheckBox(Me)
        AddHandler SaveValues, AddressOf ret.Save
        parent.Controls.Add(ret)
        Return ret
    End Function

    Function AddEdit(parent As FlowLayoutPanelEx) As SimpleUITextEdit
        Dim ret As New SimpleUITextEdit(Me)
        parent.Controls.Add(ret)
        Return ret
    End Function

    Function AddNumeric(Optional parent As FlowLayoutPanelEx = Nothing) As SimpleUINumEdit
        If parent Is Nothing Then parent = GetActiveFlowPage()
        Dim ret As New SimpleUINumEdit(Me)
        AddHandler SaveValues, AddressOf ret.Save
        parent.Controls.Add(ret)
        Return ret
    End Function

    Function AddLabel(text As String) As SimpleUILabel
        Return AddLabel(Nothing, text, 0, {})
    End Function

    Function AddLabel(text As String, widthInFontHeights As Integer) As SimpleUILabel
        Return AddLabel(Nothing, text, widthInFontHeights, {})
    End Function

    Function AddLabel(text As String, ParamArray fontStyles() As FontStyle) As SimpleUILabel
        Return AddLabel(Nothing, text, 0, fontStyles)
    End Function

    Function AddLabel(text As String, widthInFontHeights As Integer, ParamArray fontStyles() As FontStyle) As SimpleUILabel
        Return AddLabel(Nothing, text, widthInFontHeights, fontStyles)
    End Function

    Function AddLabel(parent As FlowLayoutPanelEx, text As String) As SimpleUILabel
        Return AddLabel(parent, text, 0, {})
    End Function

    Function AddLabel(parent As FlowLayoutPanelEx, text As String, ParamArray fontStyles() As FontStyle) As SimpleUILabel
        Return AddLabel(parent, text, 0, fontStyles)
    End Function

    Function AddLabel(parent As FlowLayoutPanelEx, text As String, widthInFontHeights As Integer, ParamArray fontStyles() As FontStyle) As SimpleUILabel
        If parent Is Nothing Then parent = GetActiveFlowPage()

        Dim ret As New SimpleUILabel
        ret.Offset = widthInFontHeights
        ret.Text = text

        If fontStyles?.Length > 0 Then
            ret.Font = FontManager.GetDefaultFont(0, fontStyles.Aggregate(FontStyle.None, Function(a, n) a Or n))
        End If

        parent.Controls.Add(ret)
        Return ret
    End Function

    Function AddNum(Optional parent As FlowLayoutPanelEx = Nothing) As NumBlock
        If parent Is Nothing Then parent = GetActiveFlowPage()
        Dim ret As New NumBlock(Me)
        AddHandler SaveValues, AddressOf ret.NumEdit.Save
        parent.Controls.Add(ret)
        Return ret
    End Function

    Function AddText(Optional parent As FlowLayoutPanelEx = Nothing) As TextBlock
        If parent Is Nothing Then parent = GetActiveFlowPage()
        Dim ret As New TextBlock(Me)
        parent.Controls.Add(ret)
        Return ret
    End Function

    Function AddTextMenu(Optional parent As FlowLayoutPanelEx = Nothing) As TextMenuBlock
        If parent Is Nothing Then parent = GetActiveFlowPage()
        Dim ret As New TextMenuBlock(Me)
        parent.Controls.Add(ret)
        Return ret
    End Function

    Function AddColorPicker(Optional parent As FlowLayoutPanelEx = Nothing) As ColorPickerBlock
        If parent Is Nothing Then parent = GetActiveFlowPage()
        Dim ret As New ColorPickerBlock(Me)
        AddHandler SaveValues, AddressOf ret.Save
        parent.Controls.Add(ret)
        Return ret
    End Function

    Function AddButton(Optional parent As FlowLayoutPanelEx = Nothing) As ButtonBlock
        If parent Is Nothing Then parent = GetActiveFlowPage()
        Dim ret As New ButtonBlock(Me)
        parent.Controls.Add(ret)
        Return ret
    End Function

    Function AddTextButton(Optional parent As FlowLayoutPanelEx = Nothing) As TextButtonBlock
        If parent Is Nothing Then parent = GetActiveFlowPage()
        Dim ret As New TextButtonBlock(Me)
        parent.Controls.Add(ret)
        Return ret
    End Function

    Function AddMenu(Of T)(Optional parent As FlowLayoutPanelEx = Nothing) As MenuBlock(Of T)
        If parent Is Nothing Then parent = GetActiveFlowPage()
        Dim ret As New MenuBlock(Of T)(Me)
        AddHandler SaveValues, AddressOf ret.Button.Save
        parent.Controls.Add(ret)
        Return ret
    End Function

    Function AddEmptyBlock(Optional parent As FlowLayoutPanelEx = Nothing) As EmptyBlock
        If parent Is Nothing Then parent = GetActiveFlowPage()
        Dim ret As New EmptyBlock
        parent.Controls.Add(ret)
        Return ret
    End Function

    Function AddLine(Optional parent As FlowLayoutPanelEx = Nothing, Optional text As String = "", Optional marginTop As Integer = 12, Optional marginBottom As Integer = 7) As SimpleUILineControl
        If parent Is Nothing Then parent = GetActiveFlowPage()

        Dim line As New SimpleUILineControl With {
            .Text = text,
            .Expand = True
        }

        Dim wrapper As New Panel With {.Content = line, .Padding = New Padding(0, CInt(marginTop * FontHeight / 16), 0, CInt(marginBottom * FontHeight / 16))}
        parent.Controls.Add(wrapper)
        Return line
    End Function

    Function CreateFlowPage(Optional path As String = "-", Optional autoSuspend As Boolean = False) As FlowPage
        Dim ret = New FlowPage
        Pages.Add(ret)
        ret.Path = path
        ret.Node = Tree.AddNode(path)
        ActivePage = ret

        If Host.Content Is Nothing Then
            Host.Content = ret
        End If

        Return ret
    End Function

    'removes all pages and the save handlers of their controls
    Sub ClearPages()
        SaveValuesEvent = Nothing
        Pages.Clear()
        Tree.Nodes.Clear()
        Host.Content = Nothing
        ActivePage = Nothing
    End Sub

    Function CreateDataPage(path As String) As DataPage
        Dim ret = New DataPage
        Pages.Add(ret)
        ret.Path = path
        ret.Node = Tree.AddNode(path)
        ActivePage = ret

        If Host.Content Is Nothing Then
            Host.Content = ret
        End If

        Return ret
    End Function

    'page with an editable grid, rows are added and removed with buttons
    Public Class DataPage
        Inherits Panel
        Implements IPage

        Property Node As TreeNode Implements IPage.Node
        Property Path As String Implements IPage.Path
        Property TipProvider As TipProvider Implements IPage.TipProvider
        Property FormSizeScaleFactor As SizeF Implements IPage.FormSizeScaleFactor
        Property NewItemFunc As Func(Of Object)

        Public Grid As New GridView With {.AllowMultipleSelection = False, .GridLines = GridLines.Both}

        Sub New()
            TipProvider = New TipProvider(Nothing)

            Dim bnAdd As New ButtonEx("Add")
            Dim bnRemove As New ButtonEx("Remove")

            bnAdd.ClickAction = Sub()
                                    Dim list = TryCast(Grid.DataStore, IList)

                                    If list IsNot Nothing AndAlso NewItemFunc IsNot Nothing Then
                                        list.Add(NewItemFunc())
                                    End If
                                End Sub

            bnRemove.ClickAction = Sub()
                                       Dim list = TryCast(Grid.DataStore, IList)

                                       If list IsNot Nothing AndAlso Grid.SelectedItem IsNot Nothing Then
                                           list.Remove(Grid.SelectedItem)
                                       End If
                                   End Sub

            Content = New TableLayout(
                New TableRow(Grid) With {.ScaleHeight = True},
                New TableRow(HRow(bnAdd, bnRemove))) With {.Spacing = DefaultSpacing}
        End Sub
    End Class

    Function AddControlPage(ctrl As IPage, path As String) As IPage
        Pages.Add(ctrl)
        ctrl.Path = path
        ctrl.Node = Tree.AddNode(path)

        Dim savable = TryCast(ctrl, ISaveValues)

        If savable IsNot Nothing Then
            AddHandler SaveValues, AddressOf savable.SaveValues
        End If

        If Host.Content Is Nothing Then
            Host.Content = DirectCast(ctrl, Control)
        End If

        Return ctrl
    End Function

    Public Class FlowPage
        Inherits FlowLayoutPanelEx
        Implements IPage

        Property Node As TreeNode Implements IPage.Node
        Property Path As String Implements IPage.Path
        Property TipProvider As TipProvider Implements IPage.TipProvider
        Property AutoSuspend As Boolean
        Property FormSizeScaleFactor As SizeF Implements IPage.FormSizeScaleFactor

        Private LabelsAligned As Boolean
        Private AlignPending As Boolean

        Sub New()
            MyBase.New()
            TipProvider = New TipProvider(Nothing)
            FlowDirection = FlowDirection.TopDown
            Stack.Spacing = CInt(FontHeight / 6)
        End Sub

        Protected Overrides Sub InitContent()
            Dim scroll As New Scrollable With {.Content = Stack, .Border = BorderType.None, .ExpandContentWidth = True, .ExpandContentHeight = False}

            'only vertical scrolling, so the minimum width of the page content is respected
            TryCast(scroll.ControlObject, Global.Gtk.ScrolledWindow)?.SetPolicy(Global.Gtk.PolicyType.Never, Global.Gtk.PolicyType.Automatic)
            Content = scroll
        End Sub

        Protected Overrides Sub OnLoadComplete(e As EventArgs)
            MyBase.OnLoadComplete(e)
            AlignLabels()
        End Sub

        'pages can get items after they were shown, the texts are set after adding
        Protected Overrides Sub OnItemAdded()
            LabelsAligned = False

            If Loaded AndAlso Not AlignPending Then
                AlignPending = True
                Application.Instance.AsyncInvoke(Sub()
                                                     AlignPending = False
                                                     AlignLabels()
                                                 End Sub)
            End If
        End Sub

        'gives all labels of label blocks the same width so the controls are aligned
        Sub AlignLabels()
            If LabelsAligned Then
                Exit Sub
            End If

            LabelsAligned = True
            Dim labels = Controls.OfType(Of LabelBlock).Select(Function(x) x.Label).Where(Function(x) x.Offset = 0).ToList()

            If labels.Count > 0 Then
                Dim maxWidth = labels.Max(Function(x) MeasureTextWidth(x.Text + " ", x.Font))

                For Each i In labels
                    i.Width = maxWidth
                Next
            End If
        End Sub
    End Class

    Public Class SimpleUILineControl
        Inherits LineControl
        Implements ISimpleUIControl

        Property Expand As Boolean Implements ISimpleUIControl.Expand
    End Class

    Public Class SimpleUICheckBox
        Inherits CheckBoxEx

        Property MarginLeft As Double
        Property ValueChangedAction As Action(Of Boolean)
        Property SaveAction As Action(Of Boolean)
        Property HelpAction As Action
        Property Offset As Integer

        Private SimpleUI As SimpleUI
        Private FieldValue As String
        Private PropertyValue As String

        Sub New(ui As SimpleUI)
            SimpleUI = ui
        End Sub

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)

            If e.Buttons = MouseButtons.Alternate AndAlso HelpAction IsNot Nothing Then
                e.Handled = True
                HelpAction.Invoke
            End If
        End Sub

        Protected Overrides Sub OnCheckedChanged(e As EventArgs)
            MyBase.OnCheckedChanged(e)
            SimpleUI.RaiseChangeEvent()
            ValueChangedAction?.Invoke(Checked)
        End Sub

        Sub Save()
            SaveAction?.Invoke(Checked)

            If Field <> "" Then
                SimpleUI.Store.GetType.GetField(Field).SetValue(SimpleUI.Store, Checked)
            ElseIf [Property] <> "" Then
                SimpleUI.Store.GetType.GetProperty([Property]).SetValue(SimpleUI.Store, Checked)
            End If
        End Sub

        Property Field As String
            Get
                Return FieldValue
            End Get
            Set(value As String)
                Checked = CBool(SimpleUI.Store.GetType.GetField(value).GetValue(SimpleUI.Store))
                FieldValue = value
            End Set
        End Property

        Property [Property] As String
            Get
                Return PropertyValue
            End Get
            Set(value As String)
                Checked = CBool(SimpleUI.Store.GetType.GetProperty(value).GetValue(SimpleUI.Store))
                PropertyValue = value
            End Set
        End Property

        Private TipProviderValue As New TipProvider

        WriteOnly Property Help As String
            Set(value As String)
                If value.StartsWith("http") Then
                    value = $"[{value} {value}]"
                End If

                TipProviderValue.SetTip(value, Me)
            End Set
        End Property
    End Class

    Public Class SimpleUINumEdit
        Inherits NumEdit

        Private SimpleUI As SimpleUI
        Private PropertyValue As String
        Private FieldValue As String

        Property SaveAction As Action(Of Double)

        Sub New(ui As SimpleUI)
            SimpleUI = ui

            'GTK shows the buttons next to the text, so the width is defined in characters
            Dim spin = TryCast(ControlObject, Global.Gtk.SpinButton)
            If spin IsNot Nothing Then spin.WidthChars = 8
        End Sub

        Sub Save()
            SaveAction?.Invoke(Value)

            If Field <> "" Then
                Dim field = SimpleUI.Store.GetType.GetField(Me.Field)
                field.SetValue(SimpleUI.Store, Convert.ChangeType(Value, field.FieldType))
            ElseIf [Property] <> "" Then
                Dim prop = SimpleUI.Store.GetType.GetProperty([Property])
                prop.SetValue(SimpleUI.Store, Convert.ChangeType(Value, prop.PropertyType))
            End If
        End Sub

        Property [Property] As String
            Get
                Return PropertyValue
            End Get
            Set(value As String)
                Me.Value = CDbl(SimpleUI.Store.GetType.GetProperty(value).GetValue(SimpleUI.Store))
                PropertyValue = value
            End Set
        End Property

        Property Field As String
            Get
                Return FieldValue
            End Get
            Set(value As String)
                Me.Value = CDbl(SimpleUI.Store.GetType.GetField(value).GetValue(SimpleUI.Store))
                FieldValue = value
            End Set
        End Property

        Property Config As Double()
            Get
                Return {Minimum, Maximum, Increment, DecimalPlaces}
            End Get
            Set(value As Double())
                If value IsNot Nothing Then
                    If value.Length > 1 AndAlso value(0) = 0 AndAlso value(1) = 0 Then
                        value(0) = Double.MinValue
                        value(1) = Double.MaxValue
                    End If

                    If value.Length > 3 Then DecimalPlaces = CInt(value(3))
                    If value.Length > 0 Then Minimum = Math.Max(value(0), -9000000000000.0)
                    If value.Length > 1 Then Maximum = Math.Min(value(1), 9000000000000.0)
                    If value.Length > 2 Then Increment = value(2)
                End If
            End Set
        End Property

        Protected Overrides Sub OnValueChanged(numEdit As NumEdit)
            MyBase.OnValueChanged(numEdit)
            SimpleUI.RaiseChangeEvent()
        End Sub
    End Class

    Public Class SimpleUIButton
        Inherits ButtonEx
        Implements ISimpleUIControl

        Property Expand As Boolean Implements ISimpleUIControl.Expand
    End Class

    Public Class SimpleUITextEdit
        Inherits TextEdit
        Implements ISimpleUIControl

        Private ExpandValue As Boolean
        Private WidthFactorValue As Integer = 10
        Private MultilineHeightFactorValue As Single = 4
        Private FieldValue As String
        Private PropertyValue As String

        Property TextChangedAction As Action(Of String)
        Property SaveAction As Action(Of String)
        Property SimpleUI As SimpleUI

        Sub New(ui As SimpleUI)
            SimpleUI = ui
            UpdateSize()
            AddHandler TextChanged, Sub()
                                        SimpleUI.RaiseChangeEvent()
                                        TextChangedAction?.Invoke(Text)
                                    End Sub
            AddHandler SimpleUI.SaveValues, AddressOf Save
        End Sub

        Property Expand As Boolean Implements ISimpleUIControl.Expand
            Get
                Return ExpandValue
            End Get
            Set(value As Boolean)
                ExpandValue = value
                UpdateSize()
                TryCast(Parent, FlowLayoutPanelEx)?.UpdateExpand()

                If Parent IsNot Nothing Then
                    TryCast(Parent.Parent, FlowLayoutPanelEx)?.UpdateExpand()
                End If
            End Set
        End Property

        Property WidthFactor As Integer
            Get
                Return WidthFactorValue
            End Get
            Set(value As Integer)
                WidthFactorValue = value
                UpdateSize()
            End Set
        End Property

        Property MultilineHeightFactor As Single
            Get
                Return MultilineHeightFactorValue
            End Get
            Set(value As Single)
                MultilineHeightFactorValue = value
                Multiline = True
                UpdateSize()
            End Set
        End Property

        Sub UpdateSize()
            If Multiline Then
                Height = CInt(FontHeight * MultilineHeightFactorValue)
                Width = -1
            Else
                Width = If(ExpandValue, -1, FontHeight * WidthFactorValue)
            End If
        End Sub

        Sub Save()
            SaveAction?.Invoke(Text)

            If Field <> "" Then
                SimpleUI.Store.GetType.GetField(Field).SetValue(SimpleUI.Store, Text)
            ElseIf [Property] <> "" Then
                SimpleUI.Store.GetType.GetProperty([Property]).SetValue(SimpleUI.Store, Text)
            End If
        End Sub

        Property Field As String
            Get
                Return FieldValue
            End Get
            Set(value As String)
                Text = CStr(SimpleUI.Store.GetType.GetField(value).GetValue(SimpleUI.Store))
                FieldValue = value
            End Set
        End Property

        Property [Property] As String
            Get
                Return PropertyValue
            End Get
            Set(value As String)
                Text = CStr(SimpleUI.Store.GetType.GetProperty(value).GetValue(SimpleUI.Store))
                PropertyValue = value
            End Set
        End Property

        WriteOnly Property UseCommandlineEditor As Boolean
            Set(value As Boolean)
                If value Then
                    AddHandler MouseDown, Sub() EditCommandline()
                End If
            End Set
        End Property

        WriteOnly Property UseMacroEditor As Boolean
            Set(value As Boolean)
                If value Then
                    AddHandler Click, Sub() EditMacro()
                End If
            End Set
        End Property

        Sub EditCommandline()
            Using form As New MacroEditorDialog
                form.SetBatchDefaults()
                form.MacroEditorControl.Value = Text

                If form.ShowDialog() = DialogResult.OK Then
                    Text = form.MacroEditorControl.Value
                End If
            End Using
        End Sub

        Sub EditMacro()
            Using dialog As New MacroEditorDialog
                dialog.SetMacroDefaults()
                dialog.MacroEditorControl.Value = Text

                If dialog.ShowDialog() = DialogResult.OK Then
                    Text = dialog.MacroEditorControl.Value
                End If
            End Using
        End Sub
    End Class

    Public Class SimpleUIMenuButton(Of T)
        Inherits MenuButton
        Implements ISimpleUIControl

        Property Expand As Boolean Implements ISimpleUIControl.Expand
        Property ValueChangedAction As Action(Of T)
        Property SaveAction As Action(Of T)
        Property HelpAction As Action

        Private SimpleUI As SimpleUI
        Private PropertyValue As String
        Private FieldValue As String

        Sub New(ui As SimpleUI)
            SimpleUI = ui
            Width = FontHeight * 14
        End Sub

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)

            If e.Buttons = MouseButtons.Alternate AndAlso HelpAction IsNot Nothing Then
                e.Handled = True
                HelpAction.Invoke
            End If
        End Sub

        Protected Overrides Sub OnValueChanged(value As Object)
            MyBase.OnValueChanged(value)
            SimpleUI.RaiseChangeEvent()
            ValueChangedAction?.Invoke(CType(value, T))
        End Sub

        Shadows Property Value As T
            Get
                Return CType(MyBase.Value, T)
            End Get
            Set(value As T)
                MyBase.Value = value
            End Set
        End Property

        Sub Save()
            SaveAction?.Invoke(Value)

            If Field <> "" Then
                Dim field = SimpleUI.Store.GetType.GetField(Me.Field)
                field.SetValue(SimpleUI.Store, Convert.ChangeType(Value, field.FieldType))
            ElseIf [Property] <> "" Then
                Dim prop = SimpleUI.Store.GetType.GetProperty([Property])
                prop.SetValue(SimpleUI.Store, Convert.ChangeType(Value, prop.PropertyType))
            End If
        End Sub

        Property [Property] As String
            Get
                Return PropertyValue
            End Get
            Set(value As String)
                Me.Value = DirectCast(SimpleUI.Store.GetType.GetProperty(value).GetValue(SimpleUI.Store), T)
                PropertyValue = value
            End Set
        End Property

        Property Field As String
            Get
                Return FieldValue
            End Get
            Set(value As String)
                Me.Value = DirectCast(SimpleUI.Store.GetType.GetField(value).GetValue(SimpleUI.Store), T)
                FieldValue = value
            End Set
        End Property
    End Class

    Public Class SimpleUILabel
        Inherits LabelEx

        Private OffsetValue As Double
        Private TipProviderValue As New TipProvider

        Property MarginTop As Integer
        Property HelpAction As Action

        Property Offset As Double
            Get
                Return OffsetValue
            End Get
            Set(value As Double)
                OffsetValue = value

                If value > 0 Then
                    Width = Math.Max(CInt(value * FontHeight), MeasureTextWidth(Text + " ", Font))
                End If
            End Set
        End Property

        WriteOnly Property Help As String
            Set(value As String)
                If value.StartsWith("http") Then
                    value = $"[{value} {value}]"
                End If

                TipProviderValue.SetTip(value, Me)
            End Set
        End Property

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)

            If e.Buttons = MouseButtons.Alternate AndAlso HelpAction IsNot Nothing Then
                e.Handled = True
                HelpAction.Invoke
            End If
        End Sub
    End Class

    Public Class EmptyBlock
        Inherits FlowLayoutPanelEx

        Sub New()
            MyBase.New()
            FlowDirection = FlowDirection.LeftToRight
        End Sub
    End Class

    MustInherit Class LabelBlock
        Inherits EmptyBlock

        Property Label As New SimpleUILabel

        Sub New()
            Controls.Add(Label)
        End Sub

        Shadows Property Text As String
            Get
                Return Label.Text
            End Get
            Set(value As String)
                If value.EndsWith(":") Then
                    Label.Text = value
                ElseIf value <> "" Then
                    Label.Text = value + ":"
                End If
            End Set
        End Property

        Overridable WriteOnly Property Help As String
            Set(value As String)
                Label.Help = value
            End Set
        End Property

        Public WriteOnly Property HelpAction As Action
            Set(value As Action)
                Label.HelpAction = value
            End Set
        End Property
    End Class

    Public Class NumBlock
        Inherits LabelBlock

        Property NumEdit As SimpleUINumEdit

        Sub New(ui As SimpleUI)
            NumEdit = New SimpleUINumEdit(ui)
            Controls.Add(NumEdit)
        End Sub

        Overrides WriteOnly Property Help As String
            Set(value As String)
                Label.Help = value
                NumEdit.Help = value
            End Set
        End Property

        Property Field As String
            Get
                Return NumEdit.Field
            End Get
            Set(value As String)
                NumEdit.Field = value
            End Set
        End Property

        Property [Property] As String
            Get
                Return NumEdit.Property
            End Get
            Set(value As String)
                NumEdit.Property = value
            End Set
        End Property

        Property Config As Double()
            Get
                Return NumEdit.Config
            End Get
            Set(value As Double())
                NumEdit.Config = value
            End Set
        End Property
    End Class

    Public Class ButtonBlock
        Inherits EmptyBlock

        Property Button As SimpleUIButton

        Sub New(ui As SimpleUI)
            Button = New SimpleUIButton
            Controls.Add(Button)
        End Sub
    End Class

    Public Class TextBlock
        Inherits LabelBlock

        Property Edit As SimpleUITextEdit

        Sub New(ui As SimpleUI)
            Edit = New SimpleUITextEdit(ui)
            Controls.Add(Edit)
        End Sub

        Overridable Property Field As String
            Get
                Return Edit.Field
            End Get
            Set(value As String)
                Edit.Field = value
            End Set
        End Property

        Overridable Property [Property] As String
            Get
                Return Edit.Property
            End Get
            Set(value As String)
                Edit.Property = value
            End Set
        End Property

        Property Expanded As Boolean
            Get
                Return Edit.Expand
            End Get
            Set(value As Boolean)
                Edit.Expand = value
                UpdateExpand()
            End Set
        End Property
    End Class

    Public Class TextMenuBlock
        Inherits TextBlock

        Property Button As New ButtonEx

        Sub New(ui As SimpleUI)
            MyBase.New(ui)
            Button.Text = ""
            Button.ShowMenuSymbol = True
            Button.ContextMenuStrip = New ContextMenuStripEx
            Controls.Add(Button)
            AddHandler Edit.EnabledChanged, Sub() Button.Enabled = Edit.Enabled
        End Sub

        Sub MenuClick(value As String)
            value = Macro.Expand(value)
            Dim tup = Macro.ExpandGUI(value)

            If tup.Cancel Then
                Exit Sub
            End If

            Edit.Text = tup.Value
        End Sub

        Sub AddMenu(menu As String)
            TextCustomMenu.GetMenu(menu, Button, AddressOf MenuClick)
        End Sub

        Function AddMenu(menuText As String, menuValue As String) As MenuItemEx
            Return AddMenu(menuText, Function() menuValue)
        End Function

        Function AddMenu(menuText As String, menuFunc As Func(Of String)) As MenuItemEx
            Dim action = Sub()
                             Dim v = menuFunc.Invoke

                             If v <> "" Then
                                 Edit.Text = v
                             End If
                         End Sub

            Return AddMenu(menuText, action)
        End Function

        Function AddMenu(menuText As String, menuAction As Action) As MenuItemEx
            Return MenuItemEx.Add(Button.ContextMenuStrip.Items, menuText, menuAction)
        End Function
    End Class

    Public Class ColorPickerBlock
        Inherits TextButtonBlock

        Private ColorValue As Color
        Private SimpleUI As SimpleUI
        Private FieldValue As String
        Private PropertyValue As String

        Property SaveAction As Action(Of Color)
        Property ValueChangedAction As Action(Of Color)

        Sub New(ui As SimpleUI)
            MyBase.New(ui)
            SimpleUI = ui
            Edit.ReadOnly = True

            Button.ClickAction = Sub()
                                     Using cd As New ColorDialog
                                         cd.Color = Color

                                         If cd.ShowDialog() = DialogResult.OK Then
                                             Color = cd.Color
                                         End If
                                     End Using
                                 End Sub
        End Sub

        Property Color As Color
            Get
                Return ColorValue
            End Get
            Set(value As Color)
                ColorValue = value

                If value.A = 0 Then
                    Edit.ResetBackColor()
                Else
                    Edit.BackColor = value
                    Edit.ForeColor = If(value.R * 0.299 + value.G * 0.587 + value.B * 0.114 < 0.5, Colors.White, Colors.Black)
                End If

                Edit.Text = If(value.A = 0, "", value.ToHex(False))
                SimpleUI.RaiseChangeEvent()
                ValueChangedAction?.Invoke(value)
            End Set
        End Property

        Shared Function ToStoreValue(value As Color, type As Type) As Object
            If type Is GetType(System.Drawing.Color) Then
                Return If(value.A = 0, System.Drawing.Color.Empty, ToDrawingColor(value))
            ElseIf type Is GetType(ColorHSL) Then
                Return CType(value, ColorHSL)
            End If

            Return value
        End Function

        Shared Function FromStoreValue(value As Object) As Color
            If TypeOf value Is System.Drawing.Color Then
                Dim c = DirectCast(value, System.Drawing.Color)
                Return If(c.IsEmpty, Nothing, ToEtoColor(c))
            ElseIf TypeOf value Is ColorHSL Then
                Return DirectCast(value, ColorHSL).ToColor
            ElseIf TypeOf value Is Color Then
                Return DirectCast(value, Color)
            End If

            Return Nothing
        End Function

        Sub Save()
            SaveAction?.Invoke(Color)

            If Field <> "" Then
                Dim f = SimpleUI.Store.GetType.GetField(Field)
                f.SetValue(SimpleUI.Store, ToStoreValue(Color, f.FieldType))
            ElseIf [Property] <> "" Then
                Dim pi = SimpleUI.Store.GetType.GetProperty([Property])
                pi.SetValue(SimpleUI.Store, ToStoreValue(Color, pi.PropertyType))
            End If
        End Sub

        Overrides Property Field As String
            Get
                Return FieldValue
            End Get
            Set(value As String)
                Color = FromStoreValue(SimpleUI.Store.GetType.GetField(value).GetValue(SimpleUI.Store))
                FieldValue = value
            End Set
        End Property

        Overrides Property [Property] As String
            Get
                Return PropertyValue
            End Get
            Set(value As String)
                Color = FromStoreValue(SimpleUI.Store.GetType.GetProperty(value).GetValue(SimpleUI.Store))
                PropertyValue = value
            End Set
        End Property
    End Class

    Public Class TextButtonBlock
        Inherits TextBlock

        Property Button As New ButtonEx

        Sub New(ui As SimpleUI)
            MyBase.New(ui)
            Button.Text = "..."
            Controls.Add(Button)
            AddHandler Edit.EnabledChanged, Sub() Button.Enabled = Edit.Enabled
        End Sub

        WriteOnly Property ClickAction As Action
            Set(value As Action)
                Button.ClickAction = value
            End Set
        End Property

        Sub BrowseFile(filterTypes As String())
            BrowseFile(FileTypes.GetFilter(filterTypes))
        End Sub

        Sub BrowseFile(filter As String, Optional initDir As String = Nothing)
            Button.ClickAction = Sub()
                                     Using dia As New OpenFileDialog
                                         dia.Filter = filter

                                         If initDir = "" OrElse Not Directory.Exists(initDir) Then
                                             initDir = p.TempDir
                                         End If

                                         dia.SetInitDir(Edit.Text, initDir)

                                         If dia.ShowDialog = DialogResult.OK Then
                                             Edit.Text = dia.FileName
                                         End If
                                     End Using
                                 End Sub
        End Sub

        Sub BrowseFolder()
            Button.ClickAction =
                Sub()
                    Using dialog As New FolderBrowserDialog
                        dialog.SetSelectedPath(If(Edit.Text <> "" AndAlso Directory.Exists(Edit.Text), Edit.Text, s.LastSourceDir))

                        If dialog.ShowDialog = DialogResult.OK Then
                            Edit.Text = dialog.SelectedPath
                        End If
                    End Using
                End Sub
        End Sub
    End Class

    Public Class MenuBlock(Of T)
        Inherits LabelBlock

        Property Button As SimpleUIMenuButton(Of T)

        Sub New(ui As SimpleUI)
            Button = New SimpleUIMenuButton(Of T)(ui)
            Controls.Add(Button)
        End Sub

        Sub Add(items As IEnumerable(Of Object))
            Button.AddRange(items)
        End Sub

        Sub Add(ParamArray items As Object())
            Button.AddRange(items)
        End Sub

        Sub Add(path As String, obj As T)
            Button.Add(path, obj)
        End Sub

        Public Shadows WriteOnly Property Help As String
            Set(value As String)
                Dim tp As New TipProvider
                tp.SetTip(value, Label, Button)
            End Set
        End Property

        Public Shadows WriteOnly Property HelpAction As Action
            Set(value As Action)
                Button.HelpAction = value
                Label.HelpAction = value
            End Set
        End Property

        Property Expanded As Boolean
            Get
                Return Button.Expand
            End Get
            Set(value As Boolean)
                Button.Expand = value
                UpdateExpand()
            End Set
        End Property

        Property Field As String
            Get
                Return Button.Field
            End Get
            Set(value As String)
                Button.Field = value
            End Set
        End Property

        Property [Property] As String
            Get
                Return Button.Property
            End Get
            Set(value As String)
                Button.Property = value
            End Set
        End Property
    End Class
End Class
