
Imports System.Threading

Imports StaxRip.UI

Public Class MacrosForm
    Inherits DialogBase

    Public WithEvents lv As ListViewEx
    Public WithEvents stb As SearchTextBox
    Public WithEvents bnCopy As ButtonEx
    Public lName, lValue, lDescription As LabelEx

    Sub New()
        lv = New ListViewEx With {.View = View.Details, .HeaderStyle = ColumnHeaderStyle.None, .FullRowSelect = True, .MultiSelect = False}
        lv.Columns.Add("Macro")
        stb = New SearchTextBox
        bnCopy = New ButtonEx("Copy")
        lName = New LabelEx With {.Wrap = WrapMode.Character}
        lValue = New LabelEx With {.Wrap = WrapMode.Character}
        lDescription = New LabelEx With {.Wrap = WrapMode.Word, .VerticalAlignment = VerticalAlignment.Top}

        Dim boldFont = FontManager.GetDefaultFont(0, FontStyle.Bold)
        Dim right As New TableLayout(
            Row(New LabelEx With {.Text = "Name:", .Font = boldFont}),
            Row(lName),
            Row(HRow(bnCopy)),
            Row(New LabelEx With {.Text = "Value:", .Font = boldFont}),
            Row(lValue),
            Row(New LabelEx With {.Text = "Description:", .Font = boldFont}),
            ScaledRow(lDescription)) With {.Spacing = DefaultSpacing}

        Dim left As New TableLayout(Row(stb), ScaledRow(lv)) With {.Spacing = DefaultSpacing}

        Content = New TableLayout(ScaledRow(New TableCell(left) With {.ScaleWidth = False}, New TableCell(right, True))) With {
            .Spacing = New Size(FH(1), 0),
            .Padding = DefaultPadding}

        left.Width = FH(18)
        Text = $"Macros - {g.DefaultCommands.GetApplicationDetails()}"
        ScaleClientSize(50, 36)
    End Sub

    Shared Sub ShowDialogForm()
        Using form As New MacrosForm
            form.ShowDialog()
        End Using
    End Sub

    Function Match(search As String, ParamArray values As String()) As Boolean
        For Each i In values
            If i <> "" AndAlso i.ToLowerInvariant.Contains(search.ToLowerInvariant) Then
                Return True
            End If
        Next
    End Function

    Sub Populate()
        lv.BeginUpdate()
        lv.Items.Clear()

        Dim macros As New StringPairList

        For Each mac In Macro.GetMacros(True, True, True, True, False)
            macros.Add(mac.Name, mac.Description)
        Next

        For Each i In macros
            If stb.Text = "" OrElse Match(stb.Text, i.Name, i.Value) Then
                Dim item As New ListViewItem With {
                    .Text = i.Name,
                    .Tag = i.Value
                }
                lv.Items.Add(item)
            End If
        Next

        lv.EndUpdate()

        If lv.Items.Count > 0 Then
            lv.Items(0).Selected = True
            lv.Items(0).EnsureVisible()
        End If
    End Sub

    Sub tbFilter_TextChanged() Handles stb.TextChanged
        Populate()
    End Sub

    Sub MacrosForm_HelpRequested(sender As Object, e As EventArgs) Handles Me.HelpRequested
        g.ShowWikiPage("Macros")
    End Sub

    Sub TaskForm_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyData = Keys.Enter Then
            e.Handled = True
            bnCopy.PerformClick()
            Close()
        End If
    End Sub

    Sub lv_DoubleClick(sender As Object, e As EventArgs) Handles lv.DoubleClick
        bnCopy.PerformClick()
        Close()
    End Sub

    Sub lv_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lv.SelectedIndexChanged
        If lv.SelectedItems.Count > 0 Then
            Dim item = lv.SelectedItems(0)
            lName.Text = item.Text
            lValue.Text = Macro.Expand(item.Text)
            lDescription.Text = CStr(item.Tag)
        Else
            lName.Text = ""
            lValue.Text = ""
            lDescription.Text = ""
        End If
    End Sub

    Sub bnCopy_Click(sender As Object, e As EventArgs) Handles bnCopy.Click
        Clipboard.SetText(lName.Text)
    End Sub

    Sub MacrosForm_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        Populate()
        stb.Focus()
    End Sub
End Class
