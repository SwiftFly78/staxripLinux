
Imports StaxRip.UI

Public Class EventCommandEditor
    Inherits DialogBase

    Public WithEvents tbName As TextEdit
    Public WithEvents cbEvent As ComboBoxEx
    Public TipProvider As TipProvider
    Public WithEvents CriteriaControl As CriteriaControl
    Public WithEvents tbCommand As TextEdit
    Public WithEvents bnCommand As ButtonEx
    Public WithEvents cmsCommands As ContextMenuStripEx
    Public WithEvents pgParameters As PropertyGridEx
    Public lParameters As LabelEx
    Public WithEvents rbMatchAllCriteria As RadioButton
    Public WithEvents rbMatchAnyCriteria As RadioButton
    Public gbCriteria, gbCommand, gbName, gbEvent As GroupBox
    Public WithEvents bnCancel As ButtonEx
    Public WithEvents bnOK As ButtonEx
    Public WithEvents bnAdd As ButtonEx

    Private Sub InitializeComponent()
        tbName = New TextEdit
        cbEvent = New ComboBoxEx
        TipProvider = New TipProvider
        CriteriaControl = New CriteriaControl
        tbCommand = New TextEdit With {.ReadOnly = True}
        cmsCommands = New ContextMenuStripEx
        bnCommand = New ButtonEx With {.ShowMenuSymbol = True, .ContextMenuStrip = cmsCommands}
        pgParameters = New PropertyGridEx
        lParameters = New LabelEx With {.Text = "Parameters:"}
        rbMatchAllCriteria = New RadioButton With {.Text = "Match All"}
        rbMatchAnyCriteria = New RadioButton(rbMatchAllCriteria) With {.Text = "Match Any"}
        bnAdd = New ButtonEx("Add")
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)

        gbName = New GroupBox With {.Text = "Name", .Content = tbName}
        gbEvent = New GroupBox With {.Text = "Event", .Content = cbEvent}
        gbCriteria = New GroupBox With {
            .Text = "Criteria",
            .Content = New TableLayout(Row(HRow(rbMatchAllCriteria, rbMatchAnyCriteria, bnAdd)), ScaledRow(CriteriaControl)) With {.Spacing = DefaultSpacing}}
        gbCommand = New GroupBox With {
            .Text = "Command",
            .Content = New TableLayout(
                Row(New TableLayout(Row(Scaled(tbCommand), bnCommand)) With {.Spacing = DefaultSpacing}),
                Row(lParameters),
                ScaledRow(pgParameters)) With {.Spacing = DefaultSpacing}}

        Content = PaddedTable(
            Row(New TableLayout(Row(Scaled(gbName), Scaled(gbEvent))) With {.Spacing = DefaultSpacing}),
            ScaledRow(gbCriteria),
            ScaledRow(gbCommand),
            Row(ButtonRow(bnOK, bnCancel)))

        DefaultButton = bnOK
        AbortButton = bnCancel
        Text = $"Event Command Editor - {g.DefaultCommands.GetApplicationDetails()}"
    End Sub

    Private GridTypeDescriptor As New GridTypeDescriptor
    Private CommandParameters As CommandParameters
    Private EventCommandValue As EventCommand

    Sub New(ev As EventCommand)
        MyBase.New()
        InitializeComponent()
        ScaleClientSize(50, 33)
        EventCommandValue = ev
        tbName.Text = EventCommandValue.Name

        cbEvent.Items.AddRange(ListBag(Of ApplicationEvent).GetBagsForEnumType)
        ListBag(Of ApplicationEvent).SelectItem(cbEvent, EventCommandValue.Event)

        rbMatchAllCriteria.Checked = Not ev.OrOnly
        rbMatchAnyCriteria.Checked = ev.OrOnly

        Dim allCriteria As New List(Of Criteria)

        For Each m In Macro.GetMacros(True, False, True, True, False)
            Dim c = Criteria.Create(m.Type)
            c.Name = m.FriendlyName
            c.Description = m.Description
            c.Macro = m.Name
            allCriteria.Add(c)
        Next

        allCriteria.Sort(New Comparer(Of Criteria)(NameOf(Criteria.Name)))

        CriteriaControl.AllCriteria = allCriteria
        CriteriaControl.CriteriaList = EventCommandValue.CriteriaList

        If EventCommandValue.CommandParameters IsNot Nothing Then
            CommandParameters = ObjectHelp.GetCopy(EventCommandValue.CommandParameters)
        End If

        SetCommandParameters(CommandParameters)
        Command.PopulateCommandMenu(cmsCommands.Items, New List(Of Command)(g.MainForm.CustomMainMenu.CommandManager.Commands.Values), AddressOf MenuClick)

        For Each i In cmsCommands.Items.OfType(Of MenuItemEx)
            If i.Text = "Dynamic" Then
                i.Visible = False
            End If
        Next

        TipProvider.SetTip("Parameters used to execute the command.", pgParameters, lParameters)
        TipProvider.SetTip("Criteria can be defined optionally to execute the command only if the criteria is matched.", CriteriaControl, gbCriteria)
        TipProvider.SetTip("Command to be executed.", tbCommand, gbCommand)
        TipProvider.SetTip("The event on which the command gets executed.", cbEvent, gbEvent)
        TipProvider.SetTip("Name of the Event Command.", tbName, gbName)
    End Sub

    Sub MenuClick(c As Command)
        CommandParameters = New CommandParameters(c.MethodInfo.Name, c.GetDefaultParameters.ToArray)
        SetCommandParameters(CommandParameters)
    End Sub

    Sub SetCommandParameters(cp As CommandParameters)
        If cp Is Nothing Then
            pgParameters.Visible = False
            lParameters.Visible = False
            Exit Sub
        End If

        Dim cm = g.MainForm.CustomMainMenu.CommandManager

        If Not cm.HasCommand(cp.MethodName) Then
            pgParameters.Visible = False
            lParameters.Visible = False
            Exit Sub
        End If

        Dim c = g.MainForm.CustomMainMenu.CommandManager.GetCommand(cp.MethodName)
        tbCommand.Text = cp.MethodName

        If cp.Parameters Is Nothing OrElse cp.Parameters.Count = 0 Then
            pgParameters.Visible = False
            lParameters.Visible = False
            Exit Sub
        End If

        Dim mi = c.MethodInfo

        If mi Is Nothing Then
            pgParameters.Visible = False
            lParameters.Visible = False
            Exit Sub
        End If

        pgParameters.Visible = True
        lParameters.Visible = True

        GridTypeDescriptor.Items.Clear()

        Dim params = mi.GetParameters

        For i = 0 To params.Length - 1
            Dim gp As New GridProperty With {
                .Name = DispNameAttribute.GetValue(params(i).GetCustomAttributes(False))
            }

            If gp.Name Is Nothing Then
                gp.Name = params(i).Name.ToTitleCase
            End If

            gp.Value = c.FixParameters(cp.Parameters)(i)
            gp.Description = DescriptionAttributeHelp.GetDescription(params(i).GetCustomAttributes(False))
            gp.TypeEditor = EditorAttributeHelp.GetEditor(params(i).GetCustomAttributes(False))

            GridTypeDescriptor.Add(gp)
        Next

        pgParameters.SelectedObject = GridTypeDescriptor
    End Sub

    Sub EventCommandEditor_FormClosed() Handles Me.Closed
        If DialogResult = DialogResult.OK Then
            EventCommandValue.Name = tbName.Text
            EventCommandValue.Event = ListBag(Of ApplicationEvent).GetValue(cbEvent)
            EventCommandValue.CriteriaList = CriteriaControl.CriteriaList
            EventCommandValue.CommandParameters = CommandParameters
            EventCommandValue.OrOnly = rbMatchAnyCriteria.Checked
        End If
    End Sub

    Sub EventCommandEditor_HelpRequested() Handles Me.HelpRequested
        g.ShowWikiPage("Commands")
    End Sub

    Sub pgParameters_PropertyValueChanged(sender As Object, e As EventArgs) Handles pgParameters.PropertyValueChanged
        CommandParameters.Parameters.Clear()

        For Each i As DictionaryEntry In GridTypeDescriptor.Items
            CommandParameters.Parameters.Add(DirectCast(i.Value, GridProperty).Value)
        Next
    End Sub

    Sub bnAdd_Click(sender As Object, e As EventArgs) Handles bnAdd.Click
        CriteriaControl.AddItem(Nothing)
    End Sub
End Class
