
Imports System.ComponentModel

Namespace UI
    <Serializable()>
    Public Class CustomMenuItem
        Sub New()
        End Sub

        Sub New(text As String)
            Me.Text = text
        End Sub

        <NonSerialized()>
        Public CustomMenu As CustomMenu

        <NonSerialized()>
        Public MenuItem As MenuItemEx

        Overridable Property Text As String

        Property SubItems As New List(Of CustomMenuItem)
        Property KeyData As Keys
        Property Symbol As Symbol
        Property MethodName As String

        Private ParametersValue As List(Of Object)

        Property Parameters() As List(Of Object)
            Get
                If ParametersValue Is Nothing Then
                    ParametersValue = New List(Of Object)
                End If

                Return ParametersValue
            End Get
            Set(Value As List(Of Object))
                ParametersValue = Value
            End Set
        End Property

        Sub Add(path As String)
            Add(path, Nothing, Keys.None, Symbol.None, Nothing)
        End Sub

        Sub Add(path As String, symbol As Symbol)
            Add(path, Nothing, Keys.None, symbol, Nothing)
        End Sub

        Sub Add(path As String, methodName As String)
            Add(path, methodName, Keys.None, Symbol.None, Nothing)
        End Sub

        Sub Add(path As String, methodName As String, symbol As Symbol)
            Add(path, methodName, Keys.None, symbol, Nothing)
        End Sub

        Sub Add(path As String, methodName As String, symbol As Symbol, params As Object())
            Add(path, methodName, Keys.None, symbol, params)
        End Sub

        Sub Add(path As String, methodName As String, keyData As Keys)
            Add(path, methodName, keyData, Symbol.None, Nothing)
        End Sub

        Sub Add(path As String, methodName As String, keyData As Keys, symbol As Symbol)
            Add(path, methodName, keyData, symbol, Nothing)
        End Sub

        Sub Add(path As String, methodName As String, keyData As Keys, params As Object())
            Add(path, methodName, keyData, Symbol.None, params)
        End Sub

        Sub Add(path As String, methodName As String, params As Object())
            Add(path, methodName, Keys.None, params)
        End Sub

        Sub Add(path As String,
                methodName As String,
                keyData As Keys,
                symbol As Symbol,
                params As Object())

            Dim pathArray = path.SplitNoEmpty("|")
            Dim l = SubItems

            For i = 0 To pathArray.Length - 1
                Dim found As Boolean = False

                For Each iItem In l
                    If i < pathArray.Length - 1 Then
                        If iItem.Text = pathArray(i) Then
                            found = True
                            l = iItem.SubItems
                        End If
                    End If
                Next

                If Not found Then
                    Dim item As New CustomMenuItem(pathArray(i))
                    l.Add(item)
                    l = item.SubItems

                    If i = pathArray.Length - 1 Then
                        item.MethodName = methodName
                        item.KeyData = keyData
                        item.Symbol = symbol

                        If Not params Is Nothing Then
                            item.Parameters.AddRange(params)
                        End If
                    End If
                End If
            Next
        End Sub

        <NonSerialized()>
        Public Parent As CustomMenuItem

        Shared Sub SetParents(item As CustomMenuItem)
            For Each i In item.SubItems
                i.Parent = item
                SetParents(i)
            Next
        End Sub

        Sub Remove()
            Parent.SubItems.Remove(Me)
        End Sub

        Function GetAllItems() As List(Of CustomMenuItem)
            Dim ret As New List(Of CustomMenuItem)
            AddToList(Me, ret)
            Return ret
        End Function

        Sub AddToList(item As CustomMenuItem, list As List(Of CustomMenuItem))
            For Each i In item.SubItems
                list.Add(i)
                AddToList(i, list)
            Next
        End Sub

        Function GetClone() As CustomMenuItem
            Return ObjectHelp.GetCopy(Me)
        End Function

        Shared Sub UpdateObsoleteCommands(newNames As Dictionary(Of String, String), menus As CustomMenuItem())
            For Each menu In menus
                For Each item In menu.GetAllItems
                    For Each pair In newNames
                        If item.MethodName = pair.Key Then
                            item.MethodName = pair.Value
                        End If
                    Next
                Next
            Next
        End Sub
    End Class

    'Builds an Eto menu (menu bar or context menu) from a CustomMenuItem definition.
    Public Class CustomMenu
        Private Items As New List(Of CustomMenuItem)

        Property MenuItems As New List(Of MenuItemEx)
        Property ActionDictionary As New Dictionary(Of String, MenuItemEx)
        Property TextDictionary As New Dictionary(Of String, MenuItemEx)
        Property DefaultMenu As Func(Of CustomMenuItem)
        Property MenuItem As CustomMenuItem
        Property CommandManager As CommandManager
        Property Target As MenuItemCollection
        Property Form As Window

        'shortcuts are handled by the form key handler instead of native accelerators
        Property HandleShortcutsManually As Boolean

        'top level items of a menu bar don't show the shortcut key in the text
        Property IsMenuBar As Boolean

        Event Command(e As CustomMenuItemEventArgs)

        Sub New(defaultMenu As Func(Of CustomMenuItem),
                menuItem As CustomMenuItem,
                commandManager As CommandManager,
                target As MenuItemCollection)

            Me.CommandManager = commandManager
            Me.DefaultMenu = defaultMenu
            Me.MenuItem = menuItem
            Me.Target = target
        End Sub

        Sub AddKeyDownHandler(form As Window)
            Me.Form = form
            HandleShortcutsManually = True
            AddHandler form.KeyDown, AddressOf OnKeyDown
        End Sub

        Sub RemoveKeyDownHandler(control As Control)
            RemoveHandler control.KeyDown, AddressOf OnKeyDown
        End Sub

        Function GetKeys() As StringPairList
            Dim ret As New StringPairList

            For Each i As MenuItemEx In MenuItems
                If i.ShortcutKeyDisplayString.TrimEx <> "" Then
                    Dim sp As New StringPair

                    If i.Text.EndsWith("...") Then
                        sp.Name = i.Text.TrimEnd("."c)
                    Else
                        sp.Name = i.Text
                    End If

                    sp.Value = i.ShortcutKeyDisplayString
                    ret.Add(sp)
                End If
            Next

            Return ret
        End Function

        Function GetTips() As StringPairList
            Dim ret As New StringPairList

            For Each item In MenuItems
                Dim help = item.GetHelp

                If Not help Is Nothing Then
                    ret.Add(help)
                End If
            Next

            Return ret
        End Function

        Sub EnableMenuItemByActionName(actionName As String, enabled As Boolean)
            If ActionDictionary.ContainsKey(actionName) Then
                ActionDictionary(actionName).Enabled = enabled
            End If
        End Sub

        Function GetMenuItemByText(text As String) As MenuItemEx
            If TextDictionary.ContainsKey(text) Then
                Return TextDictionary(text)
            End If
        End Function

        Function Edit() As CustomMenuItem
            Using form As New CustomMenuEditor(Me)
                If form.ShowDialog = DialogResult.OK Then
                    MenuItem = form.GetState
                    BuildMenu()
                End If
            End Using

            Return MenuItem
        End Function

        Sub OnKeyDown(sender As Object, e As KeyEventArgs)
            If e.Handled Then
                Exit Sub
            End If

            For Each i As CustomMenuItem In Items
                If i.KeyData <> Keys.None AndAlso i.KeyData = e.KeyData AndAlso i.MenuItem IsNot Nothing AndAlso i.MenuItem.Enabled Then
                    e.Handled = True
                    OnCommand(i)
                    Exit For
                End If
            Next
        End Sub

        Sub MenuClick(sender As Object, e As EventArgs)
            Dim mi = TryCast(sender, MenuItemEx)

            If mi IsNot Nothing Then
                OnCommand(mi.CustomMenuItem)
            End If
        End Sub

        Sub OnCommand(item As CustomMenuItem)
            If item.MethodName <> "" Then
                Dim e As New CustomMenuItemEventArgs(item)
                RaiseEvent Command(e)

                If Not e.Handled Then
                    Process(item)
                End If

                Form?.Invalidate()
            End If
        End Sub

        Sub Process(item As CustomMenuItem)
            CommandManager.Process(item.MethodName, item.Parameters)
        End Sub

        Sub BuildMenu()
            Target.Clear()
            Items.Clear()
            MenuItems.Clear()
            ActionDictionary.Clear()
            TextDictionary.Clear()
            BuildMenu(Target, MenuItem)
        End Sub

        Sub BuildMenu(collection As MenuItemCollection, item As CustomMenuItem)
            For Each cmi In item.SubItems
                cmi.CustomMenu = Me

                If cmi.Text = "-" Then
                    collection.Add(New SeparatorMenuItem)
                    Items.Add(cmi)
                    Continue For
                End If

                Dim mi As New MenuItemEx()
                MenuItems.Add(mi)

                If cmi.MethodName <> "" Then
                    ActionDictionary(cmi.MethodName) = mi
                End If

                If cmi.Text <> "" Then
                    TextDictionary(cmi.Text) = mi
                End If

                mi.CustomMenuItem = cmi
                cmi.MenuItem = mi
                mi.Text = cmi.Text

                If cmi.KeyData <> Keys.None Then
                    If IsMenuBar AndAlso collection Is Target Then
                        mi.Help = If(mi.Help = "", KeysHelp.GetKeyString(cmi.KeyData), mi.Help)
                    ElseIf HandleShortcutsManually Then
                        mi.ShortcutKeyDisplayString = KeysHelp.GetKeyString(cmi.KeyData)
                    Else
                        mi.SetNativeShortcut(cmi.KeyData)
                    End If
                End If

                If cmi.SubItems.Count = 0 Then
                    AddHandler mi.Click, AddressOf MenuClick

                    If IsMenuBar AndAlso collection Is Target Then
                        EnableDirectClick(mi)
                    End If
                End If

                Items.Add(cmi)
                collection.Add(mi)
                BuildMenu(mi.Items, cmi)
            Next
        End Sub

        'GTK activates menu bar items without submenu only if the button is held down for a while
        Shared Sub EnableDirectClick(mi As MenuItemEx)
            Dim item = TryCast(mi.ControlObject, Global.Gtk.MenuItem)

            If item Is Nothing Then
                Exit Sub
            End If

            AddHandler item.ButtonPressEvent, Sub(sender, e) e.RetVal = True
            AddHandler item.ButtonReleaseEvent, Sub(sender, e)
                                                    e.RetVal = True

                                                    If e.Event.Button = 1 AndAlso mi.Enabled Then
                                                        mi.PerformClick()
                                                    End If
                                                End Sub
        End Sub
    End Class

    Public Class CustomMenuItemEventArgs
        Inherits EventArgs

        Property Handled As Boolean
        Property Item As CustomMenuItem

        Sub New(item As CustomMenuItem)
            Me.Item = item
        End Sub
    End Class

    Public Class MenuItemEx
        Inherits ButtonMenuItem

        Shared Property UseTooltips As Boolean = True

        Private Action As Action
        Private FormValue As Window
        Private ShortcutValue As Keys
        Private ShortcutKeyDisplayStringValue As String = ""
        Private TextValue As String = ""

        Property EnabledFunc As Func(Of Boolean)
        Property VisibleFunc As Func(Of Boolean)
        Property Path As String

        Sub New()
            MyBase.New()
        End Sub

        Sub New(text As String)
            MyBase.New()
            Me.Text = text
        End Sub

        Sub New(text As String, a As Action)
            Me.New(text, a, Nothing)
        End Sub

        Sub New(text As String,
                action As Action,
                Optional tooltip As String = Nothing,
                Optional enabled As Boolean = True)

            MyBase.New()
            Me.Text = text
            Me.Action = action
            Me.Help = tooltip
            Me.Enabled = enabled
        End Sub

        'the text without the shortcut display string
        Shadows Property Text As String
            Get
                Return TextValue
            End Get
            Set(value As String)
                TextValue = If(value, "")
                UpdateText()
            End Set
        End Property

        Property ShortcutKeyDisplayString As String
            Get
                Return ShortcutKeyDisplayStringValue
            End Get
            Set(value As String)
                ShortcutKeyDisplayStringValue = If(value, "")
                UpdateText()
            End Set
        End Property

        Private MarkedValue As Boolean

        'marks the currently selected value in menus of menu buttons
        Property Marked As Boolean
            Get
                Return MarkedValue
            End Get
            Set(value As Boolean)
                If value <> MarkedValue Then
                    MarkedValue = value
                    UpdateText()
                End If
            End Set
        End Property

        Sub UpdateText()
            Dim value = TextValue.Replace("&", "&&")

            If MarkedValue Then
                value = "✓ " + value
            End If

            If ShortcutKeyDisplayStringValue.Trim <> "" Then
                value += "    " + ShortcutKeyDisplayStringValue.Trim
            End If

            MyBase.Text = value
        End Sub

        Property Form As Window
            Get
                Return FormValue
            End Get
            Set(value As Window)
                If FormValue IsNot Nothing Then
                    RemoveHandler FormValue.KeyDown, AddressOf KeyDown
                End If

                FormValue = value

                If FormValue IsNot Nothing AndAlso ShortcutValue <> Keys.None Then
                    AddHandler FormValue.KeyDown, AddressOf KeyDown
                End If
            End Set
        End Property

        'context menu shortcuts are handled by the form key handler, the menu only shows the key
        Shadows Property Shortcut As Keys
            Get
                Return ShortcutValue
            End Get
            Set(value As Keys)
                If FormValue IsNot Nothing AndAlso ShortcutValue <> Keys.None Then
                    RemoveHandler FormValue.KeyDown, AddressOf KeyDown
                End If

                ShortcutValue = value

                If value <> Keys.None Then
                    ShortcutKeyDisplayString = KeysHelp.GetKeyString(value)

                    If FormValue IsNot Nothing Then
                        AddHandler FormValue.KeyDown, AddressOf KeyDown
                    End If
                End If
            End Set
        End Property

        'native accelerator, used for menu bar items
        Sub SetNativeShortcut(value As Keys)
            MyBase.Shortcut = value
        End Sub

        Sub KeyDown(sender As Object, e As KeyEventArgs)
            If e.Handled OrElse ShortcutValue = Keys.None Then
                Exit Sub
            End If

            If e.KeyData = ShortcutValue Then
                If EnabledFunc IsNot Nothing Then
                    Enabled = EnabledFunc.Invoke
                End If

                If Enabled AndAlso (VisibleFunc Is Nothing OrElse VisibleFunc.Invoke) Then
                    e.Handled = True
                    PerformClick()
                End If
            End If
        End Sub

        Sub Opening(sender As Object, e As EventArgs)
            If EnabledFunc IsNot Nothing Then
                Enabled = EnabledFunc.Invoke
            End If

            If VisibleFunc IsNot Nothing Then
                Visible = VisibleFunc.Invoke
            End If
        End Sub

        'kept for compatibility, symbol images aren't used on Linux
        Sub SetImage(symbol As Symbol, Optional color As Color = Nothing)
        End Sub

        ReadOnly Property DropDownItems As MenuItemCollection
            Get
                Return Items
            End Get
        End Property

        Shared Function Add(Of T)(
            items As MenuItemCollection,
            path As String,
            action As Action(Of T),
            value As T,
            Optional help As String = Nothing) As MenuItemEx

            Return Add(items, path, value, Sub() action(value), help)
        End Function

        Shared Function Add(items As MenuItemCollection, path As String, Optional help As String = Nothing) As MenuItemEx
            Return Add(items, path, path, help)
        End Function

        Shared Function Add(items As MenuItemCollection, path As String, tag As Object, Optional help As String = Nothing) As MenuItemEx
            Return Add(items, path, tag, Nothing, help)
        End Function

        Shared Function Add(items As MenuItemCollection, path As String, action As Action, Optional help As String = Nothing) As MenuItemEx
            Return Add(items, path, path, action, help)
        End Function

        Shared Function Add(items As MenuItemCollection, path As String, tag As Object, action As Action, Optional help As String = Nothing) As MenuItemEx
            Return Add(items, path, tag, action, Symbol.None, help)
        End Function

        Shared Function Add(items As MenuItemCollection, path As String, tag As Object, action As Action, symbol As Symbol, Optional help As String = Nothing) As MenuItemEx
            Dim a = path.SplitNoEmpty(" | ")
            Dim l = items
            Dim p = ""

            For x = 0 To a.Length - 1
                p += If(x = 0, "", " | ") + a(x)
                Dim found = False

                For Each i In l.OfType(Of MenuItemEx)()
                    If x < a.Length - 1 Then
                        If i.Text = a(x) Then
                            found = True
                            l = i.Items
                            Exit For
                        End If
                    End If
                Next

                If Not found Then
                    If x = a.Length - 1 Then
                        If a(x) = "-" Then
                            l.Add(New SeparatorMenuItem)
                        Else
                            Dim item As New MenuItemEx(a(x), action, help)
                            item.Path = p
                            item.Tag = tag
                            l.Add(item)
                            Return item
                        End If
                    Else
                        Dim item As New MenuItemEx()
                        item.Text = a(x)
                        item.Path = p
                        item.Tag = Nothing
                        l.Add(item)
                        l = item.Items
                    End If
                End If
            Next
        End Function

        Function GetHelp() As StringPair
            If Not CustomMenuItem Is Nothing AndAlso Not CustomMenuItem.CustomMenu Is Nothing AndAlso
                CustomMenuItem.CustomMenu.CommandManager.HasCommand(CustomMenuItem.MethodName) Then

                Dim command = CustomMenuItem.CustomMenu.CommandManager.GetCommand(CustomMenuItem.MethodName)

                If command.Attribute.Description <> "" Then
                    Dim ret As New StringPair

                    If Text.EndsWith("...") Then
                        ret.Name = Text.TrimEnd("."c)
                    Else
                        ret.Name = Text
                    End If

                    ret.Value = command.Attribute.Description
                    Dim paramHelp = command.GetParameterHelp(CustomMenuItem.Parameters)
                    If paramHelp <> "" Then ret.Value += " (" + paramHelp + ")"

                    Return ret
                End If
            End If
        End Function

        Private CustomMenuItemValue As CustomMenuItem

        Property CustomMenuItem() As CustomMenuItem
            Get
                Return CustomMenuItemValue
            End Get
            Set(Value As CustomMenuItem)
                CustomMenuItemValue = Value

                If Not Value Is Nothing AndAlso
                    Not Value.CustomMenu Is Nothing AndAlso
                    Value.CustomMenu.CommandManager.HasCommand(CustomMenuItem.MethodName) Then

                    Dim c = CustomMenuItem.CustomMenu.CommandManager.GetCommand(CustomMenuItem.MethodName)

                    If c.MethodInfo.Name <> "DynamicMenuItem" Then
                        If c.MethodInfo.Name = "ExecuteCommandLine" Then
                            Help = CustomMenuItem.Parameters(0).ToString.Trim(""""c)
                        Else
                            Help = c.Attribute.Description
                        End If
                    End If
                End If
            End Set
        End Property

        Private HelpValue As String

        Property Help() As String
            Get
                Return HelpValue
            End Get
            Set(Value As String)
                HelpValue = Value

                If UseTooltips AndAlso HelpValue <> "" Then
                    ToolTip = HelpDocument.ConvertMarkup(HelpValue, True)
                End If
            End Set
        End Property

        Protected Overrides Sub OnClick(e As EventArgs)
            Action?.Invoke()
            MyBase.OnClick(e)
        End Sub

        Sub ClearAction()
            Action = Nothing
            EnabledFunc = Nothing
            VisibleFunc = Nothing
            Form = Nothing
        End Sub
    End Class

    Public Class TextCustomMenu
        Shared Function EditMenu(value As String, defaults As String, owner As Window) As String
            Using dialog As New MacroEditorDialog
                dialog.SetMacroDefaults()
                dialog.MacroEditorControl.Value = value
                dialog.MacroEditorControl.rtbDefaults.Text = defaults
                dialog.Text = $"Menu Editor - {g.DefaultCommands.GetApplicationDetails()}"

                If defaults <> "" Then
                    dialog.bnContext.Text = " Restore Defaults... "
                    dialog.bnContext.Visible = True
                    dialog.bnContext.ClickAction = Sub()
                                                       If MsgOK("Restore defaults?") Then
                                                           dialog.MacroEditorControl.Value = defaults
                                                           MsgInfo("Defaults were restored.")
                                                       End If
                                                   End Sub
                End If

                If dialog.ShowDialog(owner) = DialogResult.OK Then
                    value = dialog.MacroEditorControl.Value
                End If
            End Using

            Return value
        End Function

        Shared Function GetMenu(definition As String, owner As IContextMenuOwner, action As Action(Of String)) As ContextMenuStripEx
            If owner.ContextMenuStrip Is Nothing Then
                owner.ContextMenuStrip = New ContextMenuStripEx()
            End If

            Dim ret = owner.ContextMenuStrip
            ret.Items.Clear()

            If Not String.IsNullOrWhiteSpace(definition) Then
                For Each i In definition.SplitKeepEmpty(BR)
                    If i.Contains("=") Then
                        Dim arg = i.Right("=").Trim
                        MenuItemEx.Add(ret.Items, i.Left("=").Trim, action, arg, Nothing)
                    ElseIf i.EndsWith("-") Then
                        MenuItemEx.Add(ret.Items, i)
                    ElseIf i = "" Then
                        ret.Items.Add(New SeparatorMenuItem)
                    End If
                Next
            End If

            Return ret
        End Function
    End Class

    'implemented by controls that expose a WinForms like ContextMenuStrip property
    Public Interface IContextMenuOwner
        Property ContextMenuStrip As ContextMenuStripEx
    End Interface

    'owns a menu that is shown manually
    Public Class MenuHolder
        Implements IContextMenuOwner

        Property ContextMenuStrip As ContextMenuStripEx Implements IContextMenuOwner.ContextMenuStrip
    End Class

    Public Class ContextMenuStripEx
        Inherits ContextMenu

        Private FormValue As Window

        Sub New()
            MyBase.New()
        End Sub

        Sub New(container As IContainer)
            MyBase.New()
        End Sub

        Property Form As Window
            Get
                Return FormValue
            End Get
            Set(value As Window)
                FormValue = value

                For Each i In GetItems.OfType(Of MenuItemEx)
                    If i.Shortcut <> Keys.None Then
                        i.Form = value
                    End If
                Next
            End Set
        End Property

        'Eto places menus of GTK widgets without own window, like group boxes, relative to their container,
        'so the position is translated to the window
        Overloads Sub Show(control As Control, x As Integer, y As Integer)
            Dim window = control.ParentWindow
            Dim widget = TryCast(control.ControlObject, Global.Gtk.Widget)
            Dim gtkWindow = TryCast(window?.ControlObject, Global.Gtk.Widget)
            Dim windowX, windowY As Integer

            If widget IsNot Nothing AndAlso gtkWindow IsNot Nothing AndAlso widget.TranslateCoordinates(gtkWindow, x, y, windowX, windowY) Then
                MyBase.Show(window, New PointF(windowX, windowY))
            Else
                MyBase.Show(control, New PointF(x, y))
            End If
        End Sub

        Function Add(path As String) As MenuItemEx
            Return Add(path, Nothing)
        End Function

        Function Add(path As String, action As Action) As MenuItemEx
            Return Add(path, action, True)
        End Function

        Function Add(path As String, action As Action, key As Keys) As MenuItemEx
            Return Add(path, action, key, True, Nothing, Nothing)
        End Function

        Function Add(path As String, action As Action, key As Keys, symbol As Symbol) As MenuItemEx
            Return Add(path, action, key, True, Nothing, Nothing, symbol)
        End Function

        Function Add(path As String, action As Action, key As Keys, enabled As Boolean) As MenuItemEx
            Return Add(path, action, key, enabled, Nothing, Nothing)
        End Function

        Function Add(path As String, action As Action, help As String) As MenuItemEx
            Return Add(path, action, True, help)
        End Function

        Function Add(path As String, action As Action, enabled As Boolean) As MenuItemEx
            Return Add(path, action, enabled, Nothing)
        End Function

        Function Add(path As String, action As Action, key As Keys, help As String) As MenuItemEx
            Return Add(path, action, key, True, Nothing, help)
        End Function

        Function Add(path As String, action As Action, key As Keys, enabledFunc As Func(Of Boolean)) As MenuItemEx
            Return Add(path, action, key, True, enabledFunc)
        End Function

        Function Add(path As String, action As Action, key As Keys, enabledFunc As Func(Of Boolean), help As String) As MenuItemEx
            Return Add(path, action, key, True, enabledFunc, help)
        End Function

        Function Add(path As String, action As Action, enabled As Boolean, help As String) As MenuItemEx
            Return Add(path, action, Keys.None, enabled, Nothing, help)
        End Function

        Function Add(path As String, action As Action, enabledFunc As Func(Of Boolean), help As String) As MenuItemEx
            Return Add(path, action, Keys.None, True, enabledFunc, help)
        End Function

        Function Add(
            path As String,
            action As Action,
            key As Keys,
            enabled As Boolean,
            enabledFunc As Func(Of Boolean),
            Optional help As String = Nothing,
            Optional symbol As Symbol = Symbol.None) As MenuItemEx

            Dim ret = MenuItemEx.Add(Items, path, action)

            If ret Is Nothing Then
                Exit Function
            End If

            ret.Form = Form
            ret.Shortcut = key
            ret.Enabled = enabled
            ret.EnabledFunc = enabledFunc
            ret.Help = help

            AddHandler Opening, AddressOf ret.Opening

            Return ret
        End Function

        Sub ApplyMarginFix()
        End Sub

        Function GetTips() As StringPairList
            Dim ret As New StringPairList

            For Each i In GetItems.OfType(Of MenuItemEx)()
                If i.Help <> "" Then
                    Dim pair As New StringPair

                    If i.Text.EndsWith("...") Then
                        pair.Name = i.Text.TrimEnd("."c)
                    Else
                        pair.Name = i.Text
                    End If

                    pair.Value = i.Help
                    ret.Add(pair)
                End If
            Next

            Return ret
        End Function

        Function GetKeys() As StringPairList
            Dim ret As New StringPairList

            For Each mi In GetItems.OfType(Of MenuItemEx)()
                If mi.ShortcutKeyDisplayString <> "" Then
                    Dim sp As New StringPair

                    If mi.Text.EndsWith("...") Then
                        sp.Name = mi.Text.TrimEnd("."c)
                    Else
                        sp.Name = mi.Text
                    End If

                    sp.Value = mi.ShortcutKeyDisplayString
                    ret.Add(sp)
                End If
            Next

            Return ret
        End Function

        Function GetItems() As List(Of MenuItem)
            Dim ret As New List(Of MenuItem)
            AddItemsRecursive(Items, ret)
            Return ret
        End Function

        Shared Sub AddItemsRecursive(searchList As MenuItemCollection, returnList As List(Of MenuItem))
            For Each i As MenuItem In searchList
                returnList.Add(i)

                Dim bmi = TryCast(i, ButtonMenuItem)

                If bmi IsNot Nothing Then
                    AddItemsRecursive(bmi.Items, returnList)
                End If
            Next
        End Sub
    End Class
End Namespace
