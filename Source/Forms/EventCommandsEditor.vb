
Imports StaxRip.UI

Public Class EventCommandsEditor
    Inherits DialogBase

    Public WithEvents bnAdd As ButtonEx
    Public WithEvents bnRemove As ButtonEx
    Public WithEvents bnClone As ButtonEx
    Public WithEvents bnCancel As ButtonEx
    Public WithEvents bnOK As ButtonEx
    Public WithEvents bnDown As ButtonEx
    Public WithEvents bnUp As ButtonEx
    Public WithEvents bnEdit As ButtonEx
    Public WithEvents bnVariables As ButtonEx
    Public WithEvents lv As ListViewEx

    Private Sub InitializeComponent()
        bnAdd = New ButtonEx("Add...")
        bnEdit = New ButtonEx("Edit...")
        bnClone = New ButtonEx("Clone")
        bnRemove = New ButtonEx("Remove")
        bnUp = New ButtonEx("Up")
        bnDown = New ButtonEx("Down")
        bnVariables = New ButtonEx("Variables...")
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        lv = New ListViewEx

        Dim buttons As New TableLayout(
            Row(bnAdd), Row(bnEdit), Row(bnClone), Row(bnRemove), Row(bnUp), Row(bnDown),
            New TableRow With {.ScaleHeight = True}, Row(bnVariables)) With {.Spacing = DefaultSpacing}

        Content = DialogContent(ButtonRow(bnOK, bnCancel), ScaledRow(Scaled(lv), buttons))

        DefaultButton = bnOK
        AbortButton = bnCancel
        Text = $"Event Command - {g.DefaultCommands.GetApplicationDetails()}"
        SetMinimumSize(20, 15)
    End Sub

    Sub New(eventCommands As List(Of EventCommand))
        MyBase.New()
        InitializeComponent()

        ScaleClientSize(40, 23)

        lv.UpButton = bnUp
        lv.DownButton = bnDown
        lv.RemoveButton = bnRemove
        lv.SingleSelectionButtons = {bnEdit, bnClone}
        lv.UpdateControls()
        lv.EnableListBoxMode()
        lv.CheckBoxes = True
        lv.MultiSelect = False
        lv.ItemCheckProperty = NameOf(EventCommand.Enabled)
        lv.AddItems(ObjectHelp.GetCopy(eventCommands))
        lv.SelectFirst()
    End Sub

    'variables that event commands use as %var:name% and PowerShell scripts as $Variables
    Sub bnVariables_Click() Handles bnVariables.Click
        Using dialog As New MacroEditorDialog
            dialog.Text = $"Variables - {g.DefaultCommands.GetApplicationDetails()}"
            dialog.MacroEditorControl.Value = If(s.Variables <> "", s.Variables,
                "# One variable per line like name = value, for %var:name% in macros" + BR +
                "# and $Variables['name'] in PowerShell scripts" + BR)

            If dialog.ShowDialog() = DialogResult.OK Then
                s.Variables = dialog.MacroEditorControl.Value
                g.SaveSettings()
            End If
        End Using
    End Sub

    Sub bnAdd_Click() Handles bnAdd.Click
        Dim ec As New EventCommand

        Using editor As New EventCommandEditor(ec)
            If editor.ShowDialog = DialogResult.OK Then
                lv.AddItem(ec).Selected = True
            End If
        End Using
    End Sub

    Sub EventCommandsEditor_HelpRequested() Handles Me.HelpRequested
        g.ShowDocumentationPage("Usage/Events.md")
    End Sub

    Sub bnClone_Click() Handles bnClone.Click
        lv.AddItem(ObjectHelp.GetCopy(lv.SelectedItem)).Selected = True
    End Sub

    Sub bnEdit_Click(sender As Object, e As EventArgs) Handles bnEdit.Click
        Using form As New EventCommandEditor(lv.SelectedItem(Of EventCommand))
            If form.ShowDialog = DialogResult.OK Then
                lv.RefreshSelection()
            End If
        End Using
    End Sub
End Class
