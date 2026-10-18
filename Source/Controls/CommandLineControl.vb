
Imports StaxRip.UI

Public Class CommandLineControl
    Inherits Panel

    Public WithEvents tb As TextEdit
    Public WithEvents bn As ButtonEx

    Private ReadOnly MenuHolder As New MenuHolder

    Property RestoreFunc As Func(Of String)
    Property Presets As String

    Event PresetsChanged(presets As String)
    Event ValueChanged(value As String)

    Sub New()
        tb = New TextEdit With {.Multiline = True, .Font = FontManager.GetCodeFont()}
        bn = New ButtonEx With {.ShowMenuSymbol = True}
        AddHandler tb.TextChanged, Sub() RaiseEvent ValueChanged(tb.Text)

        Dim buttonColumn As New TableLayout(New TableRow(bn), New TableRow With {.ScaleHeight = True})
        Content = New TableLayout(New TableRow(New TableCell(tb, True), New TableCell(buttonColumn)) With {.ScaleHeight = True}) With {
            .Spacing = New Size(FH(0.4), 0)}
    End Sub

    Sub MenuItenClick(value As String)
        Dim tup = Macro.ExpandGUI(value)

        If tup.Cancel Then
            Exit Sub
        End If

        value = tup.Value

        If Not value Like "*$*$*" Then
            If tb.Text = "" Then
                tb.Text = value
            Else
                tb.Text = tb.Text.Trim + " " + value
            End If
        End If
    End Sub

    Sub EditPresets()
        Using dialog As New MacroEditorDialog
            dialog.SetMacroDefaults()
            dialog.MacroEditorControl.Value = Presets.FormatColumn("=")
            dialog.Text = $"Menu Editor - {g.DefaultCommands.GetApplicationDetails()}"

            If Not RestoreFunc Is Nothing Then
                dialog.bnContext.Text = " Restore Defaults... "
                dialog.bnContext.Visible = True
                dialog.bnContext.ClickAction = Sub()
                                                   If MsgOK("Restore defaults?") Then
                                                       dialog.MacroEditorControl.Value = RestoreFunc.Invoke
                                                       MsgInfo("Defaults were restored.")
                                                   End If
                                               End Sub

                dialog.MacroEditorControl.rtbDefaults.Text = RestoreFunc.Invoke
            End If

            If dialog.ShowDialog(Me) = DialogResult.OK Then
                Presets = dialog.MacroEditorControl.Value
                RaiseEvent PresetsChanged(Presets)
            End If
        End Using
    End Sub

    Sub bn_Click(sender As Object, e As EventArgs) Handles bn.Click
        Dim cms = TextCustomMenu.GetMenu(Presets, MenuHolder, AddressOf MenuItenClick)
        cms.Items.Add(New SeparatorMenuItem)
        cms.Items.Add(New MenuItemEx("Edit Menu...", AddressOf EditPresets))
        cms.Show(bn, 0, bn.Height)
    End Sub
End Class
