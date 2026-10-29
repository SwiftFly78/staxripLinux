

Namespace UI
    'icons of the GTK theme, symbolic icons take the text color, so they follow light and dark themes
    Public Module Styling
        'the first icon of the theme, icon names differ between themes like Breeze and Adwaita
        Function GetIconName(ParamArray names As String()) As String
            Dim theme = Global.Gtk.IconTheme.Default

            For Each name In names
                If theme.HasIcon(name) Then
                    Return name
                End If
            Next

            Return Nothing
        End Function

        Function GetIconName(symbol As Symbol) As String
            Select Case symbol
                Case Symbol.Up, Symbol.ChevronUp
                    Return GetIconName("go-up-symbolic")
                Case Symbol.Down, Symbol.ChevronDown
                    Return GetIconName("go-down-symbolic")
                Case Symbol.Back, Symbol.ChevronLeft
                    Return GetIconName("go-previous-symbolic")
                Case Symbol.Forward, Symbol.ChevronRight
                    Return GetIconName("go-next-symbolic")
                Case Symbol.Add
                    Return GetIconName("list-add-symbolic")
                Case Symbol.Remove
                    Return GetIconName("list-remove-symbolic")
                Case Symbol.Delete
                    Return GetIconName("edit-delete-symbolic")
                Case Symbol.Edit
                    Return GetIconName("document-edit-symbolic")
                Case Symbol.Copy
                    Return GetIconName("edit-copy-symbolic")
                Case Symbol.Search, Symbol.Zoom
                    Return GetIconName("system-search-symbolic")
                Case Symbol.Help
                    Return GetIconName("help-browser-symbolic", "help-about-symbolic")
                Case Symbol.Play
                    Return GetIconName("media-playback-start-symbolic")
                Case Symbol.Settings
                    Return GetIconName("emblem-system-symbolic", "preferences-system-symbolic")
            End Select

            Return Nothing
        End Function

        Function CreateIcon(iconName As String) As Global.Gtk.Image
            Dim ret = Global.Gtk.Image.NewFromIconName(iconName, Global.Gtk.IconSize.Button)
            ret.Show()
            Return ret
        End Function

        'shows an icon left of the button text and a menu arrow right of it, fill aligns the text left and the arrow
        'right like in a combo box, Eto replaces the child of the button when the text changes, so this runs after
        'each change
        Sub SetButtonContent(button As Button, iconName As String, showMenuArrow As Boolean, Optional fill As Boolean = False)
            Dim gtkButton = TryCast(button.ControlObject, Global.Gtk.Button)
            Dim label = TryCast(gtkButton?.Child, Global.Gtk.Label)

            If gtkButton Is Nothing OrElse (iconName = "" AndAlso Not showMenuArrow) Then
                Exit Sub
            End If

            Dim arrowName = If(showMenuArrow, GetIconName("pan-down-symbolic"), Nothing)

            If label Is Nothing OrElse label.Text = "" Then
                If iconName <> "" OrElse arrowName <> "" Then
                    If label IsNot Nothing Then gtkButton.Remove(label)
                    gtkButton.Child = CreateIcon(If(iconName <> "", iconName, arrowName))
                End If

                Exit Sub
            End If

            gtkButton.Remove(label)
            Dim box As New Global.Gtk.Box(Global.Gtk.Orientation.Horizontal, 6) With {
                .Halign = If(fill, Global.Gtk.Align.Fill, Global.Gtk.Align.Center)}

            If iconName <> "" Then
                box.PackStart(CreateIcon(iconName), False, False, 0)
            End If

            label.Xalign = 0
            box.PackStart(label, fill, fill, 0)

            If arrowName <> "" Then
                box.PackStart(CreateIcon(arrowName), False, False, 0)
            End If

            box.Show()
            gtkButton.Child = box
        End Sub
    End Module
End Namespace
