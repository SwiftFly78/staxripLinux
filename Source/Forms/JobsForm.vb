
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
        bnUp = New ButtonEx With {.Symbol = Symbol.Up, .Enabled = False}
        bnDown = New ButtonEx With {.Symbol = Symbol.Down, .Enabled = False}
        bnMenu = New ButtonEx With {.ShowMenuSymbol = True}
        bnRemove = New ButtonEx("Remove")
        bnLoad = New ButtonEx("Load")
        stb = New SearchTextBox
        lv = New ListViewEx

        Content = PaddedTable(
            Row(stb),
            ScaledRow(lv),
            Row(SplitRow({bnStart, bnUp, bnDown}, {bnMenu, bnRemove, bnLoad})))

        Text = $"Jobs - {g.DefaultCommands.GetApplicationDetails()}"
        SetMinimumSize(20, 12)
    End Sub

    Private FileWatcher As New FileSystemWatcher
    Private IsLoading As Boolean
    Private BlockSave As Boolean
    Private AllJobs As List(Of Job)

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
        lv.AddItems(AllJobs)
        lv.SelectFirst()

        SetTitle()

        Dim cms As New ContextMenuStripEx()
        cms.Form = Me
        bnMenu.ContextMenuStrip = cms
        lv.ContextMenuStrip = cms

        AddHandler lv.ItemRemoved, Sub(item)
                                       Dim job = DirectCast(item.Tag, Job)
                                       Dim fp = job.Path

                                       AllJobs.Remove(job)
                                       SaveJobs()

                                       If fp.StartsWith(Path.Combine(Folder.Settings, "Batch Projects") + Path.DirectorySeparatorChar) Then
                                           FileHelp.Delete(fp)
                                       End If
                                   End Sub

        cms.Add("Select All", Sub() SelectAll(), Keys.Control Or Keys.S, Function() lv.Items.Count > lv.SelectedItems.Count)
        cms.Add("Select None", Sub() SelectNone(), Keys.Shift Or Keys.S, Function() lv.SelectedItems.Count > 0)
        cms.Add("-")
        cms.Add("Check Selection", Sub() CheckSelection(), Keys.Shift Or Keys.C, Function() lv.SelectedItems.OfType(Of ListViewItem).Where(Function(item) Not item.Checked).Count > 0).ShortcutKeyDisplayString = "Space"
        cms.Add("Check All", Sub() CheckAll(), Keys.Control Or Keys.C, Function() lv.Items.Count > lv.CheckedItems.Count)
        cms.Add("-")
        cms.Add("Uncheck Selection", Sub() UncheckSelection(), Keys.Shift Or Keys.U, Function() lv.SelectedItems.OfType(Of ListViewItem).Where(Function(item) item.Checked).Count > 0).ShortcutKeyDisplayString = "Space"
        cms.Add("Uncheck All", Sub() UncheckAll(), Keys.Control Or Keys.U, Function() lv.CheckedItems.Count > 0)
        cms.Add("-")
        cms.Add("Move Selection Up", Sub() bnUp.PerformClick(), Keys.Control Or Keys.Up, Function() lv.CanMoveUp)
        cms.Add("Move Selection Down", Sub() bnDown.PerformClick(), Keys.Control Or Keys.Down, Function() lv.CanMoveDown)
        cms.Add("-")
        cms.Add("Move Selection To Top", Sub() lv.MoveSelectionTop(), Keys.Control Or Keys.Home, Function() lv.CanMoveUp)
        cms.Add("Move Selection To Bottom", Sub() lv.MoveSelectionBottom(), Keys.Control Or Keys.End, Function() lv.CanMoveDown)
        cms.Add("-")
        cms.Add("Sort Alphabetically", Sub() lv.SortItems(), Keys.Control Or Keys.Shift Or Keys.S, Function() lv.Items.Count > 1)
        cms.Add("Remove Selection", Sub() bnRemove.PerformClick(), Keys.Delete, Function() lv.SelectedItems.Count > 0)
        cms.Add("Load Selection", Sub() bnLoad.PerformClick(), Keys.Control Or Keys.L, Function() lv.SelectedItems.Count = 1)

    End Sub

    Sub UncheckAll()
        BlockSave = True

        For Each item As ListViewItem In lv.Items
            item.Checked = False
        Next

        BlockSave = False
        HandleItemsChanged()
    End Sub

    Sub UncheckSelection()
        BlockSave = True

        For Each item As ListViewItem In lv.SelectedItems
            item.Checked = False
        Next

        BlockSave = False
        HandleItemsChanged()
    End Sub

    Sub CheckAll()
        BlockSave = True

        For Each item As ListViewItem In lv.Items
            item.Checked = True
        Next

        BlockSave = False
        HandleItemsChanged()
    End Sub

    Sub CheckSelection()
        BlockSave = True

        For Each item As ListViewItem In lv.SelectedItems
            item.Checked = True
        Next

        BlockSave = False
        HandleItemsChanged()
    End Sub

    Sub SelectNone()
        BlockSave = True

        For Each item As ListViewItem In lv.Items
            item.Selected = False
        Next

        HandleItemsChanged()
        BlockSave = False
    End Sub

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

            If Not BlockSave Then SaveJobs()
        End If

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
