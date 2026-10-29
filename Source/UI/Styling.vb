
Imports System.Runtime.CompilerServices

Namespace UI
    'styles and icons of the GTK theme, the style sheet only uses the named colors of the theme,
    'so the controls follow light and dark themes like Breeze and Adwaita
    Public Module Styling
        Private Const StyleSheet As String = "
.staxrip-card {
    background-color: @theme_base_color;
    border: 1px solid @borders;
    border-radius: 8px;
}

.staxrip-heading {
    font-weight: bold;
    font-size: 1.1em;
}

.staxrip-title {
    font-weight: bold;
    font-size: 1.25em;
}

.staxrip-toolbar {
    border-bottom: 1px solid @borders;
}

.staxrip-sidebar {
    background-color: @theme_base_color;
    border-right: 1px solid @borders;
}

.staxrip-sidebar treeview {
    -GtkTreeView-vertical-separator: 10;
}

.staxrip-action-area {
    border-top: 1px solid @borders;
}

.staxrip-next-step {
    border-top: 1px solid @borders;
}

.staxrip-next-step-ready .staxrip-next-step-icon {
    color: @success_color;
}

.staxrip-next-step-hint .staxrip-next-step-icon {
    color: @theme_selected_bg_color;
}

.staxrip-next-step-warning {
    background-color: mix(@theme_bg_color, @warning_color, 0.1);
}

.staxrip-next-step-warning .staxrip-next-step-icon {
    color: @warning_color;
}

.staxrip-next-step-error {
    background-color: mix(@theme_bg_color, @error_color, 0.1);
}

.staxrip-next-step-error .staxrip-next-step-icon {
    color: @error_color;
}

entry.staxrip-attention-warning,
button.staxrip-attention-warning {
    border-color: @warning_color;
}

label.staxrip-attention-warning {
    color: @warning_color;
}

entry.staxrip-attention-error,
button.staxrip-attention-error {
    border-color: @error_color;
}

label.staxrip-attention-error {
    color: @error_color;
}
"

        'card headers share their height, so the headings of a row have the same baseline
        Private HeaderSizeGroup As Global.Gtk.SizeGroup

        Sub Install()
            Dim provider As New Global.Gtk.CssProvider
            provider.LoadFromData(StyleSheet)
            Global.Gtk.StyleContext.AddProviderForScreen(Global.Gdk.Screen.Default, provider, 600)
        End Sub

        Sub AddToHeaderSizeGroup(control As Control)
            Dim widget = TryCast(control?.ControlObject, Global.Gtk.Widget)

            If widget IsNot Nothing Then
                HeaderSizeGroup = If(HeaderSizeGroup, New Global.Gtk.SizeGroup(Global.Gtk.SizeGroupMode.Vertical))
                HeaderSizeGroup.AddWidget(widget)
            End If
        End Sub

        <Extension>
        Sub AddStyleClass(control As Control, ParamArray names As String())
            Dim widget = TryCast(control?.ControlObject, Global.Gtk.Widget)

            If widget IsNot Nothing Then
                For Each name In names
                    widget.StyleContext.AddClass(name)
                Next
            End If
        End Sub

        <Extension>
        Sub RemoveStyleClass(control As Control, ParamArray names As String())
            Dim widget = TryCast(control?.ControlObject, Global.Gtk.Widget)

            If widget IsNot Nothing Then
                For Each name In names
                    widget.StyleContext.RemoveClass(name)
                Next
            End If
        End Sub

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

        'an icon of the theme in the given size as Eto control
        Function CreateIconControl(iconName As String, size As Integer) As Control
            Dim image = Global.Gtk.Image.NewFromIconName(iconName, Global.Gtk.IconSize.Dialog)
            image.PixelSize = size
            image.Show()
            Return New NativeControlHost(image)
        End Function

        'shows an icon left of the button text and a menu arrow right of it, fill aligns the text left and the arrow
        'right like in a combo box, Eto replaces the child of the button when the text changes, so this runs after
        'each change
        Sub SetButtonContent(button As Button, iconName As String, showMenuArrow As Boolean, Optional fill As Boolean = False)
            SetButtonContent(button, iconName, If(showMenuArrow, GetIconName("pan-down-symbolic"), Nothing), fill)
        End Sub

        Sub SetButtonContent(button As Button, iconName As String, arrowName As String, fill As Boolean)
            Dim gtkButton = TryCast(button.ControlObject, Global.Gtk.Button)
            Dim label = TryCast(gtkButton?.Child, Global.Gtk.Label)

            If gtkButton Is Nothing OrElse (iconName = "" AndAlso arrowName = "") Then
                Exit Sub
            End If

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
