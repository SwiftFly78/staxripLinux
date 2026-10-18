
Imports StaxRip.UI

Public Class ProcessingForm
    Inherits FormBase

    Public WithEvents bnAbort As ButtonEx
    Public WithEvents bnJobs As ButtonEx
    Public WithEvents bnLog As ButtonEx
    Public WithEvents bnMenu As ButtonEx
    Public WithEvents mbShutdown As MenuButton
    Public laWhenfinisheddo As LabelEx
    Public pnLogHost As Panel
    Public pnStatusHost As Panel
    Public flpNav As StackLayout

    Private Shared ReadOnly _priorities() As KeyValuePair(Of ProcessPriorityClass, String) = {
        New KeyValuePair(Of ProcessPriorityClass, String)(ProcessPriorityClass.High, "High"),
        New KeyValuePair(Of ProcessPriorityClass, String)(ProcessPriorityClass.AboveNormal, "Above Normal"),
        New KeyValuePair(Of ProcessPriorityClass, String)(ProcessPriorityClass.Normal, "Normal"),
        New KeyValuePair(Of ProcessPriorityClass, String)(ProcessPriorityClass.BelowNormal, "Below Normal"),
        New KeyValuePair(Of ProcessPriorityClass, String)(ProcessPriorityClass.Idle, "Idle")
    }

    Private CMS As ContextMenuStripEx
    Private OutputHighlightingMenuItem As MenuItemEx
    Private ProgressReformattingMenuItem As MenuItemEx
    Private StopAfterCurrentJobMenuItem As MenuItemEx
    Private TitleBase As String = ""

    Private Const _priorityMenuName As String = "Priority"
    Private Const _themeMenuName As String = "Temporary Theme"
    Private Const _progressMenuName As String = "Progress Highlighting"

    Sub New()
        bnAbort = New ButtonEx("Abort (ESC)")
        bnJobs = New ButtonEx("Jobs (F6)")
        bnLog = New ButtonEx("Log (F8)")
        bnMenu = New ButtonEx With {.ShowMenuSymbol = True}
        mbShutdown = New MenuButton With {.ShowMenuSymbol = True}
        laWhenfinisheddo = New LabelEx With {.Text = "When finished do:"}
        pnLogHost = New Panel
        pnStatusHost = New Panel
        flpNav = New StackLayout With {.Orientation = Orientation.Horizontal, .Spacing = FH(0.3)}

        Content = PaddedTable(
            Row(New Scrollable With {.Content = flpNav, .Border = BorderType.None, .ExpandContentHeight = False}),
            ScaledRow(pnLogHost),
            Row(pnStatusHost),
            Row(ButtonRow(laWhenfinisheddo, mbShutdown, bnAbort, bnJobs, bnLog, bnMenu)))

        Text = $"Processing... - {g.DefaultCommands.GetApplicationDetails()}"
        mbShutdown.AddRange(System.Enum.GetValues(GetType(ShutdownMode)).Cast(Of Object))
        RestoreClientSize(42, 28)

        CMS = New ContextMenuStripEx
        CMS.Form = Me
        CMS.Add("Suspend", AddressOf ProcController.Suspend, "Suspends the current process, might not work with all tools.")
        CMS.Add("Resume", AddressOf ProcController.ResumeProcs, "Resumes a suspended process.")

        For Each priority In _priorities
            CMS.Add($"{_priorityMenuName} | {priority.Value}", Sub() ProcController.SetProcessPriority(priority.Key))
        Next

        CMS.Add("-")
        CMS.Add("Abort", AddressOf Abort, Keys.Escape, "Aborts all job processing of this StaxRip instance.")
        CMS.Add("Skip", AddressOf Skip, "Aborts the current job and continues with the next job.")
        StopAfterCurrentJobMenuItem = CMS.Add("Stop After Current Job", AddressOf StopAfterCurrentJob, "Stops all job processing after the current job.")
        CMS.Add("-")
        OutputHighlightingMenuItem = CMS.Add("Output Highlighting", AddressOf SetOutputHighlighting, Keys.Control Or Keys.O)
        ProgressReformattingMenuItem = CMS.Add("Progress Reformatting", AddressOf SetProgressReformatting, Keys.Control Or Keys.P)

        CMS.Add($"{_progressMenuName} | None", Sub() SetProgressHighlighting("None"))

        For Each category In ThemeManager.ColorCategories
            CMS.Add($"{_progressMenuName} | {category.Item1}", Sub() SetProgressHighlighting(category.Item1))
        Next

        'themes only apply with a dark GTK theme
        If IsDarkMode Then
            For Each theme In ThemeManager.Themes
                CMS.Add($"{_themeMenuName} | {theme.Name}", Sub() ThemeManager.SetCurrentTheme(theme.Name))
            Next
        End If

        CMS.Add("-")
        CMS.Add("Jobs", AddressOf JobsForm.ShowForm, Keys.F6, "Shows the Jobs dialog.")
        CMS.Add("Log", AddressOf g.DefaultCommands.ShowLogFile, Keys.F8, "Shows the log file.")
        CMS.Add("-")
        CMS.Add("Help", AddressOf ShowHelp).ShortcutKeyDisplayString = "F1"
    End Sub

    Sub AddProcControls(button As Button)
        flpNav.Items.Add(New StackLayoutItem(button))
    End Sub

    Sub RemoveProcControls(pc As ProcController)
        Dim item = flpNav.Items.FirstOrDefault(Function(i) i.Control Is pc.Button)

        If item IsNot Nothing Then
            flpNav.Items.Remove(item)
        End If

        If pnLogHost.Content Is pc.LogTextBox Then
            pnLogHost.Content = Nothing
        End If

        If pnStatusHost.Content Is pc.ProgressBar Then
            pnStatusHost.Content = Nothing
        End If
    End Sub

    Sub ShowProc(pc As ProcController)
        pnLogHost.Content = pc.LogTextBox
        pnStatusHost.Content = pc.ProgressBar
    End Sub

    Sub SetProgressTitle(progress As Double)
        If progress >= 0 Then
            Title = $"{progress:0.0}% - {TitleBase}"
        Else
            Title = TitleBase
        End If
    End Sub

    Protected Overrides Sub OnClosing(e As ComponentModel.CancelEventArgs)
        If g.IsJobProcessing OrElse ProcController.Procs.Count > 0 Then
            e.Cancel = True
            Abort()
            Exit Sub
        End If

        MyBase.OnClosing(e)
    End Sub

    Protected Overrides Sub OnWindowStateChanged(e As EventArgs)
        MyBase.OnWindowStateChanged(e)

        If s.MinimizeToTray AndAlso WindowState = WindowState.Minimized AndAlso g.ShowTrayIcon(Sub() ShowForm()) Then
            Visible = False
        End If
    End Sub

    Protected Overrides Sub OnActivated(e As EventArgs)
        MyBase.OnActivated(e)
        UpdateControls()
        ProcController.SetLastActivation()
    End Sub

    Sub StopAfterCurrentJob()
        g.StopAfterCurrentJob = Not g.StopAfterCurrentJob
    End Sub

    Sub SetOutputHighlighting()
        OutputHighlightingMenuItem.Marked = Not OutputHighlightingMenuItem.Marked
        ProcController.SetOutputHighlighting(OutputHighlightingMenuItem.Marked, ThemeManager.CurrentTheme)
    End Sub

    Sub SetProgressHighlighting(colorName As String)
        If String.IsNullOrWhiteSpace(colorName) Then
            s.ProgressHighlighting = False
        ElseIf colorName = "None" Then
            s.ProgressHighlighting = False
            s.ProgressHighlightingColorName = colorName
        Else
            s.ProgressHighlighting = True
            s.ProgressHighlightingColorName = colorName
        End If
    End Sub

    Sub SetProgressReformatting()
        s.ProgressReformatting = Not s.ProgressReformatting
    End Sub

    Sub Abort()
        If MsgOK("Abort processing?") Then
            Log.Save()
            ProcController.SetLastActivation()
            ProcController.Abort()
        End If
    End Sub

    Sub Skip()
        If MsgOK("Skip current process?") Then
            Log.Save()
            ProcController.SetLastActivation()
            ProcController.Skip()
        End If
    End Sub

    Sub ShowForm()
        g.HideTrayIcon()
        Show()

        If WindowState = WindowState.Minimized Then
            WindowState = WindowState.Normal
        End If

        Activate()
    End Sub

    Sub HideForm()
        g.HideTrayIcon()
        WindowState = WindowState.Normal
        Visible = False
    End Sub

    Sub UpdateControls()
        TitleBase = $"Processing: {JobManager.JobPath.Base()}"
        Title = TitleBase
        laWhenfinisheddo.Enabled = g.IsJobProcessing
        mbShutdown.Enabled = g.IsJobProcessing
        bnJobs.Enabled = g.IsJobProcessing
        OutputHighlightingMenuItem.Marked = s.OutputHighlighting
        mbShutdown.Value = g.ShutdownMode
    End Sub

    Sub cbShutdown_SelectedIndexChanged() Handles mbShutdown.ValueChangedUser
        g.ShutdownMode = CType(mbShutdown.Value, ShutdownMode)
    End Sub

    Sub bnAbort_Click(sender As Object, e As EventArgs) Handles bnAbort.Click
        Abort()
    End Sub

    Sub bnJobs_Click(sender As Object, e As EventArgs) Handles bnJobs.Click
        JobsForm.ShowForm()
    End Sub

    Sub bnLog_Click(sender As Object, e As EventArgs) Handles bnLog.Click
        g.DefaultCommands.ShowLogFile()
    End Sub

    Sub bnMenu_Click(sender As Object, e As EventArgs) Handles bnMenu.Click
        StopAfterCurrentJobMenuItem.Enabled = g.IsJobProcessing
        StopAfterCurrentJobMenuItem.Marked = g.StopAfterCurrentJob

        OutputHighlightingMenuItem.Marked = s.OutputHighlighting
        ProgressReformattingMenuItem.Marked = s.ProgressReformatting

        Dim priority = ProcController.GetProcessPriority()
        Dim priorityText = If(priority.HasValue, _priorities.FirstOrDefault(Function(x) x.Key = priority.Value).Value, "")

        For Each item In CMS.GetItems().OfType(Of MenuItemEx).Where(Function(i) i.Path IsNot Nothing AndAlso i.Path.StartsWith(_priorityMenuName + " | "))
            item.Enabled = priority.HasValue
            item.Marked = item.Path.EndsWith($" | {priorityText}")
        Next

        For Each item In CMS.GetItems().OfType(Of MenuItemEx).Where(Function(i) i.Path IsNot Nothing AndAlso i.Path.StartsWith(_progressMenuName + " | "))
            item.Marked = item.Path.EndsWith($" | {s.ProgressHighlightingColorName}")
        Next

        For Each item In CMS.GetItems().OfType(Of MenuItemEx).Where(Function(i) i.Path IsNot Nothing AndAlso i.Path.StartsWith(_themeMenuName + " | "))
            item.Marked = item.Path.EndsWith($" | {ThemeManager.CurrentTheme.Name}")
        Next

        CMS.Show(bnMenu, 0, 0)
    End Sub

    Sub ProcessingForm_HelpRequested(sender As Object, e As EventArgs) Handles Me.HelpRequested
        ShowHelp()
    End Sub

    Sub ShowHelp()
        Dim form As New HelpForm()
        form.Doc.WriteStart(Text)
        form.Doc.WriteTips(CMS.GetTips)
        form.Show()
    End Sub
End Class
