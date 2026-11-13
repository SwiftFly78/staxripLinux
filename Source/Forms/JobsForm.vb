
Imports System.Threading
Imports System.Threading.Tasks

Imports StaxRip.UI

Friend Class JobsForm
    Inherits DialogBase

    Public WithEvents bnStart As ButtonEx
    Public WithEvents bnDown As ButtonEx
    Public WithEvents bnUp As ButtonEx
    Public WithEvents bnLoad As ButtonEx
    Public WithEvents stb As SearchTextBox
    Public WithEvents lv As ListViewEx
    Public WithEvents bnMenu As ButtonEx
    Public WithEvents bnRemove As ButtonEx

    Sub InitializeComponent()
        bnStart = New ButtonEx("Start")
        bnStart.AddStyleClass("suggested-action")
        bnUp = New ButtonEx With {.Symbol = Symbol.Up, .Enabled = False}
        bnDown = New ButtonEx With {.Symbol = Symbol.Down, .Enabled = False}
        bnMenu = New ButtonEx With {.Text = "Edit", .ShowMenuSymbol = True}
        bnRemove = New ButtonEx("Remove")
        bnLoad = New ButtonEx("Load")
        stb = New SearchTextBox
        stb.SendMessageCue("Search jobs")
        lv = New ListViewEx

        Content = DialogContent(SplitRow({bnStart, bnUp, bnDown}, {bnMenu, bnRemove, bnLoad}), Row(stb), ScaledRow(lv))

        Text = $"Jobs - {g.DefaultCommands.GetApplicationDetails()}"
        SetMinimumSize(20, 12)
    End Sub

    Private FileWatcher As New FileSystemWatcher
    Private IsLoading As Boolean
    Private BlockSave As Boolean
    Private AllJobs As List(Of Job)
    Private CommandManager As New CommandManager
    Private CustomMenu As CustomMenu
    Private MenuEnabledFuncs As Dictionary(Of String, Func(Of Boolean))

    Private ReadOnly Property FilteredJobs As List(Of Job)
        Get
            Dim lowerText = stb.Text.ToLowerEx()

            If String.IsNullOrEmpty(lowerText) Then
                Return AllJobs.ToList()
            ElseIf lowerText = "<active>" Then
                Return AllJobs.Where(Function(x) x.Active).ToList()
            Else
                Return AllJobs.Where(Function(x) x.Name.ToLower().Contains(lowerText)).ToList()
            End If
        End Get
    End Property

    Sub New()
        MyClass.New(ThemeManager.CurrentTheme)
    End Sub

    Sub New(theme As Theme)
        InitializeComponent()
        RestoreClientSize(55, 35)

        SaveAndLoadSize = True
        AllJobs = JobManager.GetJobs()

        lv.UpButton = bnUp
        lv.DownButton = bnDown
        lv.RemoveButton = bnRemove
        lv.RightClickOnlyForMenu = True

        lv.SingleSelectionButtons = {bnLoad}
        lv.CheckBoxes = True
        lv.EnableListBoxMode()
        lv.ItemCheckProperty = NameOf(Job.Active)
        lv.ItemText = Function(item) GetDisplayName(DirectCast(item, Job))
        lv.AddItems(AllJobs)
        lv.SelectFirst()

        SetTitle()

        Dim cms As New ContextMenuStripEx()
        bnMenu.ContextMenuStrip = cms
        lv.ContextMenuStrip = cms

        MenuEnabledFuncs = New Dictionary(Of String, Func(Of Boolean)) From {
            {NameOf(SelectAll), Function() lv.Items.Count > lv.SelectedItems.Count},
            {NameOf(SelectNone), Function() lv.SelectedItems.Count > 0},
            {NameOf(CheckSelection), Function() lv.SelectedItems.Any(Function(item) Not item.Checked)},
            {NameOf(CheckAll), Function() lv.Items.Count > lv.CheckedItems.Count},
            {NameOf(UncheckSelection), Function() lv.SelectedItems.Any(Function(item) item.Checked)},
            {NameOf(UncheckAll), Function() lv.CheckedItems.Count > 0},
            {NameOf(MoveSelectionUp), Function() lv.CanMoveUp},
            {NameOf(MoveSelectionDown), Function() lv.CanMoveDown},
            {NameOf(MoveSelectionToTop), Function() lv.CanMoveUp},
            {NameOf(MoveSelectionToBottom), Function() lv.CanMoveDown},
            {NameOf(SortAlphabetically), Function() lv.Items.Count > 1},
            {NameOf(RemoveSelection), Function() lv.SelectedItems.Count > 0},
            {NameOf(LoadSelection), Function() lv.SelectedItems.Count = 1}}

        CommandManager.AddCommandsFromObject(Me)
        CustomMenu = New CustomMenu(AddressOf GetDefaultMenu, s.CustomMenuJobs, CommandManager, cms.Items) With {
            .Form = Me,
            .HandleShortcutsManually = True}
        CustomMenu.BuildMenu()

        AddHandler cms.Opening, Sub() UpdateMenu()
        AddHandler lv.KeyDown, AddressOf OnMenuKeyDown
        AddHandler KeyDown, AddressOf OnMenuKeyDown

        AddHandler lv.ItemRemoved, Sub(item)
                                       Dim job = DirectCast(item.Tag, Job)
                                       Dim fp = job.Path

                                       AllJobs.Remove(job)
                                       SaveJobs()

                                       If fp.StartsWith(Path.Combine(Folder.Settings, "Batch Projects") + Path.DirectorySeparatorChar) Then
                                           FileHelp.Delete(fp)
                                       End If
                                   End Sub

    End Sub

    Shared Function GetDefaultMenu() As CustomMenuItem
        Dim ret As New CustomMenuItem("Root")
        ret.Add("Select All", NameOf(SelectAll), Keys.Control Or Keys.S)
        ret.Add("Select None", NameOf(SelectNone), Keys.Shift Or Keys.S)
        ret.Add("-")
        ret.Add("Check Selection", NameOf(CheckSelection), Keys.Shift Or Keys.C)
        ret.Add("Check All", NameOf(CheckAll), Keys.Control Or Keys.C)
        ret.Add("-")
        ret.Add("Uncheck Selection", NameOf(UncheckSelection), Keys.Shift Or Keys.U)
        ret.Add("Uncheck All", NameOf(UncheckAll), Keys.Control Or Keys.U)
        ret.Add("-")
        ret.Add("Move Selection Up", NameOf(MoveSelectionUp), Keys.Control Or Keys.Up)
        ret.Add("Move Selection Down", NameOf(MoveSelectionDown), Keys.Control Or Keys.Down)
        ret.Add("-")
        ret.Add("Move Selection To Top", NameOf(MoveSelectionToTop), Keys.Control Or Keys.Home)
        ret.Add("Move Selection To Bottom", NameOf(MoveSelectionToBottom), Keys.Control Or Keys.End)
        ret.Add("-")
        ret.Add("Sort Alphabetically", NameOf(SortAlphabetically), Keys.Control Or Keys.Shift Or Keys.S)
        ret.Add("Remove Selection", NameOf(RemoveSelection), Keys.Delete)
        ret.Add("Load Selection", NameOf(LoadSelection), Keys.Control Or Keys.L)
        ret.Add("-")
        ret.Add("Edit Menu...", NameOf(ShowMenuEditor), Keys.Control Or Keys.M)
        Return ret
    End Function

    'the list gets keys like Ctrl+Down before the form, the form handles the keys of the other controls
    'except the search field, which gets every key
    Sub OnMenuKeyDown(sender As Object, e As KeyEventArgs)
        If e.Handled OrElse stb.HasFocus OrElse (sender Is Me AndAlso lv.HasFocus) Then Exit Sub
        UpdateMenu()
        CustomMenu.OnKeyDown(sender, e)
    End Sub

    Sub UpdateMenu()
        For Each pair In MenuEnabledFuncs
            CustomMenu.EnableMenuItemByActionName(pair.Key, pair.Value.Invoke)
        Next
    End Sub

    <Command("Moves the selected jobs up.")>
    Sub MoveSelectionUp()
        bnUp.PerformClick()
    End Sub

    <Command("Moves the selected jobs down.")>
    Sub MoveSelectionDown()
        bnDown.PerformClick()
    End Sub

    <Command("Moves the selected jobs to the top.")>
    Sub MoveSelectionToTop()
        lv.MoveSelectionTop()
    End Sub

    <Command("Moves the selected jobs to the bottom.")>
    Sub MoveSelectionToBottom()
        lv.MoveSelectionBottom()
    End Sub

    <Command("Sorts the jobs alphabetically.")>
    Sub SortAlphabetically()
        lv.SortItems()
    End Sub

    <Command("Removes the selected jobs.")>
    Sub RemoveSelection()
        bnRemove.PerformClick()
    End Sub

    <Command("Opens the project of the selected job.")>
    Sub LoadSelection()
        bnLoad.PerformClick()
    End Sub

    <Command("Dialog to configure the menu.")>
    Sub ShowMenuEditor()
        s.CustomMenuJobs = CustomMenu.Edit()
        g.SaveSettings()
    End Sub

    'jobs are named by the path of their project file, the list shows its name,
    'with the folder when several jobs have the same name
    Function GetDisplayName(job As Job) As String
        Dim name = If(job.Name.Contains(Path.DirectorySeparatorChar), job.Name.Base, job.Name)

        If AllJobs.Where(Function(x) x.Name.Base = job.Name.Base).Count() > 1 AndAlso job.Name.Contains(Path.DirectorySeparatorChar) Then
            name += $"  ({job.Name.Dir.TrimEnd(Path.DirectorySeparatorChar).FileName})"
        End If

        Return name
    End Function

    <Command("Unchecks all jobs.")>
    Sub UncheckAll()
        BlockSave = True

        For Each item As ListViewItem In lv.Items
            item.Checked = False
        Next

        BlockSave = False
        HandleItemsChanged()
    End Sub

    <Command("Unchecks the selected jobs.")>
    Sub UncheckSelection()
        BlockSave = True

        For Each item As ListViewItem In lv.SelectedItems
            item.Checked = False
        Next

        BlockSave = False
        HandleItemsChanged()
    End Sub

    <Command("Checks all jobs.")>
    Sub CheckAll()
        BlockSave = True

        For Each item As ListViewItem In lv.Items
            item.Checked = True
        Next

        BlockSave = False
        HandleItemsChanged()
    End Sub

    <Command("Checks the selected jobs.")>
    Sub CheckSelection()
        BlockSave = True

        For Each item As ListViewItem In lv.SelectedItems
            item.Checked = True
        Next

        BlockSave = False
        HandleItemsChanged()
    End Sub

    <Command("Selects no job.")>
    Sub SelectNone()
        BlockSave = True

        For Each item As ListViewItem In lv.Items
            item.Selected = False
        Next

        HandleItemsChanged()
        BlockSave = False
    End Sub

    <Command("Selects all jobs.")>
    Sub SelectAll()
        BlockSave = True

        For Each item As ListViewItem In lv.Items
            item.Selected = True
        Next

        HandleItemsChanged()
        BlockSave = False
    End Sub

    Sub HandleItemsChanged(ParamArray indices As Integer())
        If stb.Text = "" AndAlso AllJobs.Count = lv.Items.Count Then
            If indices?.Any() Then
                Dim orderedIndices = indices.OrderByDescending(Function(x) x)

                For Each index As Integer In orderedIndices
                    AllJobs.RemoveAt(index)
                    AllJobs.Insert(index, DirectCast(lv.Items(index).Tag, Job))
                Next
            Else
                AllJobs.Clear()
                AllJobs.AddRange(lv.Items.OfType(Of ListViewItem).Select(Function(x) DirectCast(x.Tag, Job)))
            End If
        End If

        'a filtered list shows the same jobs, so its checked states are saved too
        If Not BlockSave Then SaveJobs()

        UpdateControls()
    End Sub

    Sub Reload(sender As Object, e As FileSystemEventArgs)
        AllJobs = JobManager.GetJobs()
        Invoke(Sub()
                   If Not IsDisposed Then
                       IsLoading = True
                       lv.BeginUpdate()
                       lv.Items.Clear()
                       lv.AddItems(FilteredJobs)
                       lv.EndUpdate()
                       lv.SelectFirst()
                       UpdateControls()
                       IsLoading = False
                   End If
               End Sub)
    End Sub

    Sub UpdateControls()
        bnStart.Enabled = AllJobs.Any(Function(x) x.Active)
        SetTitle()
    End Sub

    Sub SetTitle()
        Dim all = AllJobs.Count
        Dim filtered = FilteredJobs.Count

        Me.Text = $"Jobs ( {filtered} / {all} ) - {g.DefaultCommands.GetApplicationDetails()}"
    End Sub

    Sub SaveJobs()
        If IsLoading Then Exit Sub

        FileWatcher.EnableRaisingEvents = False
        JobManager.SaveJobs(AllJobs)
        FileWatcher.EnableRaisingEvents = True
    End Sub

    Sub bnStart_Click(sender As Object, e As EventArgs) Handles bnStart.Click
        If Not g.VerifyRequirements Then Exit Sub

        Close()

        If g.IsJobProcessing Then
            g.ShellExecute(AppInfo.ExecutablePath, "-StartJobs -NoFocus")
        Else
            Task.Run(Sub()
                         Thread.Sleep(500)
                         UIInvoke(Sub() g.ProcessJobs())
                     End Sub)
        End If
    End Sub

    Sub bnLoad_Click(sender As Object, e As EventArgs) Handles bnLoad.Click
        Dim job = DirectCast(lv.SelectedItem, Job)

        If g.MainForm.LoadProject(job.Path) Then
            Close()
        End If
    End Sub

    Sub SearchTextBox_TextChanged() Handles stb.TextChanged
        BlockSave = True
        lv.BeginUpdate()
        lv.Items.Clear()
        lv.AddItems(FilteredJobs)
        lv.EndUpdate()
        BlockSave = False
        SetTitle()
    End Sub

    Protected Overrides Sub OnGotFocus(e As EventArgs)
        MyBase.OnGotFocus(e)
        UpdateControls()
        If Not ProcController.BlockActivation Then
            ProcController.SetLastActivation()
            ProcController.BlockActivation = False
        End If
    End Sub

    Protected Overrides Sub OnClosing(args As ComponentModel.CancelEventArgs)
        MyBase.OnClosing(args)
        RemoveHandler FileWatcher.Changed, AddressOf Reload
        RemoveHandler FileWatcher.Created, AddressOf Reload
        FileWatcher.Dispose()
        RemoveHandler lv.ItemsChanged, AddressOf HandleItemsChanged
    End Sub

    Protected Overrides Sub OnLoad(args As EventArgs)
        MyBase.OnLoad(args)

        FileWatcher.Path = Folder.Settings
        FileWatcher.NotifyFilter = NotifyFilters.LastWrite Or NotifyFilters.CreationTime
        FileWatcher.Filter = "Jobs.dat"
        FileWatcher.EnableRaisingEvents = True

        AddHandler FileWatcher.Changed, AddressOf Reload
        AddHandler FileWatcher.Created, AddressOf Reload
        AddHandler lv.ItemsChanged, AddressOf HandleItemsChanged

        UpdateControls()
    End Sub

    Shared Sub ShowForm()
        Using form As New JobsForm()
            form.ShowDialog()
        End Using
    End Sub
End Class
