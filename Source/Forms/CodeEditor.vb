

Imports System.Text.RegularExpressions

Imports StaxRip.UI

Public Class CodeEditor
    Inherits DialogBase

    Public WithEvents bnOK As ButtonEx
    Public WithEvents bnCancel As ButtonEx
    Public MainFlowLayoutPanel As StackLayout

    Private Sub InitializeComponent()
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        MainFlowLayoutPanel = New StackLayout With {
            .Orientation = Orientation.Vertical,
            .HorizontalContentAlignment = HorizontalAlignment.Stretch,
            .Spacing = FH(0.4)}

        Content = DialogContent(ButtonRow(bnOK, bnCancel),
            ScaledRow(New Scrollable With {.Content = MainFlowLayoutPanel, .Border = BorderType.None, .ExpandContentWidth = True}))

        AbortButton = bnCancel
        Text = $"Code Editor - {g.DefaultCommands.GetApplicationDetails()}"
    End Sub

    ReadOnly Property Tables As List(Of FilterTable)
        Get
            Return MainFlowLayoutPanel.Items.Select(Function(i) i.Control).OfType(Of FilterTable).ToList
        End Get
    End Property

    Sub AddTable(table As FilterTable, Optional index As Integer = -1)
        table.Editor = Me
        table.rtbScript.ContextMenuStrip = cmsMain

        If index < 0 OrElse index >= MainFlowLayoutPanel.Items.Count Then
            MainFlowLayoutPanel.Items.Add(New StackLayoutItem(table))
        Else
            MainFlowLayoutPanel.Items.Insert(index, New StackLayoutItem(table))
        End If
    End Sub

    Sub RemoveTable(table As FilterTable)
        Dim item = MainFlowLayoutPanel.Items.FirstOrDefault(Function(i) i.Control Is table)

        If item IsNot Nothing Then
            MainFlowLayoutPanel.Items.Remove(item)
        End If
    End Sub

    Function IndexOfTable(table As FilterTable) As Integer
        Return Tables.IndexOf(table)
    End Function

    Sub MoveTable(table As FilterTable, index As Integer)
        RemoveTable(table)
        AddTable(table, index)
    End Sub

    Property ActiveTable As FilterTable
    Property Engine As ScriptEngine
    Property PreviewScript As VideoScript

    Private CustomMenu As CustomMenu
    Private CommandManager As New CommandManager

    Public WithEvents cmsMain As ContextMenuStripEx

    Sub New(doc As VideoScript)
        InitializeComponent()

        Engine = doc.Engine

        cmsMain = New ContextMenuStripEx()
        CommandManager.AddCommandsFromObject(Me)
        CommandManager.AddCommandsFromObject(g.DefaultCommands)

        CustomMenu = New CustomMenu(AddressOf GetDefaultMenu, s.CustomMenuCodeEditor, CommandManager, cmsMain.Items)
        CustomMenu.AddKeyDownHandler(Me)
        CustomMenu.BuildMenu()

        ModifyMenu()

        For Each i In doc.Filters
            AddTable(CreateFilterTable(i))
        Next

        Dim maxWidth = Tables.Select(Function(t) t.TextWidth).DefaultIfEmpty(0).Max
        ClientSize = New Size(Math.Min(FH(70), Math.Max(FH(40), maxWidth + FH(14))), FH(30))
    End Sub

    Sub ModifyMenu()
        CustomMenu.GetMenuItemByText("Cut").ShortcutKeyDisplayString = "Ctrl+X"
        CustomMenu.GetMenuItemByText("Copy").ShortcutKeyDisplayString = "Ctrl+C"
        CustomMenu.GetMenuItemByText("Paste").ShortcutKeyDisplayString = "Ctrl+V"

        For Each pack In Package.Items.Values
            cmsMain.Add("Help | " + pack.Name.Substring(0, 1).ToUpperInvariant + " | " + pack.ID, Sub() pack.ShowHelp())
        Next
    End Sub

    <Command("Opens the menu editor.")>
    Sub OpenMenuEditor()
        Dim ret = CustomMenu.Edit()

        If Not ret Is s.CustomMenuCodeEditor Then
            ModifyMenu()
        End If

        s.CustomMenuCodeEditor = ret
        g.SaveSettings()
    End Sub

    <Command("Applies the current filter state.")>
    Sub ApplyFilters()
        If PreviewScript Is Nothing Then Exit Sub

        PreviewScript.Filters = GetFilters()
        PreviewScript.RemoveFilter("Cutting")
    End Sub

    <Command("Removes a filter.")>
    Sub RemoveFilter()
        If Not ActiveTable Is Nothing Then
            If MsgConfirm("Remove the filter?", Nothing, "Remove") Then
                If Tables.Count > 1 Then
                    RemoveTable(ActiveTable)
                    ActiveTable = Nothing
                End If
            End If
        End If
    End Sub

    <Command("Moves a filter up.")>
    Sub MoveFilterUp()
        If ActiveTable Is Nothing Then Exit Sub
        Dim index = IndexOfTable(ActiveTable)
        index -= 1

        If index < 0 Then
            index = 0
        End If

        MoveTable(ActiveTable, index)
    End Sub

    <Command("Moves a filter down.")>
    Sub MoveFilterDown()
        If ActiveTable Is Nothing Then Exit Sub
        Dim index = IndexOfTable(ActiveTable)
        index += 1

        If index > Tables.Count - 1 Then
            index = Tables.Count - 1
        End If

        MoveTable(ActiveTable, index)
    End Sub

    <Command("Shows a code preview with solved macros.")>
    Sub ShowCodePreview()
        Dim script As New VideoScript With {
            .Engine = Engine,
            .Filters = GetFilters()
        }
        g.ShowCommandLinePreview("", script.GetFullScript(), True)
    End Sub

    <Command("Shows script parameters like framecount and colorspace.")>
    Sub ShowInfo()
        Dim script = CreateTempScript()

        If script IsNot Nothing Then
            g.ShowScriptInfo(script)
        End If
    End Sub

    <Command("Shows advanced script parameters.")>
    Sub ShowAdvancedInfo()
        Dim script = CreateTempScript()

        If script Is Nothing Then
            Exit Sub
        End If

        g.ShowAdvancedScriptInfo(script)
    End Sub

    <Command("Cuts selected text.")>
    Sub Cut()
        If ActiveTable IsNot Nothing Then
            Clipboard.SetText(ActiveTable.rtbScript.SelectedText)
            ActiveTable.rtbScript.SelectedText = ""
        End If
    End Sub

    <Command("Copies selected text.")>
    Sub Copy()
        If ActiveTable IsNot Nothing Then
            Clipboard.SetText(ActiveTable.rtbScript.SelectedText)
        End If
    End Sub

    <Command("Pastes selected text.")>
    Sub Paste()
        If Not ActiveTable Is Nothing Then
            ActiveTable.rtbScript.SelectedText = Clipboard.GetText
            ActiveTable.rtbScript.ScrollToCaret()
        End If
    End Sub

    <Command("Dialog to configure filter profiles.")>
    Sub ShowFilterProfilesDialog()
        g.MainForm.ShowFilterProfilesDialog()
        PopulateDynamicMenu(DynamicMenuItemID.AddFilters)
        PopulateDynamicMenu(DynamicMenuItemID.InsertFilters)
        PopulateDynamicMenu(DynamicMenuItemID.ReplaceFilters)
    End Sub

    <Command("Plays the script with mpv.")>
    Sub PlayWithMpvnet()
        g.PlayScriptWithMPV(CreateTempScript())
    End Sub

    <Command("Plays the script with mpc.")>
    Sub PlayWithMPC()
        g.PlayScriptWithMPC(CreateTempScript())
    End Sub

    <Command("Shows the video preview.")>
    Sub ShowVideoPreview()
        If p.SourceFile = "" Then
            Exit Sub
        End If

        If PreviewScript Is Nothing Then
            PreviewScript = CreateTempScript()
            If PreviewScript Is Nothing Then
                Exit Sub
            End If
            PreviewScript.RemoveFilter("Cutting")
        Else
            ApplyFilters()
        End If

        Dim form As New PreviewForm(PreviewScript)
        form.Show()
    End Sub

    <Command("Joins all active filters into one filter.")>
    Sub JoinActiveFilters()
        Dim activeTables = Tables.Where(Function(i) i.cbActive.Checked AndAlso Not {"source", "crop", "cutting", "resize", "rotation"}.Contains(i.cbActive.Text.ToLowerInvariant()))
        Dim firstActiveTable = activeTables?.FirstOrDefault()

        If firstActiveTable Is Nothing Then Return

        firstActiveTable.tbName.Text = "Actives merged"
        firstActiveTable.cbActive.Text = "Misc"
        firstActiveTable.rtbScript.Text = activeTables.Select(Function(arg) arg.rtbScript.Text.Trim).Join(BR) + BR

        For x = activeTables.Count() - 1 To 1 Step -1
            RemoveTable(activeTables.ElementAt(x))
        Next
    End Sub

    <Command("Joins all filters into one filter.")>
    Sub JoinAllFilters()
        Dim joinTables = Tables.Where(Function(i) Not {"source", "crop", "cutting", "resize", "rotation"}.Contains(i.cbActive.Text.ToLowerInvariant())).ToList
        Dim firstTable = joinTables.FirstOrDefault()

        If firstTable Is Nothing Then Return

        firstTable.tbName.Text = "Merged"
        firstTable.cbActive.Text = "Misc"
        firstTable.rtbScript.Text = joinTables.Select(Function(arg) If(arg.cbActive.Checked, arg.rtbScript.Text.Trim, "# " + arg.rtbScript.Text.Trim.FixBreak.Replace(BR, BR + "# "))).Join(BR) + BR

        For x = joinTables.Count - 1 To 1 Step -1
            RemoveTable(joinTables(x))
        Next
    End Sub

    <Command("Joins all inactive filters into one filter.")>
    Sub JoinInactiveFilters()
        Dim inactiveTables = Tables.Where(Function(i) Not i.cbActive.Checked AndAlso Not {"source", "crop", "cutting", "resize", "rotation"}.Contains(i.cbActive.Text.ToLowerInvariant()))
        Dim firstInactiveTable = inactiveTables?.FirstOrDefault()

        If firstInactiveTable Is Nothing Then Return

        firstInactiveTable.tbName.Text = "Inactives merged"
        firstInactiveTable.cbActive.Text = "Misc"
        firstInactiveTable.rtbScript.Text = inactiveTables.Select(Function(arg) arg.rtbScript.Text.Trim).Join(BR) + BR

        For x = inactiveTables.Count() - 1 To 1 Step -1
            RemoveTable(inactiveTables.ElementAt(x))
        Next
    End Sub

    Sub SetParameters(parameters As FilterParameters)
        ActiveTable?.SetParameters(parameters)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        Tables.FirstOrDefault?.rtbScript.Focus()
        PopulateDynamicMenu(DynamicMenuItemID.AddFilters)
        PopulateDynamicMenu(DynamicMenuItemID.InsertFilters)
        PopulateDynamicMenu(DynamicMenuItemID.ReplaceFilters)
    End Sub

    Sub PopulateDynamicMenu(id As DynamicMenuItemID, Optional text As String = Nothing)
        Dim filterProfiles = If(p.Script.IsAviSynth, s.AviSynthProfiles, s.VapourSynthProfiles)

        For Each i In CustomMenu.MenuItems
            If i.CustomMenuItem.MethodName = "DynamicMenuItem" AndAlso
                i.CustomMenuItem.Parameters(0).Equals(id) Then

                i.DropDownItems.ClearAndDisplose

                Select Case id
                    Case DynamicMenuItemID.InsertFilters, DynamicMenuItemID.ReplaceFilters, DynamicMenuItemID.AddFilters
                        Dim action As Action(Of VideoFilter) = Nothing

                        Select Case id
                            Case DynamicMenuItemID.InsertFilters
                                action = AddressOf InsertFilter
                            Case DynamicMenuItemID.ReplaceFilters
                                action = AddressOf ReplaceFilter
                            Case DynamicMenuItemID.AddFilters
                                action = AddressOf AddFilter
                        End Select

                        For Each filterCategory In filterProfiles
                            For Each filter In filterCategory.Filters
                                Dim tip = filter.Script
                                MenuItemEx.Add(i.DropDownItems, filterCategory.Name + " | " + filter.Path, action, filter.GetCopy, tip)
                            Next
                        Next

                        MenuItemEx.Add(i.DropDownItems, "Empty Filter", action, New VideoFilter("Misc", "", ""), "Filter with empty values.")
                    Case DynamicMenuItemID.FilterCategory
                        i.Text = text

                        For Each iCategory In filterProfiles
                            If iCategory.Name = ActiveTable.cbActive.Text Then
                                Dim cat = iCategory

                                For Each iFilter In cat.Filters
                                    Dim tip = iFilter.Script
                                    MenuItemEx.Add(i.DropDownItems, iFilter.Path, AddressOf ReplaceFilter, iFilter.GetCopy, tip)
                                Next
                            End If
                        Next
                End Select

                Exit For
            End If
        Next
    End Sub

    Sub AddFilter(filter As VideoFilter)
        ActiveTable?.Add(filter)
    End Sub

    Sub ReplaceFilter(filter As VideoFilter)
        ActiveTable?.ReplaceFilter(filter)
    End Sub

    Sub InsertFilter(filter As VideoFilter)
        ActiveTable?.Insert(filter)
    End Sub

    Shared Function GetDefaultMenu() As CustomMenuItem
        Dim ret As New CustomMenuItem("Root")

        ret.Add("Category", NameOf(g.DefaultCommands.DynamicMenuItem), {DynamicMenuItemID.FilterCategory})
        ret.Add("-")
        ret.Add("Replace", NameOf(g.DefaultCommands.DynamicMenuItem), Symbol.Switch, {DynamicMenuItemID.ReplaceFilters})
        ret.Add("Insert", NameOf(g.DefaultCommands.DynamicMenuItem), Symbol.LeftArrowKeyTime0, {DynamicMenuItemID.InsertFilters})
        ret.Add("Add", NameOf(g.DefaultCommands.DynamicMenuItem), Symbol.Add, {DynamicMenuItemID.AddFilters})
        ret.Add("-")
        ret.Add("Apply all Filters", NameOf(ApplyFilters), Keys.Control Or Keys.S, Symbol.Save)
        ret.Add("Preview Video...", NameOf(ShowVideoPreview), Keys.F6, Symbol.fa_eye)
        ret.Add("Preview Code...", NameOf(ShowCodePreview), Keys.F4, Symbol.Code)
        ret.Add("Play", NameOf(PlayWithMpvnet), Keys.F9, Symbol.Play)
        ret.Add("Info...", NameOf(ShowInfo), Keys.F2, Symbol.Info)
        ret.Add("Advanced Info...", NameOf(ShowAdvancedInfo), Keys.Control Or Keys.F2, Symbol.Lightbulb)
        ret.Add("Join... | All Filters", NameOf(JoinAllFilters), Keys.Control Or Keys.J)
        ret.Add("Join... | Active Filters", NameOf(JoinActiveFilters), Keys.Control Or Keys.Alt Or Keys.J)
        ret.Add("Join... | Inactive Filters", NameOf(JoinInactiveFilters), Keys.Control Or Keys.Shift Or Keys.J)
        ret.Add("Profiles...", NameOf(ShowFilterProfilesDialog), Keys.Control Or Keys.P, Symbol.FavoriteStar)
        ret.Add("Macros...", NameOf(g.DefaultCommands.ShowMacrosDialog), Keys.Control Or Keys.M, Symbol.CalculatorPercentage)
        ret.Add("-")
        ret.Add("Move Up", NameOf(MoveFilterUp), Keys.Control Or Keys.Up, Symbol.Up)
        ret.Add("Move Down", NameOf(MoveFilterDown), Keys.Control Or Keys.Down, Symbol.Down)
        ret.Add("-")
        ret.Add("Remove", NameOf(RemoveFilter), Keys.Control Or Keys.Delete, Symbol.Remove)
        ret.Add("Cut", NameOf(Cut), Symbol.Cut)
        ret.Add("Copy", NameOf(Copy), Symbol.Copy)
        ret.Add("Paste", NameOf(Paste), Symbol.Paste)
        ret.Add("-")
        ret.Add("Edit Menu...", NameOf(OpenMenuEditor))

        Return ret
    End Function

    Shared Function CreateFilterTable(filter As VideoFilter) As FilterTable
        Dim ret As New FilterTable

        ret.cbActive.Checked = filter.Active
        ret.cbActive.Text = filter.Category
        ret.tbName.Text = filter.Name
        ret.rtbScript.Text = If(filter.Script = "", "", filter.Script + BR)
        ret.SetColor()

        Return ret
    End Function

    Function GetFilters() As List(Of VideoFilter)
        Dim ret As New List(Of VideoFilter)

        For Each table As FilterTable In Tables
            Dim filter As New VideoFilter()
            filter.Active = table.cbActive.Checked
            filter.Category = table.cbActive.Text
            filter.Path = table.tbName.Text
            filter.Script = table.rtbScript.Text.FixBreak.Trim
            ret.Add(filter)
        Next

        Return ret
    End Function

    Function CreateTempScript() As VideoScript
        Dim script As New VideoScript
        script.Engine = Engine
        script.Path = Path.Combine(p.TempDir, p.TargetFile.Base + $"_temp." + script.FileType)
        script.Filters = GetFilters()
        Dim err = script.GetError()

        If err <> "" Then
            MsgError("Script Error", err)
            Exit Function
        End If

        Return script
    End Function

    Sub CodeEditor_HelpRequested(sender As Object, e As EventArgs) Handles Me.HelpRequested
        Dim form As New HelpForm()
        form.Doc.WriteStart("Code Editor")
        form.Doc.WriteTips(CustomMenu.GetTips)
        form.Doc.WriteTable("Shortcut Keys", CustomMenu.GetKeys, False)
        form.Show()
    End Sub

    Sub cmsMain_Opening(sender As Object, e As EventArgs) Handles cmsMain.Opening
        If ActiveTable Is Nothing Then
            ActiveTable = Tables.FirstOrDefault
        End If

        If ActiveTable Is Nothing Then
            Exit Sub
        End If

        Dim topItems = cmsMain.Items.OfType(Of MenuItemEx).Where(Function(item) TypeOf item.Tag Is String AndAlso CStr(item.Tag) = "top").ToArray

        For Each i In topItems
            cmsMain.Items.Remove(i)
        Next

        Dim filterProfiles = If(p.Script.IsAviSynth, s.AviSynthProfiles, s.VapourSynthProfiles)
        Dim code = ActiveTable.rtbScript.Text.FixBreak
        Dim tempItem As New MenuItemEx

        For Each i In FilterParameters.Definitions
            If code.Contains(i.FunctionName + "(") Then
                Dim match = Regex.Match(code, i.FunctionName + "\((.+)\)")

                If match.Success Then
                    MenuItemEx.Add(tempItem.DropDownItems, i.Text, Sub() SetParameters(i))
                End If
            End If
        Next

        For Each i In tempItem.DropDownItems.OfType(Of MenuItemEx).ToArray
            i.Tag = "top"
            tempItem.DropDownItems.Remove(i)
            cmsMain.Items.Insert(0, i)
        Next

        EnableMenuItems()

        PopulateDynamicMenu(DynamicMenuItemID.FilterCategory, ActiveTable.cbActive.Text)
    End Sub

    Sub EnableMenuItems()
        CustomMenu.EnableMenuItemByActionName(NameOf(ShowVideoPreview), p.SourceFile <> "")
        CustomMenu.EnableMenuItemByActionName(NameOf(PlayWithMpvnet), p.SourceFile <> "")
        CustomMenu.EnableMenuItemByActionName(NameOf(ShowInfo), p.SourceFile <> "")
        CustomMenu.EnableMenuItemByActionName(NameOf(ShowAdvancedInfo), p.SourceFile <> "")
        CustomMenu.EnableMenuItemByActionName(NameOf(JoinAllFilters), Tables.Where(Function(i) Not {"source", "crop", "cutting", "resize", "rotation"}.Contains(i.cbActive.Text.ToLowerInvariant())).Count > 1)
        CustomMenu.EnableMenuItemByActionName(NameOf(JoinActiveFilters), Tables.Where(Function(i) i.cbActive.Checked AndAlso Not {"source", "crop", "cutting", "resize", "rotation"}.Contains(i.cbActive.Text.ToLowerInvariant())).Count() > 1)
        CustomMenu.EnableMenuItemByActionName(NameOf(JoinInactiveFilters), Tables.Where(Function(i) Not i.cbActive.Checked AndAlso Not {"source", "crop", "cutting", "resize", "rotation"}.Contains(i.cbActive.Text.ToLowerInvariant())).Count() > 1)
        CustomMenu.EnableMenuItemByActionName(NameOf(Cut), ActiveTable.rtbScript.SelectionLength > 0 AndAlso Not ActiveTable.rtbScript.ReadOnly)
        CustomMenu.EnableMenuItemByActionName(NameOf(Copy), ActiveTable.rtbScript.SelectionLength > 0)
        CustomMenu.EnableMenuItemByActionName(NameOf(Paste), Clipboard.GetText <> "" AndAlso Not ActiveTable.rtbScript.ReadOnly)
    End Sub

    Public Class FilterTable
        Inherits Panel

        Property tbName As New TextEdit
        Property rtbScript As RichTextBoxEx
        Property cbActive As New CheckBoxEx
        Property Editor As CodeEditor

        Private ReadOnly Accent As New Panel With {.Width = FH(0.2)}

        ReadOnly Property Theme As Theme
            Get
                Return ThemeManager.CurrentTheme
            End Get
        End Property

        ReadOnly Property TextWidth As Integer
            Get
                Dim lines = rtbScript.Text.FixBreak.Split({BR}, StringSplitOptions.None)
                Dim longest = lines.OrderByDescending(Function(i) i.Length).FirstOrDefault

                Return MeasureTextWidth(If(longest, ""), rtbScript.Font) + FH(2)
            End Get
        End Property

        Sub New()
            rtbScript = New RichTextBoxEx(False, False) With {
                .WordWrap = False,
                .AcceptsTab = True,
                .Font = FontManager.GetCodeFont()}

            tbName.Width = FH(9)

            AddHandler cbActive.CheckedChanged, Sub()
                                                    SetColor()
                                                    Editor?.ApplyFilters()
                                                End Sub

            AddHandler rtbScript.GotFocus, Sub()
                                               If Editor Is Nothing Then Exit Sub
                                               Editor.ActiveTable = Me
                                               Editor.EnableMenuItems()
                                               Editor.ApplyFilters()
                                           End Sub

            AddHandler rtbScript.TextChanged, Sub() UpdateHeight()

            Dim left As New TableLayout(Row(cbActive), Row(tbName), New TableRow With {.ScaleHeight = True}) With {
                .Spacing = New Size(0, FH(0.2))}

            Content = New TableLayout(New TableRow(New TableCell(Accent), New TableCell(left), New TableCell(rtbScript, True))) With {
                .Spacing = New Size(FH(0.4), 0)}

            UpdateHeight()
        End Sub

        Sub UpdateHeight()
            Dim lineCount = Math.Max(1, rtbScript.Text.FixBreak.TrimEnd.Split({BR}, StringSplitOptions.None).Length)
            lineCount = Math.Min(lineCount + 1, 16)
            Dim lineHeight = rtbScript.Font.LineHeight
            rtbScript.Height = CInt(Math.Max(FH(3.2), lineCount * lineHeight + FH(0.8)))
        End Sub

        Sub SetColor()
            If cbActive.Checked Then
                Accent.BackgroundColor = Theme.CodeEditor.ForeAccentColor.ToColor
                cbActive.TextColor = Theme.CodeEditor.ForeAccentColor.ToColor
            Else
                Accent.BackgroundColor = If(IsDarkMode, Color.FromArgb(90, 90, 90), Color.FromArgb(190, 190, 190))
                cbActive.TextColor = SystemColors.ControlText
            End If
        End Sub

        Sub SetParameters(parameters As FilterParameters)
            Dim code = rtbScript.Text.FixBreak
            Dim match = Regex.Match(code, parameters.FunctionName + "\((.+)\)")
            Dim args = FilterParameters.SplitCSV(match.Groups(1).Value)
            Dim newParameters As New List(Of String)

            For Each argument In args
                Dim skip = False

                For Each parameter In parameters.Parameters
                    If argument.ToLowerInvariant.RemoveChars(" ").Contains(
                        parameter.Name.ToLowerInvariant.RemoveChars(" ") + "=") Then

                        skip = True
                    End If
                Next

                If Not skip Then
                    newParameters.Add(argument)
                End If
            Next

            For Each parameter In parameters.Parameters
                Dim value = parameter.Value

                If Editor.Engine = ScriptEngine.VapourSynth Then
                    value = value.Replace("""", "'")
                End If

                newParameters.Add(parameter.Name + "=" + value)
            Next

            rtbScript.Text = Regex.Replace(code, parameters.FunctionName + "\((.+)\)",
                parameters.FunctionName + "(" + newParameters.Join(", ") + ")")
        End Sub

        Sub Add(filter As VideoFilter)
            Dim tup = Macro.ExpandGUI(filter.Script)

            If tup.Cancel Then
                Exit Sub
            End If

            If tup.Value <> filter.Script AndAlso tup.Caption <> "" Then
                If filter.Script.StartsWith("$") Then
                    filter.Path = tup.Caption
                Else
                    filter.Path = filter.Path.Replace("...", "") + " " + tup.Caption
                End If
            End If

            filter.Script = tup.Value
            Dim filterTable = CodeEditor.CreateFilterTable(filter)
            Editor.AddTable(filterTable)
            filterTable.rtbScript.SelectionStart = filterTable.rtbScript.Text.Length
            filterTable.rtbScript.Focus()
        End Sub

        Sub Insert(filter As VideoFilter)
            Dim tup = Macro.ExpandGUI(filter.Script)

            If tup.Cancel Then
                Exit Sub
            End If

            If tup.Value <> filter.Script AndAlso tup.Caption <> "" Then
                If filter.Script.StartsWith("$") Then
                    filter.Path = tup.Caption
                Else
                    filter.Path = filter.Path.Replace("...", "") + " " + tup.Caption
                End If
            End If

            filter.Script = tup.Value
            Dim index = Editor.IndexOfTable(Me)
            Dim filterTable = CodeEditor.CreateFilterTable(filter)
            Editor.AddTable(filterTable, index)
            filterTable.rtbScript.SelectionStart = filterTable.rtbScript.Text.Length
            filterTable.rtbScript.Focus()
        End Sub

        Sub ReplaceFilter(filter As VideoFilter)
            Dim tup = Macro.ExpandGUI(filter.Script)

            If tup.Cancel Then
                Exit Sub
            End If

            cbActive.Checked = filter.Active
            cbActive.Text = filter.Category

            If tup.Value <> filter.Script AndAlso tup.Caption <> "" Then
                If filter.Script.StartsWith("$") Then
                    tbName.Text = tup.Caption
                Else
                    tbName.Text = filter.Name.Replace("...", "") + " " + tup.Caption
                End If
            Else
                tbName.Text = filter.Name
            End If

            rtbScript.Text = tup.Value.TrimEnd + BR
            rtbScript.SelectionStart = rtbScript.Text.Length
        End Sub
    End Class
End Class