
Imports StaxRip.UI

Public Class ProfilesForm
    Inherits DialogBase

    Public WithEvents lbMain As ListBoxEx
    Public WithEvents bnLoad, bnDown, bnUp, bnRemove, bnAdd, bnEdit, bnClone, bnRight, bnLeft, bnRestore, bnRename As ButtonEx
    Public WithEvents bnCancel As ButtonEx
    Public WithEvents bnOK As ButtonEx
    Public TipProvider As TipProvider

    Private Sub InitializeComponent()
        lbMain = New ListBoxEx With {.SelectionMode = UI.SelectionMode.MultiExtended}
        bnAdd = New ButtonEx("Add...")
        bnRemove = New ButtonEx("Remove")
        bnEdit = New ButtonEx("Edit...")
        bnRename = New ButtonEx("Rename")
        bnUp = New ButtonEx With {.Symbol = Symbol.Up}
        bnDown = New ButtonEx With {.Symbol = Symbol.Down}
        bnLeft = New ButtonEx With {.Symbol = Symbol.Back}
        bnRight = New ButtonEx With {.Symbol = Symbol.Forward}
        bnClone = New ButtonEx("Clone")
        bnLoad = New ButtonEx("Load")
        bnRestore = New ButtonEx("Restore")
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        TipProvider = New TipProvider

        Dim arrows As New TableLayout(
            Row(Scaled(bnUp), Scaled(bnLeft)),
            Row(Scaled(bnDown), Scaled(bnRight))) With {.Spacing = DefaultSpacing}

        Dim buttons As New TableLayout(
            Row(bnAdd), Row(bnRemove), Row(bnEdit), Row(bnRename), Row(arrows), Row(bnClone), Row(bnLoad), Row(bnRestore),
            New TableRow With {.ScaleHeight = True}) With {.Spacing = DefaultSpacing}

        Content = PaddedTable(
            ScaledRow(Scaled(lbMain), buttons),
            Row(ButtonRow(bnOK, bnCancel), Nothing))

        DefaultButton = bnOK
        AbortButton = bnCancel
        ScaleClientSize(30, 26)
    End Sub

    Private Profiles As IList
    Private AddProfileMethod As Func(Of Profile)
    Private LoadProfileMethod As Action(Of Profile)
    Private TextValues As New Dictionary(Of Button, String)
    Private DefaultsFunc As Func(Of IList)

    Sub New(title As String,
            profiles As IList,
            loadAction As Action(Of Profile),
            addFunc As Func(Of Profile),
            defaultsFunc As Func(Of IList))

        MyBase.New()
        InitializeComponent()
        Text = $"{title} - {g.DefaultCommands.GetApplicationDetails()}"

        Me.Profiles = profiles
        Me.DefaultsFunc = defaultsFunc
        LoadProfileMethod = loadAction
        AddProfileMethod = addFunc

        For Each i As Profile In profiles
            lbMain.Items.Add(i.GetCopy)
        Next

        UpdateControls()

        TipProvider.SetTip("Adds all default profiles.", bnRestore)
        TipProvider.SetTip("Moves items up, multiselect is available.", "Up", bnUp)
        TipProvider.SetTip("Moves items down, multiselect is available.", "Down", bnDown)
        TipProvider.SetTip("Moves selected profiles to it's parent menu, multiselect is available.", "Left", bnLeft)
        TipProvider.SetTip("Moves profiles into a sub menu, multiselect is available.", "Right", bnRight)
    End Sub

    Sub UpdateControls()
        Dim pro = DirectCast(lbMain.SelectedItem, Profile)
        Dim count = lbMain.SelectedItems.Count
        bnClone.Enabled = count = 1
        bnEdit.Enabled = count = 1 AndAlso pro.CanEdit
        bnLoad.Enabled = count = 1 AndAlso Not LoadProfileMethod Is Nothing
        bnRemove.Enabled = count > 0
        bnRename.Enabled = count = 1
        bnUp.Enabled = lbMain.CanMoveUp()
        bnDown.Enabled = lbMain.CanMoveDown()
        bnLeft.Enabled = count > 0 AndAlso lbMain.Text.Contains(" | ")
        bnRight.Enabled = count > 0
        bnRestore.Enabled = Not DefaultsFunc Is Nothing
    End Sub

    Sub lbMain_KeyDown(sender As Object, e As KeyEventArgs) Handles lbMain.KeyDown
        Select Case e.KeyData
            Case Keys.Delete
                If bnRemove.Enabled Then
                    bnRemove.PerformClick()
                End If
            Case Keys.F2
                If bnRename.Enabled Then
                    bnRename.PerformClick()
                End If
            Case Keys.Control Or Keys.Up
                If bnUp.Enabled Then
                    e.Handled = True
                    bnUp.PerformClick()
                End If
            Case Keys.Control Or Keys.Down
                If bnDown.Enabled Then
                    e.Handled = True
                    bnDown.PerformClick()
                End If
        End Select
    End Sub

    Sub lbMain_SelectedIndexChanged() Handles lbMain.SelectedIndexChanged
        UpdateControls()
    End Sub

    Sub bnLoad_Click() Handles bnLoad.Click
        LoadProfileMethod(DirectCast(lbMain.SelectedItem, Profile))
        TextValues(bnLoad) = bnLoad.Text
        bnLoad.ShowBold()
    End Sub

    Sub lbMain_DoubleClick() Handles lbMain.DoubleClick
        If Not lbMain.SelectedItem Is Nothing AndAlso DirectCast(lbMain.SelectedItem, Profile).CanEdit Then
            bnEdit.PerformClick()
        End If
    End Sub

    Sub bnAdd_Click() Handles bnAdd.Click
        Dim pm = AddProfileMethod()

        If Not pm Is Nothing Then
            pm = ObjectHelp.GetCopy(pm)
            pm.Name = InputBox.Show("Enter the name of the new profile", pm.Name)

            If Not pm.Name Is Nothing Then
                Dim remove As Profile = Nothing

                For Each i As Profile In lbMain.Items
                    If i.Name = pm.Name Then
                        If MsgOK("There is already a profile with this name, overwrite?") Then
                            remove = i
                        Else
                            Exit Sub
                        End If
                    End If
                Next

                If Not remove Is Nothing Then
                    lbMain.Items.Remove(remove)
                End If

                pm.Clean()
                lbMain.Items.Insert(0, pm)
                lbMain.ClearSelected()
                lbMain.SelectedItem = pm
                UpdateControls()
            End If
        End If
    End Sub

    Sub bnRemove_Click() Handles bnRemove.Click
        lbMain.RemoveSelection()
        UpdateControls()
    End Sub

    Sub bnRename_Click() Handles bnRename.Click
        Dim profile = DirectCast(lbMain.SelectedItem, Profile)
        Dim ret = InputBox.Show("Please enter a name to rename the profile", profile.Name)

        If Not ret Is Nothing Then
            profile.Name = ret
            lbMain.UpdateSelection()
            UpdateControls()
        End If
    End Sub

    Sub bnEdit_Click() Handles bnEdit.Click
        Dim profile = DirectCast(ObjectHelp.GetCopy(lbMain.SelectedItem), Profile)

        If profile.Edit = DialogResult.OK Then
            lbMain.Items(lbMain.SelectedIndex) = profile
            UpdateControls()
        End If
    End Sub

    Sub bnClone_Click() Handles bnClone.Click
        lbMain.Items.Insert(lbMain.SelectedIndex, ObjectHelp.GetCopy(lbMain.SelectedItem))
    End Sub

    Sub bnUp_Click(sender As Object, e As EventArgs) Handles bnUp.Click
        lbMain.MoveSelectionUp()
        UpdateControls()
    End Sub

    Sub bnDown_Click(sender As Object, e As EventArgs) Handles bnDown.Click
        lbMain.MoveSelectionDown()
        UpdateControls()
    End Sub

    Sub bnLeft_Click(sender As Object, e As EventArgs) Handles bnLeft.Click
        lbMain.SaveSelection()

        For x = 0 To lbMain.Items.Count - 1
            If lbMain.GetSelected(x) Then
                Dim p = DirectCast(lbMain.Items(x), Profile)

                If p.Name.Contains(" | ") Then
                    p.Name = p.Name.Right(" | ")
                    lbMain.Items(x) = lbMain.Items(x)
                End If
            End If
        Next

        lbMain.RestoreSelection()
    End Sub

    Sub bnRightRight_Click(sender As Object, e As EventArgs) Handles bnRight.Click
        Dim inputName = InputBox.Show("Enter a name for a sub menu")

        If inputName <> "" Then
            lbMain.SaveSelection()

            For x = 0 To lbMain.Items.Count - 1
                If lbMain.GetSelected(x) Then
                    Dim p = DirectCast(lbMain.Items(x), Profile)
                    p.Name = inputName + " | " + p.Name
                    lbMain.Items(x) = lbMain.Items(x)
                End If
            Next

            lbMain.RestoreSelection()
        End If
    End Sub

    Sub bnRestore_Click(sender As Object, e As EventArgs) Handles bnRestore.Click
        Using td = New TaskDialog(Of String)()
            td.Title = "How to restore?"
            td.AddCommand("Clean Restore", "Clean")
            td.AddCommand("Overwrite current profiles with default profiles", "Current profiles will be overwritten by the defaults, profiles with different name remain.", "Overwrite")
            td.AddButton("Cancel", "Cancel")
            td.Icon = TaskIcon.Question

            Select Case td.Show
                Case "Clean"
                    lbMain.Items.Clear()
                    lbMain.Items.AddRange(DefaultsFunc().OfType(Of Profile).ToArray())
                Case "Overwrite"
                    Dim uniqueProfiles = lbMain.Items.OfType(Of Profile).Where(Function(x) Not DefaultsFunc().OfType(Of Profile).Any(Function(y) x.Name = y.Name)).ToArray()
                    lbMain.Items.Clear()
                    lbMain.Items.AddRange(uniqueProfiles)
                    lbMain.Items.AddRange(DefaultsFunc().OfType(Of Profile).ToArray())
            End Select
        End Using

        UpdateControls()
    End Sub

    Protected Overrides Sub OnClosed(e As EventArgs)
        MyBase.OnClosed(e)

        If DialogResult = DialogResult.OK Then
            Profiles.Clear()

            For Each i As Profile In lbMain.Items
                Profiles.Add(i)
            Next

            g.SaveSettings()
        End If
    End Sub

    Sub ProfilesForm_HelpRequested(sender As Object, e As EventArgs) Handles Me.HelpRequested
        Dim form As New HelpForm()
        form.Doc.WriteStart(Text)
        form.Doc.WriteTips(TipProvider.GetTips)
        form.Show()
    End Sub
End Class