
Imports System.Collections.Concurrent
Imports System.Threading
Imports System.Threading.Tasks
Imports StaxRip.VideoEncoderCommandLine
Imports StaxRip.UI

Public Class CommandLineForm
    Inherits DialogBase

    Public WithEvents bnCancel As ButtonEx
    Public WithEvents bnOK As ButtonEx
    Public WithEvents SimpleUI As SimpleUI
    Public WithEvents cbGoTo As ComboBoxEx
    Public WithEvents bnMenu As ButtonEx
    Public WithEvents cms As ContextMenuStripEx
    Public WithEvents rtbCommandLine As CommandLineRichTextBox
    Public WithEvents cmsCommandLine As ContextMenuStripEx

    Private Sub InitializeComponent()
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        SimpleUI = New SimpleUI
        cbGoTo = New ComboBoxEx With {.DropDownStyle = ComboBoxStyle.DropDown, .ToolTip = "Search"}
        cms = New ContextMenuStripEx
        bnMenu = New ButtonEx With {.ShowMenuSymbol = True, .ContextMenuStrip = cms}
        cmsCommandLine = New ContextMenuStripEx
        rtbCommandLine = New CommandLineRichTextBox With {.ReadOnly = True, .Font = FontManager.GetCodeFont()}

        Content = PaddedTable(
            ScaledRow(SimpleUI),
            Row(rtbCommandLine),
            Row(New TableLayout(Row(Scaled(cbGoTo), bnMenu, bnOK, bnCancel)) With {.Spacing = DefaultSpacing}))

        AbortButton = bnCancel
    End Sub

    Private Params As CommandLineParams
    Private SearchIndex As Integer
    Private Items As New List(Of Item)
    Private HighlightedControl As Control
    Private CommandLineHighlightingMenuItem As MenuItemEx
    Private CommandLineMouseUpSearchMenuItem As MenuItemEx
    Private GoToComboBoxCts As CancellationTokenSource

    Property HTMLHelpFunc As Func(Of String)

    Event BeforeHelp()

    Sub New(params As CommandLineParams)
        InitializeComponent()
        ScaleClientSize(52, 36)
        rtbCommandLine.ContextMenuStrip = cmsCommandLine

        Dim singleList As New List(Of String)

        For Each param In params.Items
            If param.GetKey = "" OrElse singleList.Contains(param.GetKey) Then
                'Throw New Exception("key found twice: " + param.GetKey)
            Else
                singleList.Add(param.GetKey)
            End If
        Next

        Me.Params = params
        Text = $"{params.Title} ({params.Items.Count} options) - {g.DefaultCommands.GetApplicationDetails()}"

        InitUI()
        SelectLastPage()
        AddHandler params.ValueChanged, AddressOf ValueChanged
        params.RaiseValueChanged(Nothing)

        cbGoTo.Focus()

        cms.Form = Me

        cms.Add("Execute Command Line", Sub() params.Execute(), Keys.Control Or Keys.E, p.SourceFile <> "")

        Dim a = Sub()
                    Clipboard.SetText(params.GetCommandLine(True, True))
                    MsgInfo("Command Line was copied.")
                End Sub

        cms.Add("Copy Command Line", a, Keys.Control Or Keys.Shift Or Keys.C).SetImage(Symbol.Copy)
        cms.Add("Show Command Line...", Sub() g.ShowCommandLinePreview("Command Line", params.GetCommandLinePreview, False), Keys.F4, Symbol.Code)
        cms.Add("Import Command Line...", Sub() If MsgQuestion("Import command line from clipboard?", Clipboard.GetText) = DialogResult.OK Then BasicVideoEncoder.ImportCommandLine(Clipboard.GetText, params), Keys.Control Or Keys.I).SetImage(Symbol.Download)

        cms.Add("-")

        a = Sub()
                CommandLineHighlightingMenuItem.Marked = Not CommandLineHighlightingMenuItem.Marked
                s.CommandLineHighlighting = CommandLineHighlightingMenuItem.Marked
                rtbCommandLine.Format(rtbCommandLine.Text.ToString)
            End Sub

        CommandLineHighlightingMenuItem = cms.Add("Command Line Highlighting", a, Keys.Control Or Keys.H)
        CommandLineHighlightingMenuItem.Marked = s.CommandLineHighlighting

        a = Sub()
                CommandLineMouseUpSearchMenuItem.Marked = Not CommandLineMouseUpSearchMenuItem.Marked
                s.CommandLinePreviewMouseUpSearch = CommandLineMouseUpSearchMenuItem.Marked
            End Sub

        CommandLineMouseUpSearchMenuItem = cms.Add("Preview Mouse-Up Search", a, Keys.Control Or Keys.P, "Clicking on an option in the preview navigates directly to the UI control.")
        CommandLineMouseUpSearchMenuItem.Marked = s.CommandLinePreviewMouseUpSearch

        cms.Add("-")

        Dim help = cms.Add("Help about this dialog", AddressOf ShowHelp)
        help.SetImage(Symbol.Help)
        help.ShortcutKeyDisplayString = "F1"

        cms.Add("Help about " + params.Package.Name, Sub() params.Package.ShowHelp(), Keys.Control Or Keys.F1).SetImage(Symbol.Help)

    End Sub

    Sub SelectLastPage()
        SimpleUI.SelectLast(Params.Title + "page selection")
    End Sub

    Sub ValueChanged(item As CommandLineParam)
        rtbCommandLine.SetText(Params.GetCommandLine(False, False))
        rtbCommandLine.SelectionLength = 0
        rtbCommandLine.UpdateHeight()

        Task.Run(AddressOf UpdateSearchComboBox)
    End Sub

    Sub InitUI()
        Dim flowPanels As New List(Of Control)
        Dim helpControl As Control = Nothing
        Dim currentFlow As SimpleUI.FlowPage = Nothing

        For x = 0 To Params.Items.Count - 1
            Dim param = Params.Items(x)
            Dim parent As FlowLayoutPanelEx = SimpleUI.GetFlowPage(param.Path)
            currentFlow = DirectCast(parent, SimpleUI.FlowPage)

            If Not flowPanels.Contains(parent) Then
                flowPanels.Add(parent)
                parent.SuspendLayout()
            End If

            Dim help As String = Nothing

            If param.Switch <> "" Then
                help += param.Switch + BR
            End If

            If param.HelpSwitch <> "" Then
                help += param.HelpSwitch + BR
            End If

            If param.NoSwitch <> "" Then
                help += param.NoSwitch + BR
            End If

            Dim switches = param.Switches

            If Not switches.NothingOrEmpty Then
                help += switches.Join(BR) + BR
            End If

            help += BR

            If TypeOf param Is NumParam Then
                Dim nParam = DirectCast(param, NumParam)

                If nParam.Config(0) > Double.MinValue Then
                    help += "Minimum: " & nParam.Config(0) & BR
                End If

                If nParam.Config(1) < Double.MaxValue Then
                    help += "Maximum: " & nParam.Config(1) & BR
                End If
            End If

            help += BR

            If Not param.URLs.NothingOrEmpty Then
                help += String.Join(BR, param.URLs.Select(Function(val) "[" + val + " " + val + "]"))
            End If

            If param.Help <> "" Then
                help += param.Help
            End If

            If help <> "" Then
                If help.Contains(BR2 + BR) Then
                    help = help.Replace(BR2 + BR, BR2)
                End If

                If help.EndsWith(BR) Then
                    help = help.Trim
                End If
            End If

            If param.Label <> "" Then
                SimpleUI.AddLabel(parent, param.Label).MarginTop = FontHeight \ 2
            End If

            If TypeOf param Is LineParam Then
                Dim line = SimpleUI.AddLine(parent, "", 2, 2)
                DirectCast(param, LineParam).InitParam(line)
            ElseIf TypeOf param Is BoolParam Then
                Dim checkBox = SimpleUI.AddBool(parent)
                checkBox.Text = param.Text

                If param.HelpSwitch <> "" Then
                    Dim helpOptions = param.GetSwitches
                    checkBox.HelpAction = Sub() Params.ShowHelp(helpOptions)
                Else
                    checkBox.Help = help
                End If

                checkBox.MarginLeft = param.LeftMargin
                DirectCast(param, BoolParam).InitParam(checkBox)
                helpControl = checkBox
            ElseIf TypeOf param Is NumParam Then
                Dim tempNumParam = DirectCast(param, NumParam)
                Dim nParam = DirectCast(param, NumParam)
                Dim numBlock = SimpleUI.AddNum(parent)

                If param.Text <> "" Then
                    numBlock.Label.Text = If(param.Text.EndsWithEx(":"), param.Text, param.Text + ":")
                End If

                If param.HelpSwitch <> "" Then
                    Dim helpOptions = param.GetSwitches
                    numBlock.Label.HelpAction = Sub() Params.ShowHelp(helpOptions)
                Else
                    numBlock.Label.Help = help
                End If

                numBlock.NumEdit.Config = nParam.Config

                If nParam.HintText <> "" Then
                    SimpleUI.AddLabel(numBlock, nParam.HintText)
                End If

                AddHandler numBlock.Label.MouseDoubleClick, Sub() tempNumParam.Value = tempNumParam.DefaultValue
                DirectCast(param, NumParam).InitParam(numBlock.NumEdit)
                helpControl = numBlock.Label
            ElseIf TypeOf param Is OptionParam Then
                Dim tempOptionParam = DirectCast(param, OptionParam)
                Dim oParam = DirectCast(param, OptionParam)
                Dim menuBlock = SimpleUI.AddMenu(Of Integer)(parent)
                menuBlock.Label.Text = If(param.Text.EndsWith(":"), param.Text, param.Text + ":")

                If param.HelpSwitch <> "" Then
                    Dim helpOptions = param.GetSwitches
                    menuBlock.Label.HelpAction = Sub() Params.ShowHelp(helpOptions)
                    menuBlock.Button.HelpAction = Sub() Params.ShowHelp(helpOptions)
                Else
                    menuBlock.Help = help
                End If

                If oParam.HintText <> "" Then
                    SimpleUI.AddLabel(menuBlock, oParam.HintText)
                End If

                helpControl = menuBlock.Label
                AddHandler menuBlock.Label.MouseDoubleClick, Sub() tempOptionParam.ValueChangedUser(tempOptionParam.DefaultValue)

                Dim max = oParam.Options.Select(Function(txt) txt.Length).Max

                If tempOptionParam.Expanded OrElse max > 24 Then
                    menuBlock.Button.Expand = True
                End If

                For x2 = 0 To oParam.Options.Length - 1
                    menuBlock.Button.Add(oParam.Options(x2), x2)
                Next

                oParam.InitParam(menuBlock.Button)
            ElseIf TypeOf param Is StringParam Then
                Dim tempItem = DirectCast(param, StringParam)
                Dim textBlock As SimpleUI.TextBlock

                If tempItem.BrowseFileFilter <> "" Then
                    Dim textButtonBlock = SimpleUI.AddTextButton(parent)
                    textButtonBlock.BrowseFile(tempItem.BrowseFileFilter)
                    textBlock = textButtonBlock
                ElseIf tempItem.Menu <> "" Then
                    Dim textMenuBlock = SimpleUI.AddTextMenu(parent)
                    textMenuBlock.AddMenu(tempItem.Menu)
                    textBlock = textMenuBlock
                Else
                    textBlock = SimpleUI.AddText(parent)
                End If

                textBlock.Label.Text = If(param.Text.EndsWith(":"), param.Text, param.Text + ":")

                If param.HelpSwitch <> "" Then
                    Dim helpOptions = param.GetSwitches
                    textBlock.Label.HelpAction = Sub() Params.ShowHelp(helpOptions)
                Else
                    textBlock.Label.Help = help
                End If

                helpControl = textBlock.Label
                AddHandler textBlock.Label.MouseDoubleClick, Sub() tempItem.Value = tempItem.DefaultValue
                textBlock.Edit.Expand = tempItem.Expand
                tempItem.InitParam(textBlock)
            End If

            If helpControl IsNot Nothing Then
                Dim item As New Item With {
                    .Control = helpControl,
                    .Page = currentFlow,
                    .Param = param
                }
                Items.Add(item)
            End If
        Next

        For Each panel In flowPanels
            panel.ResumeLayout()
        Next
    End Sub

    Public Class Item
        Property Page As SimpleUI.FlowPage
        Property Control As Control
        Property Param As CommandLineParam
    End Class

    Protected Overrides Sub OnClosed(e As EventArgs)
        SimpleUI.SaveLast(Params.Title + "page selection")
        RemoveHandler Params.ValueChanged, AddressOf ValueChanged
        Params.DetachControls()
        MyBase.OnClosed(e)
        g.MainForm.PopulateProfileMenu(DynamicMenuItemID.EncoderProfiles)
    End Sub

    Sub CommandLineForm_HelpRequested(sender As Object, e As EventArgs) Handles Me.HelpRequested
        ShowHelp()
    End Sub

    Sub ShowHelp()
        RaiseEvent BeforeHelp()

        Dim form As New HelpForm()
        form.Doc.WriteStart(Text)

        form.Doc.WriteH2("How to use the video encoder dialog")

        form.Doc.WriteParagraph("The context help is shown by right-clicking a label, dropdown or checkbox.")

        If cbGoTo.Visible Then
            form.Doc.WriteParagraph("The Search dropdown field at the dialog bottom left lists options and can be used to quickly find options, it searches command line switches, labels and dropdowns. Multiple matches can be cycled by pressing enter.")
        End If

        form.Doc.WriteParagraph("Numeric values and dropdown menu options can be reset to their default value by double clicking on the label.")
        form.Doc.WriteParagraph("The command line preview at the bottom of the dialog has a context menu that allows to quickly find and show options.")

        If HTMLHelpFunc IsNot Nothing Then
            form.Doc.Writer.WriteRaw(HTMLHelpFunc.Invoke)
        End If

        form.Doc.WriteTips(SimpleUI.ActivePage.TipProvider.GetTips)
        form.Show()
    End Sub

    Sub cbGoTo_KeyDown(sender As Object, e As KeyEventArgs) Handles cbGoTo.KeyDown
        If e.KeyData = Keys.Enter Then
            SearchIndex += 1
            cbGoTo_TextChanged(Nothing, Nothing)
        Else
            SearchIndex = 0
        End If

        If e.KeyData = Keys.Enter Then
            e.Handled = True
        End If
    End Sub

    Sub cbGoTo_TextChanged(sender As Object, e As EventArgs) Handles cbGoTo.TextChanged
        If HighlightedControl IsNot Nothing Then
            SetControlFont(HighlightedControl, FontManager.GetDefaultFont())
            HighlightedControl = Nothing
        End If

        Dim find = cbGoTo.Text.ToLowerInvariant()
        Dim findNoSpace = find.Replace(" ", "")
        Dim matchedItems As New HashSet(Of Item)

        If find.Length > 1 Then
            For Each item In Items
                If item.Param.Switch = cbGoTo.Text OrElse
                    item.Param.NoSwitch = cbGoTo.Text OrElse
                    item.Param.HelpSwitch = cbGoTo.Text Then

                    matchedItems.Add(item)
                End If

                If item.Param.Switches IsNot Nothing Then
                    For Each switch In item.Param.Switches
                        If switch = cbGoTo.Text Then
                            matchedItems.Add(item)
                        End If
                    Next
                End If
            Next

            For Each item In Items
                If item.Param.Switch.ToLowerEx.Contains(find) OrElse
                    item.Param.NoSwitch.ToLowerEx.Contains(find) OrElse
                    item.Param.HelpSwitch.ToLowerEx.Contains(find) OrElse
                    item.Param.Help.ToLowerEx.Contains(find) OrElse
                    item.Param.Text.ToLowerEx.Contains(find) Then

                    matchedItems.Add(item)
                End If

                If item.Param.Switches IsNot Nothing Then
                    For Each switch In item.Param.Switches
                        If switch.ToLowerInvariant.Contains(find) Then
                            matchedItems.Add(item)
                        End If
                    Next
                End If

                If TypeOf item.Param Is OptionParam Then
                    Dim param = DirectCast(item.Param, OptionParam)

                    If param.Options IsNot Nothing Then
                        For Each value In param.Options
                            Dim valueNoSpace = value.Replace(" ", "")

                            If value.ToLowerInvariant.Contains(find) Then
                                matchedItems.Add(item)
                            End If

                            If valueNoSpace.ToLowerInvariant.Contains(findNoSpace) Then
                                matchedItems.Add(item)
                            End If

                            If value.ToLowerInvariant.Contains(findNoSpace) Then
                                matchedItems.Add(item)
                            End If

                            If valueNoSpace.ToLowerInvariant.Contains(find) Then
                                matchedItems.Add(item)
                            End If
                        Next
                    End If

                    If param.Values IsNot Nothing Then
                        For Each value In param.Values
                            Dim valueNoSpace = value.Replace(" ", "")

                            If value.ToLowerInvariant.Contains(find) Then
                                matchedItems.Add(item)
                            End If

                            If valueNoSpace.ToLowerInvariant.Contains(findNoSpace) Then
                                matchedItems.Add(item)
                            End If

                            If value.ToLowerInvariant.Contains(findNoSpace) Then
                                matchedItems.Add(item)
                            End If

                            If valueNoSpace.ToLowerInvariant.Contains(find) Then
                                matchedItems.Add(item)
                            End If
                        Next
                    End If
                End If
            Next

            Dim visibleItems = matchedItems.Where(Function(arg) arg.Param.Visible)

            If visibleItems.Count > 0 Then
                If SearchIndex >= visibleItems.Count Then
                    SearchIndex = 0
                End If

                Dim control = visibleItems(SearchIndex).Control
                SimpleUI.ShowPage(visibleItems(SearchIndex).Page)
                SetControlFont(control, FontManager.GetDefaultFont(0, FontStyle.Bold))
                HighlightedControl = control
                Exit Sub
            End If
        End If
    End Sub

    Private Sub UpdateSearchComboBox()
        If GoToComboBoxCts IsNot Nothing Then
            GoToComboBoxCts.Cancel()
            GoToComboBoxCts.Dispose()
        End If

        GoToComboBoxCts = New CancellationTokenSource()
        Dim token = GoToComboBoxCts.Token
        Dim queue = New ConcurrentQueue(Of String)

        Try
            Task.Run(Sub()
                         Dim source = Items.Where(Function(x) TypeOf x.Param IsNot NumParam AndAlso x.Param.Visible).Select(Function(x) x.Param)
                         Dim options = New ParallelOptions() With {.MaxDegreeOfParallelism = Environment.ProcessorCount \ 2, .CancellationToken = token}

                         Parallel.ForEach(source, options, Sub(item)
                                                               token.ThrowIfCancellationRequested()

                                                               If item.Switches IsNot Nothing Then
                                                                   For Each switch In item.Switches
                                                                       If Not queue.Contains(switch) Then
                                                                           token.ThrowIfCancellationRequested()
                                                                           queue.Enqueue(switch)
                                                                       End If
                                                                   Next
                                                               End If

                                                               If item.Switch <> "" AndAlso Not queue.Contains(item.Switch) Then
                                                                   token.ThrowIfCancellationRequested()
                                                                   queue.Enqueue(item.Switch)
                                                               End If

                                                               If item.NoSwitch <> "" AndAlso Not queue.Contains(item.NoSwitch) Then
                                                                   token.ThrowIfCancellationRequested()
                                                                   queue.Enqueue(item.NoSwitch)
                                                               End If

                                                               If item.HelpSwitch <> "" AndAlso Not queue.Contains(item.HelpSwitch) Then
                                                                   token.ThrowIfCancellationRequested()
                                                                   queue.Enqueue(item.HelpSwitch)
                                                               End If
                                                           End Sub)
                         Dim array = queue.Distinct.OrderBy(Function(x) x).ToArray()

                         UIBeginInvoke(Sub()
                                           If token.IsCancellationRequested OrElse IsDisposed Then
                                               Exit Sub
                                           End If

                                           Dim text = cbGoTo.Text
                                           cbGoTo.Items.Clear()
                                           cbGoTo.Items.AddRange(array)
                                           cbGoTo.Text = text
                                       End Sub)
                     End Sub, token)
        Catch ex As Exception
        End Try
    End Sub

    Shared Sub SetControlFont(control As Control, font As Font)
        Dim common = TryCast(control, CommonControl)

        If common IsNot Nothing Then
            common.Font = font
        End If
    End Sub

    Sub rtbCommandLine_MouseUp(sender As Object, e As MouseEventArgs) Handles rtbCommandLine.MouseUp
        If e.Buttons = MouseButtons.Primary AndAlso s.CommandLinePreviewMouseUpSearch Then
            Dim find = FindOptionInPreview()

            If find <> "" Then
                cbGoTo.Text = find
            End If
        End If
    End Sub

    Sub cmsCommandLine_Opening(sender As Object, e As EventArgs) Handles cmsCommandLine.Opening
        cmsCommandLine.Items.Clear()

        Dim copyItem = cmsCommandLine.Add("Copy Selection", Sub() Clipboard.SetText(rtbCommandLine.SelectedText))
        copyItem.ShortcutKeyDisplayString = "Ctrl+C"
        copyItem.Visible = rtbCommandLine.SelectionLength > 0

        cmsCommandLine.Add("Copy Command Line", Sub() Clipboard.SetText(Params.GetCommandLine(True, True)))

        Dim find = FindOptionInPreview()

        If find <> "" Then
            Dim a = Sub()
                        cbGoTo.Text = find
                        cbGoTo.Focus()
                    End Sub

            cmsCommandLine.Add("Search " + find, a)
        End If
    End Sub

    Function FindOptionInPreview() As String
        Dim find = rtbCommandLine.SelectedText

        If find.Length = 0 Then
            Dim pos = rtbCommandLine.SelectionStart
            Dim leftString = rtbCommandLine.Text.Substring(0, pos)
            Dim left = leftString.LastIndexOf(" ") + 1
            Dim right = rtbCommandLine.Text.Length
            Dim rightString = rtbCommandLine.Text.Substring(pos)
            Dim index = rightString.IndexOf(" ")

            If index > -1 Then
                right = pos + index
            End If

            If right - left > 0 Then
                find = rtbCommandLine.Text.Substring(left, right - left)
            End If
        End If

        If find.Length > 0 Then
            If find.Contains("=") Then
                find = find.Left("=")
            End If

            Return find
        End If
    End Function
End Class
