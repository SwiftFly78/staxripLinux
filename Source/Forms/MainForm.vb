
Imports System.ComponentModel
Imports System.Globalization
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.VisualBasic
Imports StaxRip
Imports StaxRip.UI
Imports VB6 = Microsoft.VisualBasic

Partial Public Class MainForm
    Inherits FormBase

#Region " Designer "
    Public bnOpen As SplitButton
    Public WithEvents bnPreview As ButtonEx
    Public WithEvents bnCrop As ButtonEx
    Public WithEvents bnJobs As ButtonEx
    Public bnAddJob As SplitButton
    Public NextStep As NextStepBar
    Public WithEvents tbSourceFile As TextEdit
    Public WithEvents tbTargetFile As TextEdit
    Public WithEvents tbTargetSize As TextEdit
    Public WithEvents laBitrate As LabelEx
    Public WithEvents tbBitrate As TextEdit
    Public WithEvents lTarget1 As LabelEx
    Public WithEvents lSource1 As LabelEx
    Public WithEvents lPixel As LabelEx
    Public WithEvents lPixelText As LabelEx
    Public WithEvents tbResize As TrackBarEx
    Public WithEvents lZoom As LabelEx
    Public WithEvents lZoomText As LabelEx
    Public WithEvents tbTargetHeight As TextEdit
    Public WithEvents tbTargetWidth As TextEdit
    Public WithEvents lTargetHeight As LabelEx
    Public WithEvents lTargetWidth As LabelEx
    Public WithEvents lDAR As LabelEx
    Public WithEvents blTargetDarText As ButtonEx
    Public WithEvents lCrop As LabelEx
    Public WithEvents lCropText As LabelEx
    Public WithEvents lSource2 As LabelEx
    Public WithEvents laTarget2 As LabelEx
    Public WithEvents MenuStrip As MenuBar
    Public WithEvents pnEncoder As Panel
    Public WithEvents FiltersListView As FiltersListView
    Public WithEvents blFilesize As ButtonEx
    Public WithEvents bnMuxer As ButtonEx
    Public WithEvents bnMuxerOptions As ButtonEx
    Public WithEvents bnEncoder As ButtonEx
    Public WithEvents bnEncoderOptions As ButtonEx
    Public WithEvents bnFilters As ButtonEx
    Public WithEvents bnResize As ButtonEx
    Public WithEvents bnSourceMenu As ButtonEx
    Public WithEvents bnTargetMenu As ButtonEx
    Public WithEvents lPAR As LabelEx
    Public WithEvents blTargetParText As ButtonEx
    Public WithEvents lAspectRatioError As LabelEx
    Public WithEvents lAspectRatioErrorText As LabelEx
    Public WithEvents blCropText As ButtonEx
    Public WithEvents lCrop2 As LabelEx
    Public WithEvents blSourceParText As ButtonEx
    Public WithEvents lSourcePAR As LabelEx
    Public WithEvents lSourceDar As LabelEx
    Public WithEvents lSAR As LabelEx
    Public WithEvents lSarText As LabelEx
    Public WithEvents blSourceDarText As ButtonEx
    Public SourceCropRow As StackLayout
    Public TargetSizeRow As TableLayout
    Public cardSource As Card
    Public cardTarget As Card
    Public cardFilters As Card
    Public cardResize As Card
    Public cardEncoder As Card
    Public cardAudio As Card
    Public WithEvents bnAddAudio As ButtonEx
    Public laNoAudio As LabelEx
    Public pnAudio As Panel
    Public TipProvider As TipProvider

    Private Sub InitializeComponent()
        Dim label = Function(text As String) New LabelEx With {.Text = text}

        Dim dimLabel = Function(text As String)
                           Dim ret As New LabelEx With {.Text = text}
                           ret.AddStyleClass("dim-label")
                           Return ret
                       End Function

        Dim flatButton = Function(text As String, showMenu As Boolean)
                             Dim ret As New ButtonEx With {.Text = text, .ShowMenuSymbol = showMenu, .MinimumSize = Size.Empty}
                             ret.AddStyleClass("flat")
                             Return ret
                         End Function

        Dim toolButton = Function(text As String, iconName As String)
                             Dim ret As New ButtonEx With {.Text = text, .IconName = iconName, .MinimumSize = Size.Empty}
                             ret.AddStyleClass("flat")
                             Return ret
                         End Function

        Dim menuIcon = Styling.GetIconName("view-more-symbolic", "open-menu-symbolic")

        bnOpen = New SplitButton("Open", Styling.GetIconName("document-open-symbolic"))
        bnPreview = toolButton("Preview", Styling.GetIconName("view-preview-symbolic", "video-display-symbolic"))
        bnCrop = toolButton("Crop", Styling.GetIconName("transform-crop-symbolic", "image-crop-symbolic", "transform-crop"))
        bnJobs = toolButton("Jobs", Styling.GetIconName("view-media-playlist-symbolic", "view-list-symbolic"))
        bnAddJob = New SplitButton("Add to Jobs", Styling.GetIconName("list-add-symbolic"), True)
        NextStep = New NextStepBar
        pnAudio = New Panel
        laNoAudio = dimLabel("No audio tracks, Add Track adds a stream of the source or a file.")
        bnAddAudio = New ButtonEx With {.Text = "Add Track", .IconName = Styling.GetIconName("list-add-symbolic"), .MinimumSize = Size.Empty}
        MenuStrip = New MenuBar With {.IncludeSystemItems = MenuBarSystemItems.None}

        tbSourceFile = New TextEdit
        lSource1 = New LabelEx With {.Wrap = WrapMode.Word}
        lSource2 = New LabelEx With {.Wrap = WrapMode.Word}
        lCropText = dimLabel("Crop")
        lCrop = label("disabled")
        blSourceParText = flatButton("PAR", True)
        lSourcePAR = label("-")
        blSourceDarText = flatButton("DAR", True)
        lSourceDar = label("-")
        bnSourceMenu = New ButtonEx With {.IconName = menuIcon, .MinimumSize = Size.Empty}
        bnSourceMenu.AddStyleClass("flat")

        tbTargetFile = New TextEdit
        blFilesize = flatButton("Size", True)
        tbTargetSize = New TextEdit With {.TextAlign = HorizontalAlignment.Center}
        laBitrate = dimLabel("Video Bitrate")
        tbBitrate = New TextEdit With {.TextAlign = HorizontalAlignment.Center}
        lTarget1 = New LabelEx With {.Wrap = WrapMode.Word}
        laTarget2 = New LabelEx With {.Wrap = WrapMode.Word}
        bnMuxer = New ButtonEx With {.ShowMenuSymbol = True, .MinimumSize = Size.Empty}
        bnMuxerOptions = New ButtonEx With {.Text = "Options...", .MinimumSize = Size.Empty}
        bnTargetMenu = New ButtonEx With {.IconName = menuIcon, .MinimumSize = Size.Empty}
        bnTargetMenu.AddStyleClass("flat")

        lTargetWidth = dimLabel("Width")
        tbTargetWidth = New TextEdit With {.TextAlign = HorizontalAlignment.Center, .Width = FH(4.5)}
        lTargetHeight = dimLabel("Height")
        tbTargetHeight = New TextEdit With {.TextAlign = HorizontalAlignment.Center, .Width = FH(4.5)}
        tbResize = New TrackBarEx With {.Minimum = 0, .TickFrequency = TrackBarTicks}
        blCropText = flatButton("Crop...", False)
        blCropText.ClickAction = AddressOf ShowCropDialog
        lCrop2 = label("-")
        blTargetDarText = flatButton("DAR", True)
        lDAR = label("-")
        lPixelText = dimLabel("Pixel")
        lPixel = label("-")
        lSarText = dimLabel("SAR")
        lSAR = label("-")
        lZoomText = dimLabel("Zoom")
        lZoom = label("-")
        blTargetParText = flatButton("PAR", True)
        lPAR = label("-")
        lAspectRatioErrorText = dimLabel("Error")
        lAspectRatioError = label("-")
        bnResize = New ButtonEx With {.Text = "Presets", .ShowMenuSymbol = True, .MinimumSize = Size.Empty}

        FiltersListView = New FiltersListView With {.AutoCheckMode = AutoCheckMode.None, .Border = BorderType.None}
        bnFilters = New ButtonEx With {.ShowMenuSymbol = True, .MinimumSize = Size.Empty}

        pnEncoder = New Panel
        bnEncoder = New ButtonEx With {.ShowMenuSymbol = True, .MinimumSize = Size.Empty}
        bnEncoderOptions = New ButtonEx With {.Text = "Options...", .MinimumSize = Size.Empty}

        TipProvider = New TipProvider

        Dim rowSpacing = New Size(FH(0.5), FH(0.35))

        SourceCropRow = HRow(lCropText, lCrop, blSourceParText, lSourcePAR, blSourceDarText, lSourceDar)
        cardSource = New Card("Source") With {.HeaderRight = bnSourceMenu}
        cardSource.Content = New TableLayout(Row(tbSourceFile), Row(lSource1), Row(lSource2), Row(SourceCropRow), New TableRow With {.ScaleHeight = True}) With {.Spacing = rowSpacing}

        TargetSizeRow = New TableLayout(Row(blFilesize, Scaled(tbTargetSize), laBitrate, Scaled(tbBitrate))) With {.Spacing = rowSpacing}
        cardTarget = New Card("Target") With {.HeaderRight = HRow(bnMuxer, bnMuxerOptions, bnTargetMenu)}
        cardTarget.Content = New TableLayout(Row(tbTargetFile), Row(TargetSizeRow), Row(lTarget1), Row(laTarget2), New TableRow With {.ScaleHeight = True}) With {.Spacing = rowSpacing}

        Dim resizeSize As New TableLayout(Row(lTargetWidth, tbTargetWidth, Nothing, lTargetHeight, tbTargetHeight)) With {.Spacing = rowSpacing}
        Dim resizeValues As New TableLayout(
            Row(blCropText, Scaled(lCrop2), lPixelText, Scaled(lPixel)),
            Row(blTargetDarText, Scaled(lDAR), lZoomText, Scaled(lZoom)),
            Row(lSarText, Scaled(lSAR), lAspectRatioErrorText, Scaled(lAspectRatioError)),
            Row(blTargetParText, Scaled(lPAR), Nothing, Nothing)) With {.Spacing = New Size(FH(0.5), FH(0.15))}

        cardResize = New Card("Crop and Resize") With {.HeaderRight = bnResize, .Width = FH(19)}
        cardResize.Content = New TableLayout(Row(resizeSize), Row(tbResize), Row(resizeValues), New TableRow With {.ScaleHeight = True}) With {.Spacing = rowSpacing}

        cardFilters = New Card("Filters") With {.HeaderRight = bnFilters, .Content = FiltersListView}
        cardEncoder = New Card("Video") With {.HeaderRight = HRow(bnEncoder, bnEncoderOptions), .Content = pnEncoder}
        cardAudio = New Card("Audio") With {.HeaderRight = bnAddAudio, .Content = pnAudio}

        'the target card is as wide as the video card, so the cards form columns
        AddHandler cardEncoder.SizeChanged, Sub()
                                                If cardEncoder.Width > 0 AndAlso cardTarget.Width <> cardEncoder.Width Then
                                                    cardTarget.Width = cardEncoder.Width
                                                End If
                                            End Sub

        Dim toolbar As New TableLayout(Row(bnOpen, bnPreview, bnCrop, bnJobs, Scaled(Nothing), bnAddJob)) With {
            .Spacing = New Size(FH(0.35), 0), .Padding = New Padding(FH(0.5), FH(0.35))}

        Dim toolbarPanel As New Panel With {.Content = toolbar}
        toolbarPanel.AddStyleClass("staxrip-toolbar")

        Dim gap = New Size(FH(0.7), FH(0.7))

        Dim cards As New TableLayout(
            Row(New TableLayout(Row(Scaled(cardSource), cardTarget)) With {.Spacing = gap}),
            ScaledRow(New TableLayout(ScaledRow(Scaled(cardFilters), cardResize, Scaled(cardEncoder))) With {.Spacing = gap}),
            Row(cardAudio)) With {.Spacing = gap, .Padding = New Padding(FH(0.7))}

        Menu = MenuStrip
        Content = New TableLayout(Row(toolbarPanel), ScaledRow(cards), Row(NextStep))

        Text = g.DefaultCommands.GetApplicationDetails()
        FileDrop = True
        Maximizable = True
    End Sub
#End Region

    Public WithEvents CustomMainMenu As CustomMenu
    Public WithEvents CustomSizeMenu As CustomMenu
    Public CurrentAssistantTipKey As String
    Public BlockSubtitlesItemCheck As Boolean
    Public AssistantPassed As Boolean
    Public CommandManager As New CommandManager

    Public Property LastTbTargetFileText As String = ""
    Public Property PreviewScript As VideoScript

    Private TargetAspectRatioMenu As ContextMenuStripEx
    Private SizeContextMenuStrip As ContextMenuStripEx
    Private EncoderMenu As ContextMenuStripEx
    Private ContainerMenu As ContextMenuStripEx
    Private SourceAspectRatioMenu As ContextMenuStripEx
    Private TargetFileMenu As ContextMenuStripEx
    Private SourceFileMenu As ContextMenuStripEx
    Private NextContextMenuStrip As ContextMenuStripEx
    Private CanIgnoreTip As Boolean = True
    Private IsLoading As Boolean = True
    Private BlockBitrate, BlockSize As Boolean
    Private SkipAssistant As Boolean
    Private BlockSourceTextBoxTextChanged As Boolean
    Private AssistantClickAction As Action
    Private ThemeRefresh As Boolean
    Private Const TrackBarInterval As Integer = 16
    Private Const TrackBarTicks As Integer = 4

    Sub New(loadSettings As Boolean)
        g.MainForm = Me
        If loadSettings Then g.LoadSettings()
        g.LoadAudioProfiles()
        g.LoadVideoEncoderProfiles()
        g.LoadEvents()

        PowerShell.InitCode =
            "Using namespace StaxRip;" + BR +
            "Using namespace StaxRip.UI;" + BR +
            "[Reflection.Assembly]::LoadWithPartialName(""StaxRip"")"

        If s.WriteDebugLog Then
            Dim filePath = Path.Combine(Folder.Settings, "Debug.log")

            If File.Exists(filePath) Then
                File.Delete(filePath)
            End If

            Dim listener = New TextWriterTraceListener(filePath) With {
                .TraceOutputOptions = TraceOptions.ThreadId Or TraceOptions.DateTime
            }
            Trace.Listeners.Add(listener)
            Trace.AutoFlush = True
        End If

        MenuItemEx.UseTooltips = s.EnableTooltips
        Icon = g.Icon

        InitializeComponent()
        RestoreClientSize(62, 42)
        SetTip()

        TargetAspectRatioMenu = New ContextMenuStripEx
        SizeContextMenuStrip = New ContextMenuStripEx
        EncoderMenu = New ContextMenuStripEx
        AddHandler EncoderMenu.Opening, Sub(sender As Object, e As EventArgs) MenuOpening(sender, bnEncoder.Tag)
        ContainerMenu = New ContextMenuStripEx
        AddHandler ContainerMenu.Opening, Sub(sender As Object, e As EventArgs) MenuOpening(sender, bnMuxer.Tag)
        SourceAspectRatioMenu = New ContextMenuStripEx
        TargetFileMenu = New ContextMenuStripEx
        SourceFileMenu = New ContextMenuStripEx
        NextContextMenuStrip = New ContextMenuStripEx

        tbTargetFile.ContextMenuStrip = TargetFileMenu
        tbSourceFile.ContextMenuStrip = SourceFileMenu

        tbSourceFile.SendMessageCue("Drop video files on the window or use Open")
        tbTargetFile.SendMessageCue("Chosen after opening a source")

        Dim openMenu As New ContextMenuStripEx
        openMenu.Add("Single File...", AddressOf ShowOpenSourceSingleFileDialog)
        openMenu.Add("Multiple Files...", AddressOf ShowOpenSourceMultipleFilesDialog)
        openMenu.Add("Merge Files...", AddressOf ShowOpenSourceMergeFilesDialog)
        openMenu.Add("File Batch...", AddressOf ShowOpenSourceBatchFilesDialog)
        openMenu.Add("-")
        openMenu.Add("Blu-ray Folder...", AddressOf ShowOpenSourceBlurayFolderDialog)
        openMenu.Add("Blu-ray Image File...", AddressOf ShowOpenSourceBlurayImageDialog)

        bnOpen.Menu = openMenu
        bnOpen.ClickAction = AddressOf ShowOpenSourceSingleFileDialog
        bnOpen.ToolTipText = "Open a single source file"
        bnAddJob.Menu = NextContextMenuStrip
        bnAddJob.ClickAction = AddressOf AddJobFromButton

        AddHandler NextStep.ActionButton.Click, Sub() NextStepAction()
        AddHandler NextStep.ContinueButton.Click, Sub() SkipAssistantTip()

        CommandManager.AddCommandsFromObject(Me)
        CommandManager.AddCommandsFromObject(g.DefaultCommands)

        CustomMainMenu = New CustomMenu(AddressOf GetDefaultMainMenu, s.CustomMenuMainForm, CommandManager, MenuStrip.Items) With {.IsMenuBar = True}

        OpenProject(g.StartupTemplatePath)
        CustomMainMenu.AddKeyDownHandler(Me)
        CustomMainMenu.BuildMenu()
        UpdateAudioMenus()
        UpdateTargetSizeLabel()

        CustomSizeMenu = New CustomMenu(AddressOf GetDefaultMenuSize,
                s.CustomMenuSize, CommandManager, SizeContextMenuStrip.Items)

        CustomSizeMenu.AddKeyDownHandler(Me)
        CustomSizeMenu.BuildMenu()

        NextContextMenuStrip.Add("Add to top and open Jobs", Sub() AddJob(True, 0))
        NextContextMenuStrip.Add("Add to bottom and open Jobs", Sub() AddJob(True, -1))
        NextContextMenuStrip.Add("-")
        NextContextMenuStrip.Add("Add to top w/o opening Jobs", Sub() AddJob(False, 0))
        NextContextMenuStrip.Add("Add to bottom w/o opening Jobs", Sub() AddJob(False, -1))

        ThemeManager.SetCurrentTheme(s.ThemeName)
        ApplyTheme()

        AddHandler ThemeManager.CurrentThemeChanged, AddressOf OnCurrentThemeChanged
        AddHandler FiltersListView.Changed, AddressOf ApplyFilters
        ShowActivated = Not GetShowWithoutActivation()
    End Sub

    Sub OnCurrentThemeChanged(theme As Theme)
        ApplyTheme()
        Assistant(False)
    End Sub

    Sub ApplyTheme()
        ApplyTheme(ThemeManager.CurrentTheme)
    End Sub

    'native controls use the GTK theme, the style sheet colors the next-step bar and highlighted controls
    Sub ApplyTheme(theme As Theme)
        ResetHighlighting()
    End Sub

    Sub MenuOpening(sender As Object, tag As Object)
        Dim menu = TryCast(sender, ContextMenuStripEx)

        If menu Is Nothing Then Return
        If tag Is Nothing Then Return

        Dim allItems = menu.GetItems().OfType(Of MenuItemEx)
        Dim selectedItem = allItems.Where(Function(x) tag.Equals(x.Tag) AndAlso x.Path IsNot Nothing).OrderBy(Function(x) x.Path.Length).FirstOrDefault()

        If selectedItem Is Nothing Then Return

        Dim selectedSubPath = selectedItem.Path.LeftLast(" | ")
        Dim selectedSubSubPath = selectedSubPath.LeftLast(" | ")
        Dim selectedPathLevel = Regex.Matches(selectedItem.Path, " \| ").Count

        For Each item As MenuItemEx In allItems.Where(Function(x) Not String.IsNullOrEmpty(x.Path))
            Dim sameLevel = Regex.Matches(item.Path, " \| ").Count = selectedPathLevel
            Dim isParent = Not sameLevel AndAlso (selectedSubPath.Equals(item.Path) OrElse (selectedPathLevel > 1 AndAlso selectedSubSubPath.StartsWith(item.Path)))
            item.Marked = item.Path = selectedItem.Path OrElse isParent
        Next
    End Sub

    Function GetIfoFile() As String
        Dim dir = p.SourceFile.Dir

        If New DirectoryInfo(dir).Name.EndsWithEx("_temp") Then
            dir = dir.Parent
        End If

        If Not Directory.Exists(dir) Then Return Nothing

        'the file names of DVDs are usually upper case
        Dim name = p.SourceFile.FileName.DeleteRight(5) + "0.ifo"
        Return Directory.EnumerateFiles(dir).FirstOrDefault(Function(i) i.FileName.Equals(name, StringComparison.OrdinalIgnoreCase))
    End Function

    Sub RenameDVDTracks()
        Dim ifoPath = GetIfoFile()

        If Not File.Exists(ifoPath) Then
            Exit Sub
        End If

        For Each i In Directory.GetFiles(p.TempDir)
            If g.IsSourceSame(i) AndAlso i.Contains("VTS") Then
                Dim regex As New Regex(" T(\d\d) ")
                Dim match = regex.Match(i)

                If match.Success Then
                    Dim dgID = CInt(match.Groups(1).Value)

                    Using mi As New MediaInfo(ifoPath)
                        For x = 0 To mi.AudioStreams.Count - 1
                            If mi.GetAudio(x, "ID/String").Contains(" (0x" & dgID & ")") Then
                                Dim stream = mi.AudioStreams(x)
                                Dim base = i.Base
                                base = regex.Replace(base, " ID" & x & " ")

                                If base.Contains("2_0ch") Then
                                    base = base.Replace("2_0ch", "2ch")
                                End If

                                If base.Contains("3_2ch") Then
                                    base = base.Replace("3_2ch", "6ch")
                                End If

                                If base.Contains(" DELAY") Then
                                    base = base.Replace(" DELAY", "")
                                End If

                                If base.Contains(" 0ms") Then
                                    base = base.Replace(" 0ms", "")
                                End If

                                If stream.Language.TwoLetterCode <> "iv" Then
                                    base += " " + stream.Language.EnglishName
                                End If

                                If stream.Title <> "" Then
                                    base += " " + stream.Title
                                End If

                                FileHelp.Move(i, Path.Combine(i.Dir, base + i.ExtFull))
                            End If
                        Next
                    End Using
                End If
            End If
        Next
    End Sub

    Function IsSaveCanceled(Optional saveProject As Boolean = True) As Boolean
        SetLastModifiedTemplate()

        If s.ApplicationExitMode <> ApplicationExitMode.Regular Then
            Select Case s.ApplicationExitMode
                Case ApplicationExitMode.BypassProjectSaving
                    Return False
                Case ApplicationExitMode.ForceProjectSaving
                    If g.ProjectPath IsNot Nothing Then
                        SaveProjectPath(g.ProjectPath)
                    End If
                    Return False
                Case Else
                    Throw New NotImplementedException()
            End Select
        End If

        If ObjectHelp.GetCompareString(g.SavedProject) <> ObjectHelp.GetCompareString(p) Then
            'If String.IsNullOrWhiteSpace(p.SourceFile) AndAlso Log.Length > 0 Then Return False

            If s.AutoSaveProject AndAlso p.SourceFile <> "" Then
                If g.ProjectPath Is Nothing Then
                    g.ProjectPath = Path.Combine(p.TempDir, p.TargetFile.Base + ".srip")
                End If

                SaveProjectPath(g.ProjectPath)
                Return False
            End If

            Using td As New TaskDialog(Of DialogResult)
                td.Title = "Save changed project?"
                td.AddButton("Save", DialogResult.Yes)
                td.AddButton("Don't Save", DialogResult.No)
                td.AddButton("Cancel", DialogResult.Cancel)
                td.Show()
                Refresh()

                If td.SelectedValue = DialogResult.Yes Then
                    If g.ProjectPath Is Nothing Then
                        If Not OpenSaveProjectDialog() Then
                            Return True
                        End If
                    Else
                        SaveProjectPath(g.ProjectPath)
                    End If
                ElseIf td.SelectedValue = DialogResult.Cancel Then
                    Return True
                Else
                    Return False
                End If
            End Using
        End If
    End Function

    Sub UpdateRecentProjectsMenu()
        If IsDisposed Then Exit Sub

        For Each mi In CustomMainMenu.MenuItems
            If mi.CustomMenuItem?.MethodName = "DynamicMenuItem" AndAlso
                mi.CustomMenuItem.Parameters(0).Equals(DynamicMenuItemID.RecentProjects) Then

                mi.DropDownItems.ClearAndDisplose

                SyncLock s.RecentProjects
                    For Each recentProj In s.RecentProjects
                        If File.Exists(recentProj) AndAlso Not recentProj.Base = "recover" Then
                            Dim name = recentProj

                            If recentProj.Length > 70 Then
                                name = "..." + name.Remove(0, name.Length - 70)
                            End If

                            mi.DropDownItems.Add(New MenuItemEx(name, Sub() LoadProject(recentProj)))
                        End If
                    Next
                End SyncLock

                Exit For
            End If
        Next
    End Sub

    <DebuggerNonUserCode>
    Sub UpdateDynamicMenuAsync()
        Task.Run(Sub()
                     Thread.Sleep(500)

                     Try
                         Invoke(Sub() UpdateDynamicMenu())
                     Catch
                     End Try
                 End Sub)
    End Sub

    Sub UpdateDynamicMenu()
        PopulateProfileMenu(DynamicMenuItemID.EncoderProfiles)
        PopulateProfileMenu(DynamicMenuItemID.MuxerProfiles)
        PopulateProfileMenu(DynamicMenuItemID.AudioProfiles)
        PopulateProfileMenu(DynamicMenuItemID.FilterSetupProfiles)

        For Each iMenuItem In CustomMainMenu.MenuItems
            If iMenuItem.CustomMenuItem.MethodName = "DynamicMenuItem" Then
                If iMenuItem.CustomMenuItem.Parameters(0).Equals(DynamicMenuItemID.HelpApplications) Then
                    iMenuItem.DropDownItems.ClearAndDisplose

                    For Each pack In Package.Items.Values
                        MenuItemEx.Add(iMenuItem.DropDownItems, pack.Name.Substring(0, 1).ToUpperInvariant + " | " + pack.ID, Sub() pack.ShowHelp())
                    Next
                End If
            End If
        Next

        DoEvents()
    End Sub

    Async Sub UpdateScriptsMenuAsync()
        Dim files As String() = {}
        Dim events As String() = System.Enum.GetNames(GetType(ApplicationEvent))

        Await Task.Run(Sub()
                           If Folder.Scripts.DirExists Then
                               files = Directory.GetFiles(Folder.Scripts)
                           End If
                       End Sub)

        If IsDisposed OrElse files.Count = 0 Then
            Exit Sub
        End If

        For Each menuItem In CustomMainMenu.MenuItems
            If menuItem.CustomMenuItem.MethodName = "DynamicMenuItem" Then
                If menuItem.CustomMenuItem.Parameters(0).Equals(DynamicMenuItemID.Scripts) Then
                    menuItem.DropDownItems.ClearAndDisplose

                    For Each path In files
                        If Not events.Contains(path.FileName.Left(".")) AndAlso Not path.FileName.StartsWith("_") Then
                            MenuItemEx.Add(menuItem.DropDownItems, path.FileName.Base,
                                Sub() g.DefaultCommands.ExecutePowerShellFile(path))
                        End If
                    Next

                    menuItem.DropDownItems.Add(New SeparatorMenuItem)
                    MenuItemEx.Add(menuItem.DropDownItems, "Open Script Folder", Sub() g.ShellExecute(Folder.Scripts))
                End If
            End If
        Next
    End Sub

    Async Sub UpdateTemplatesMenuAsync()
        If IsDisposed Then Exit Sub

        Dim templates As IEnumerable(Of String) = Nothing

        Await Task.Run(Sub()
                           Thread.Sleep(500)

                           For Each iFile In Directory.GetFiles(Folder.Template, "*.srip.backup")
                               FileHelp.Move(iFile, Path.Combine(iFile.Dir, "Backup", iFile.Base))
                           Next

                           templates = Directory.GetFiles(Folder.Template, "*.srip", SearchOption.AllDirectories).OrderBy(Function(x) x.Count(Function(c) c = Path.DirectorySeparatorChar)).ThenBy(Function(x) x)
                       End Sub)


        For i = 0 To CustomMainMenu.MenuItems.Count - 1
            Dim menuItem = CustomMainMenu.MenuItems(i)

            If IsDisposed Then Exit Sub
            If menuItem.CustomMenuItem.MethodName <> "DynamicMenuItem" OrElse Not menuItem.CustomMenuItem.Parameters(0).Equals(DynamicMenuItemID.TemplateProjects) Then Continue For

            menuItem.DropDownItems.ClearAndDisplose()
            Dim items As New List(Of MenuItemEx)

            For Each template In templates
                Dim text = template.Replace(Folder.Template, "").Trim(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, " | ")
                text = text.Substring(0, text.LastIndexOf(".srip"))
                text += If(template = g.StartupTemplatePath, " (Startup)", "")

                MenuItemEx.Add(menuItem.DropDownItems, text, AddressOf LoadProject, template, Nothing)
            Next

            menuItem.DropDownItems.Add(New SeparatorMenuItem)
            MenuItemEx.Add(menuItem.DropDownItems, "Explore", Sub() g.ShellExecute(Folder.Template), "Opens the directory containing the templates.")
            MenuItemEx.Add(menuItem.DropDownItems, "Restore", AddressOf ResetTemplates, "Restores the default templates.")

            Exit For
        Next

        If IsDisposed Then Exit Sub

        DoEvents()
    End Sub

    Sub ResetTemplates()
        If MsgQuestion("Restore the default templates?") = DialogResult.OK Then
            Try
                FolderHelp.Delete(Folder.Template)
                Folder.Template.ToString()
                UpdateTemplatesMenuAsync()
            Catch ex As Exception
                g.ShowException(ex)
            End Try
        End If
    End Sub

    <Command("Loads a template.")>
    Sub LoadTemplate(name As String)
        Dim templates = Directory.GetFiles(Folder.Template, "*.srip", SearchOption.AllDirectories)

        For Each template In templates
            Dim temp = template.Replace(Folder.Template, "").Trim(Path.DirectorySeparatorChar)
            Dim text1 = temp.Substring(0, temp.LastIndexOf(".srip"))
            Dim text2 = text1.Replace(Path.DirectorySeparatorChar, " | ")

            If {text1.ToLowerEx(), text2.ToLowerEx()}.Contains(name.ToLowerEx()) Then
                LoadProject(template)
                Exit Sub
            End If
        Next
    End Sub

    Function LoadTemplateWithSelectionDialog(sources As IEnumerable(Of String), timeout As Integer, Optional templateFolder As String = "") As Boolean
        Return LoadTemplateWithSelectionDialog(sources?.Join("; "), timeout, templateFolder)
    End Function

    Function LoadTemplateWithSelectionDialog(sources As String(), timeout As Integer, Optional templateFolder As String = "") As Boolean
        Return LoadTemplateWithSelectionDialog(sources?.Join("; "), timeout, templateFolder)
    End Function

    <Command("Loads a template that you can choose from via dialog.")>
    Function LoadTemplateWithSelectionDialog(source As String, timeout As Integer, Optional templateFolder As String = "") As Boolean
        If Not templateFolder.DirExists Then templateFolder = Folder.Template
        If Not templateFolder.DirExists Then Return False
        Dim directories = Directory.GetDirectories(templateFolder, "*", SearchOption.TopDirectoryOnly).ToList()
        Dim templates = Directory.GetFiles(templateFolder, "*.srip", SearchOption.TopDirectoryOnly)?.Where(Function(x) x <> g.StartupTemplatePath)?.OrderBy(Function(x) x).ToList()
        If Not templates?.Any() Then Return True

        Using td As New TaskDialog(Of String)
            td.Timeout = If(s.ShowTemplateSelectionDefault = ShowTemplateSelectionDefaultMode.None, 0, timeout)
            td.Title = "Select a template"
            td.Content = If(String.IsNullOrWhiteSpace(source),
                "Please select a template you want to use:",
                $"Please select a template you want to use for:{BR}{source}")
            td.Icon = TaskIcon.Question

            Dim isModified = ObjectHelp.GetCompareString(g.SavedProject) <> ObjectHelp.GetCompareString(p)
            Dim isStartup = Not isModified AndAlso p.TemplateName = s.StartupTemplate

            If p.SourceFile = "" Then
                Dim postfix = If(isStartup, "(Startup and Current Template)", "(Current Template)")
                Dim name = If(isModified, "Current Template", $"{p.TemplateName}  {postfix}")
                td.AddCommand(name, "CURRENT")
            Else
                td.AddCommand("Last set Template", "LAST")
            End If

            If isModified OrElse Not isStartup Then
                td.AddCommand(g.StartupTemplatePath.Base() + " (Startup)", g.StartupTemplatePath, ThemeManager.CurrentTheme.TaskDialog.Button.TimeoutBackColor.SetHue(ThemeManager.CurrentTheme.HighlightHue))
            End If

            If templateFolder.Contains(Folder.Template + Path.DirectorySeparatorChar) Then
                td.AddCommand($". . {Path.DirectorySeparatorChar}", "UP")
            End If

            For i = 0 To templates.Count() - 1
                Dim text = templates(i).Replace(Folder.Template, "").Trim(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, $" {Path.DirectorySeparatorChar} ")
                text = text.Substring(0, text.LastIndexOf(".srip"))

                If text <> p.TemplateName OrElse isModified Then
                    td.AddCommand(text, templates(i))
                End If
            Next

            For i = 0 To directories.Count() - 1
                Dim text = directories(i).Replace(Folder.Template, "").Trim(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, $" {Path.DirectorySeparatorChar} ")
                text += $" {Path.DirectorySeparatorChar} . ."
                td.AddCommand(text, directories(i))
            Next

            If s.ShowTemplateSelectionDefault = ShowTemplateSelectionDefaultMode.None Then
                td.AddButton("Abort", "", False)
            ElseIf s.ShowTemplateSelectionDefault = ShowTemplateSelectionDefaultMode.Abort Then
                td.AddButton("Abort", "", True)
            ElseIf s.ShowTemplateSelectionDefault = ShowTemplateSelectionDefaultMode.CurrentLast Then
                If p.SourceFile = "" Then
                    td.AddButton("Current Template", "CURRENT", True)
                Else
                    td.AddButton("Last set Template", "LAST", True)
                End If
                td.AddButton("Abort", "", False)
            ElseIf s.ShowTemplateSelectionDefault = ShowTemplateSelectionDefaultMode.Startup Then
                td.AddButton("Startup Template", g.StartupTemplatePath, True)
                td.AddButton("Abort", "", False)
            End If

            Dim selection = td.Show()

            If selection = "" Then
                Return False
            ElseIf selection = "CURRENT" Then
                Return True
            ElseIf selection = "LAST" Then
                Return OpenProject(g.LastModifiedTemplate)
            ElseIf selection = "UP" Then
                Return LoadTemplateWithSelectionDialog(source, timeout, templateFolder.Dir())
            ElseIf selection.DirExists() Then
                Return LoadTemplateWithSelectionDialog(source, timeout, selection)
            Else
                Return OpenProject(td.SelectedValue, True)
            End If
        End Using
    End Function

    <Command("Adds batch jobs for multiple files.")>
    Sub AddBatchJobs(sourcefiles As String())
        If sourcefiles Is Nothing Then Return

        For Each sourcefile In sourcefiles
            AddBatchJob(sourcefile)
        Next
    End Sub

    <Command("Adds a batch job for a single file.")>
    Sub AddBatchJob(sourcefile As String)
        Dim batchFolder = Path.Combine(Folder.Settings, "Batch Projects")

        If Not Directory.Exists(batchFolder) Then
            Directory.CreateDirectory(batchFolder)
        End If

        g.RaiseAppEvent(ApplicationEvent.BeforeJobAdding)

        sourcefile = sourcefile.TrimQuotes()
        Dim batchProject = ObjectHelp.GetCopy(Of Project)(p)
        batchProject.BatchMode = True
        batchProject.SourceFiles = {sourcefile}.ToList
        Dim splits = sourcefile.Dir.Split(Path.DirectorySeparatorChar)
        Dim joins = splits.Select(Function(x) x.Substring(0, Math.Min(x.Length, 128 \ splits.Length))).Join("-").Replace(":", "-")
        Dim jobPath = Path.Combine(batchFolder, joins + " " + p.TemplateName + " - " + sourcefile.FileName + ".srip")
        SafeSerialization.Serialize(batchProject, jobPath)
        JobManager.AddJob(sourcefile.Base, jobPath)
    End Sub

    Function LoadProject(path As String) As Boolean
        SetLastModifiedTemplate()
        Refresh()

        If Not File.Exists(path) Then
            MsgWarn("Project file not found.", $"{path}{BR}could not be found.")
            s.UpdateRecentProjects(path)
            UpdateRecentProjectsMenu()
            UpdateTemplatesMenuAsync()
            Return False
        Else
            Return OpenProject(path)
        End If
    End Function

    Function OpenSaveProjectDialog() As Boolean
        Using dialog As New SaveFileDialog
            dialog.SetInitDir(p.TempDir)

            dialog.FileName = If(p.SourceFile <> "", p.TargetFile.Base, "Untitled")

            dialog.Filter = $"StaxRipLinux Project Files (*.srip)|*.srip"

            If dialog.ShowDialog() = DialogResult.OK Then
                If Not dialog.FileName.ToLowerInvariant.EndsWith(".srip") Then
                    dialog.FileName += ".srip"
                End If

                SaveProjectPath(dialog.FileName)
                Return True
            End If
        End Using
    End Function

    Function OpenSaveTemplateDialog() As Boolean
        If p.SourceFile = "" Then
            Using dialog As New SaveFileDialog
                dialog.Title = "Save Template"
                dialog.SetInitDir(p.TemplatePath.Dir, Folder.Template)
                dialog.FileName = If(p.TemplateName <> "", p.TemplateName, "Untitled")
                dialog.Filter = $"StaxRipLinux Template Files (*.srip)|*.srip"

                If dialog.ShowDialog() = DialogResult.OK Then
                    Dim fp = Path.GetFullPath(dialog.FileName)

                    If Not fp.StartsWith(Folder.Template, StringComparison.OrdinalIgnoreCase) Then
                        If MsgQuestion("Saving outside Template Folder", "You are going to save the template outside of the Template Folder, so this template won't be able to be used!", TaskButton.OkCancel) = DialogResult.Cancel Then
                            Return False
                        End If
                    End If
                    
                    If Not fp.ToLower.EndsWith(".srip") Then
                        fp += ".srip"
                    End If

                    SaveProjectPath(fp)
                    UpdateTemplatesMenuAsync()
                    Return True
                End If
            End Using
        Else
            MsgWarn("A template cannot be created after a source file was opened.")
        End If
    End Function

    Private BoundProject As Project

    'the target file text box is bound to the target file of the project
    Sub SetBindings(proj As Project, add As Boolean)
        RemoveHandler proj.PropertyChanged, AddressOf ProjectPropertyChanged

        If add Then
            BoundProject = proj
            tbTargetFile.Text = proj.TargetFile
            AddHandler proj.PropertyChanged, AddressOf ProjectPropertyChanged
        ElseIf BoundProject Is proj Then
            BoundProject = Nothing
        End If
    End Sub

    Sub ProjectPropertyChanged(sender As Object, e As PropertyChangedEventArgs)
        If sender Is BoundProject AndAlso e.PropertyName = NameOf(Project.TargetFile) AndAlso
            tbTargetFile.Text <> BoundProject.TargetFile Then

            tbTargetFile.Text = BoundProject.TargetFile
        End If

        Assistant()
    End Sub

    Function OpenProject(Optional path As String = Nothing) As Boolean
        Return OpenProject(path, True)
    End Function

    Function OpenProject(path As String, saveCurrentFirst As Boolean) As Boolean
        Try
            SetLastModifiedTemplate()

            If Not IsLoading AndAlso saveCurrentFirst AndAlso IsSaveCanceled() Then Return False
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then path = g.StartupTemplatePath

            Try
                p = SafeSerialization.Deserialize(New Project, path)
            Catch ex As Exception
                g.ShowException(ex, "Project file failed to load", "It will be reset to defaults." + BR2 + path)
                p = New Project()
                p.Init()
            End Try

            Return OpenProject(p, path)
        Catch ex As Exception
            OpenProject(g.StartupTemplatePath)
        End Try
    End Function

    Function OpenProject(proj As Project, Optional path As String = "", Optional markAsPChanged As Boolean = True) As Boolean
        Try
            SetLastModifiedTemplate()

            If proj IsNot Nothing AndAlso g.LastModifiedTemplate IsNot Nothing AndAlso ObjectHelp.GetCompareString(proj).Equals(ObjectHelp.GetCompareString(g.LastModifiedTemplate)) Then
                proj = ObjectHelp.GetCopy(g.LastModifiedTemplate)
                If String.IsNullOrWhiteSpace(path) Then path = proj.TemplatePath

                If path = "" Then
                    Dim templates = Directory.GetFiles(Folder.Template, "*.srip", SearchOption.AllDirectories).Where(Function(x) x.Base() = proj.TemplateName).ToList()
                    If templates.Count = 0 Then
                        g.ShowException(New FileNotFoundException(), "Template not found", $"'{proj.TemplateName}' was not found")
                    ElseIf templates.Count = 1 Then
                        path = templates.First()
                    Else
                        g.ShowException(New Exception("Too many templates with the same name found and could not choose."))
                    End If
                End If
            ElseIf String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                path = g.StartupTemplatePath
            End If

            p = If(proj IsNot Nothing, proj, SafeSerialization.Deserialize(New Project(), path))
            Log = p.Log

            FileHelp.Delete(IO.Path.Combine(Folder.Temp, "staxrip.log"))
            SetBindings(p, True)

            Text = $"{path.Base} - {g.DefaultCommands.GetApplicationDetails()}"

            If Not Environment.Is64BitProcess Then
                Text += " (32 bit)"
            End If

            ClosePreviewForms()
            PreviewScript = Nothing
            SkipAssistant = True

            If path.StartsWith(Folder.Template) Then
                g.ProjectPath = Nothing
                p.TemplateName = path.Base
                p.TemplatePath = path
                p.BatchMode = False
                Dim nnm = TypeOf p.Videoencoder.Muxer IsNot NullMuxer
                If nnm AndAlso Not p.VideoEncoder.Muxer.CanEdit Then
                    p.VideoEncoder.Muxer.CanEdit = nnm
                    SaveProjectPath(path)
                End If
            Else
                g.ProjectPath = path
            End If

            g.SetTempDir()

            Dim width = p.TargetWidth
            Dim height = p.TargetHeight
            Dim size = p.TargetSize
            Dim bitrate = p.VideoBitrate

            FiltersListView.Load()
            FiltersListView.Items(0).Selected = True

            If p.SourceFile = "" AndAlso p.SourceFiles.Any() Then p.SourceFile = p.SourceFiles(0)
            If p.SourceFile <> "" Then s.LastSourceDir = p.SourceFile.Dir

            BlockSourceTextBoxTextChanged = True
            tbSourceFile.Text = p.SourceFile
            BlockSourceTextBoxTextChanged = False

            p.VideoEncoder.OnStateChange()
            If p.TargetFile = "" Then
                p.VideoEncoder.UpdateTargetFile(True)
            End If
            tbTargetFile.TextBox.SelectionStart = tbTargetFile.Text.Length

            SetAudioTracks(p)

            If p.BitrateIsFixed Then
                tbBitrate.Text = ""
                tbBitrate.Text = bitrate.ToString
            Else
                tbTargetSize.Text = ""
                tbTargetSize.Text = size.ToString()
            End If

            SetSlider()

            tbTargetWidth.Text = width.ToString
            tbTargetHeight.Text = height.ToString

            If markAsPChanged Then SetSavedProject()

            SkipAssistant = False

            Assistant(False)
            s.UpdateRecentProjects(path)
            UpdateRecentProjectsMenu()
            g.RaiseAppEvent(ApplicationEvent.AfterProjectLoaded)
            g.RaiseAppEvent(ApplicationEvent.AfterProjectOrSourceLoaded)
            FiltersListView.RebuildMenu()

            If p.SourceFile <> "" AndAlso Not g.VerifyRequirements Then
                Throw New AbortException
            End If

            Return True
        Catch ex As Exception
            OpenProject(g.StartupTemplatePath)
        Finally
            SkipAssistant = False
        End Try
    End Function

    Sub SetSlider()
        tbResize.Maximum = ((Calc.FixMod(p.SourceWidth, TrackBarInterval) \ 2) * 3) \ Math.Max(TrackBarInterval, p.ForcedOutputMod)
        tbResize.Maximum += If(tbResize.Maximum Mod 3 = 0, 0, 1)
        tbResize.Maximum += If(tbResize.Maximum Mod 3 = 0, 0, 1)
    End Sub

    Sub SetSavedProject()
        g.SavedProject = ObjectHelp.GetCopy(Of Project)(p)
    End Sub

    Sub SetLastModifiedTemplate()
        If p.SourceFile <> "" OrElse Not p.Log.IsEmpty Then Exit Sub

        g.LastModifiedTemplate = ObjectHelp.GetCopy(Of Project)(p)
    End Sub

    Sub SetAudioTracks(Optional proj As Project = Nothing)
        proj = If(proj, p)
        proj.AudioTracks = If(proj.AudioTracks, New List(Of AudioTrack))
        Dim availableAudioTracks = Mathf.Clamp(proj.AudioTracksAvailable, 1, 25)
        Dim sourceFile = p.LastOriginalSourceFile
        Dim streams = If(sourceFile.FileExists(), MediaInfo.GetAudioStreams(sourceFile), New List(Of AudioStream))
        Try
            pnAudio.Content = Nothing
            ShownAudioTracks.Clear()

            If proj.AudioTracks.Count > availableAudioTracks Then
                For index = proj.AudioTracks.Count - 1 To availableAudioTracks Step -1
                    proj.AudioTracks.RemoveAt(index)
                Next
            End If

            For i = 0 To availableAudioTracks - 1
                Dim index = i

                Dim textEditContextMenuStripEx = New ContextMenuStripEx
                Dim audioTrack As AudioTrack
                Dim audioProfile As AudioProfile = If(index < proj.AudioTracks.Count AndAlso proj.AudioTracks(index)?.AudioProfile IsNot Nothing, proj.AudioTracks(index)?.AudioProfile, New MuxAudioProfile())
                If audioProfile.Streams Is Nothing OrElse Not audioProfile.Streams.Any() Then audioProfile.Streams = streams
                Dim textEdit = New AudioTextEdit(index)
                Dim languageLabel = New AudioLanguageLabel(index) With {
                    .Text = audioProfile.Language.Name
                }
                Dim nameLabel = New AudioNameButtonLabel(index) With {.ContextMenuStripEx = New ContextMenuStripEx}
                Dim editLabel = New AudioEditButtonLabel(index) With {
                    .ClickAction = Sub()
                                       audioTrack.AudioProfile.EditProject()
                                       UpdateAudioMenus()
                                       UpdateSizeOrBitrate()
                                       nameLabel.Text = g.ConvertPath(audioTrack.AudioProfile.Name)
                                       languageLabel.Text = audioTrack.AudioProfile.Language.Name
                                       ShowAudioTip(audioTrack.AudioProfile)
                                   End Sub
                }

                textEdit.ContextMenuStrip = textEditContextMenuStripEx

                Dim textEditTextChanged = Sub(sender As Object, e As EventArgs)
                                              AudioTextEditChanged(audioTrack)
                                              UpdateAudioRowVisibility(proj)
                                          End Sub
                Dim textEditDoubleClick = Sub(sender As Object, e As EventArgs)
                                              AudioTextEditDoubleClick(audioTrack)
                                          End Sub
                Dim textEditMouseDown = Sub(sender As Object, e As MouseEventArgs)
                                            If e.Buttons = MouseButtons.Alternate Then
                                                UpdateAudioFileMenu(audioTrack, index, Sub() textEditDoubleClick(Nothing, Nothing))
                                            End If
                                        End Sub

                Dim menuButton As New ButtonEx With {.IconName = Styling.GetIconName("view-more-symbolic", "open-menu-symbolic"), .MinimumSize = Size.Empty}
                menuButton.AddStyleClass("flat")

                AddHandler menuButton.Click, Sub()
                                                 UpdateAudioFileMenu(audioTrack, index, Sub() textEditDoubleClick(Nothing, Nothing))
                                                 textEditContextMenuStripEx.Show(menuButton, 0, menuButton.Height)
                                             End Sub

                TipProvider.SetTip("Language of the track, Edit changes it", languageLabel)
                TipProvider.SetTip("Audio profile of the track", nameLabel)
                TipProvider.SetTip("Options of the track for the current project or template", editLabel)
                TipProvider.SetTip("Source streams, files and actions of the track", menuButton)

                AddHandler textEdit.TextChanged, textEditTextChanged
                AddHandler textEdit.DoubleClick, textEditDoubleClick
                AddHandler textEdit.MouseDown, textEditMouseDown

                If proj.AudioTracks.ElementAtOrDefault(index) Is Nothing Then
                    audioTrack = New AudioTrack(index) With {.AudioProfile = audioProfile, .EditLabel = editLabel, .LanguageLabel = languageLabel, .NameLabel = nameLabel, .TextEdit = textEdit,
                        .RowLabel = New AudioRowLabel(index), .MenuButton = menuButton}
                    proj.AudioTracks.Add(audioTrack)
                Else
                    audioTrack = New AudioTrack(index) With {.AudioProfile = proj.AudioTracks(index).AudioProfile, .EditLabel = editLabel, .LanguageLabel = languageLabel, .NameLabel = nameLabel, .TextEdit = textEdit,
                        .RowLabel = New AudioRowLabel(index), .MenuButton = menuButton}
                    proj.AudioTracks(index) = audioTrack
                End If
            Next

            Dim rows As New List(Of TableRow)

            For index = 0 To proj.AudioTracks.Count - 1
                Dim track = proj.AudioTracks(index)
                track.TextEdit.SendMessageCue("Audio file or source stream")

                rows.Add(New TableRow(
                    New TableCell(track.RowLabel),
                    New TableCell(track.TextEdit, True),
                    New TableCell(track.MenuButton),
                    New TableCell(track.LanguageLabel),
                    New TableCell(track.NameLabel),
                    New TableCell(track.EditLabel)))
            Next

            pnAudio.Content = New TableLayout(
                New TableRow(laNoAudio),
                New TableRow(New TableLayout(rows) With {.Spacing = New Size(FH(0.5), FH(0.35))})) With {.Spacing = New Size(0, 0)}

            UpdateAudioRowVisibility(proj)
        Catch ex As Exception
            g.ShowException(ex)
        Finally
            UpdateAudioMenus()
            PopulateProfileMenu(DynamicMenuItemID.AudioProfiles)
        End Try
    End Sub

    Private ReadOnly ShownAudioTracks As New HashSet(Of Integer)

    'tracks without file and with the default profile are hidden until Add Track shows them,
    'tracks configured in a template stay visible without file
    Function IsAudioTrackShown(track As AudioTrack) As Boolean
        Return track.TextEdit.Text <> "" OrElse ShownAudioTracks.Contains(track.Index) OrElse
            TypeOf track.AudioProfile IsNot MuxAudioProfile OrElse track.AudioProfile.Language.IsDetermined
    End Function

    Sub UpdateAudioRowVisibility(proj As Project)
        If proj?.AudioTracks Is Nothing Then Exit Sub

        Dim anyShown = False
        Dim anyHidden = False

        For Each track In proj.AudioTracks
            If track.MenuButton Is Nothing Then Continue For

            Dim shown = IsAudioTrackShown(track)

            For Each control In New Control() {track.RowLabel, track.TextEdit, track.MenuButton, track.LanguageLabel, track.NameLabel, track.EditLabel}
                control.Visible = shown
            Next

            anyShown = anyShown OrElse shown
            anyHidden = anyHidden OrElse Not shown
        Next

        laNoAudio.Visible = Not anyShown
        bnAddAudio.Enabled = anyHidden
    End Sub

    Sub bnAddAudio_Click() Handles bnAddAudio.Click
        Dim track = p.AudioTracks.FirstOrDefault(Function(x) x.MenuButton IsNot Nothing AndAlso Not IsAudioTrackShown(x))

        If track IsNot Nothing Then
            ShownAudioTracks.Add(track.Index)
            UpdateAudioRowVisibility(p)

            'the menu opens when the shown row has its position
            UIBeginInvoke(Sub() track.MenuButton.PerformClick())
        End If
    End Sub

    Sub AddAudioTracks()
        If p.AudioTracks.Count = 0 Then Exit Sub

        Dim files = g.GetFilesInTempDirAndParent().AsEnumerable()
        files = files.Where(Function(x) FileTypes.Audio.Contains(x.Ext.ToLowerInvariant()))
        files = files.Where(Function(x) g.IsSourceSame(x))
        files = files.Where(Function(x) x.Ext.ToLowerInvariant() <> "avs")
        files = files.Where(Function(x) Not x.ToLowerInvariant().Contains("_cut_"))
        files = files.Where(Function(x) Not x.ToLowerInvariant().Contains("_out"))
        files = files.Where(Function(x) Not x.ToLowerInvariant().Matches("_chunk\d+$"))
        files = files.OrderBy(Function(x) x, New StringLogicalComparer()).ToList()

        Dim hqAudioFiles = files.Where(Function(x) FileTypes.AudioHQ.Contains(x.Ext()))
        Dim normalAudioFiles = files.Where(Function(x) (FileTypes.Audio.Except(FileTypes.AudioHQ)).Contains(x.Ext()))
        Dim orderedAudioFiles = hqAudioFiles.Concat(normalAudioFiles).Distinct()
        Dim groupedAudioFiles = orderedAudioFiles.GroupBy(Function(x) g.ExtractLanguageFromPath(x)).ToList()

        Dim setLanguages = p.AudioTracks.Select(Function(x) x.AudioProfile.Language).Where(Function(x) x.IsDetermined).Select(Function(x) x.ThreeLetterCode)
        Dim preferredAudios = p.PreferredAudio.ToLowerInvariant().SplitNoEmptyAndWhiteSpace(",", ";", " ")
        Dim selectedAudios = preferredAudios.Concat(setLanguages.Distinct().Except(preferredAudios)).ToList()

        Dim audioTracks As New List(Of (FilePath As String, Language As Language, Stream As AudioStream))

        Dim addAudioTrack = Sub(groupIndex As Integer, pathIndex As Integer)
                                If groupedAudioFiles.Count <= groupIndex Then Exit Sub
                                If groupedAudioFiles(groupIndex) Is Nothing Then Exit Sub
                                If groupedAudioFiles(groupIndex).Count() <= pathIndex Then Exit Sub
                                If groupedAudioFiles(groupIndex)(pathIndex) = "" Then Exit Sub

                                Dim filePath = groupedAudioFiles(groupIndex)(pathIndex)
                                Dim baseName = filePath.Base().UnescapeIllegalFileSysChars()

                                audioTracks.Add((filePath, groupedAudioFiles(groupIndex).Key, Nothing))
                            End Sub

        Dim audioStreams = New MediaInfo(p.SourceFile)?.AudioStreams
        Dim addAudioStream = Sub(index As Integer)
                                 If index >= audioStreams.Count Then Exit Sub
                                 If audioStreams(index) Is Nothing Then Exit Sub
                                 If p.DemuxAudio = DemuxMode.Dialog Then Exit Sub
                                 If s.Demuxers.Any(Function(x) x.Active AndAlso TypeOf x Is eac3toDemuxer AndAlso x.InputExtensions.Contains(p.SourceFile.Ext())) Then Exit Sub

                                 audioTracks.Add((audioStreams(index).Name, audioStreams(index).Language, audioStreams(index)))
                             End Sub


        For i = 0 To selectedAudios.Count() - 1
            Dim selectedAudio = selectedAudios(i)

            Dim idMatch = Regex.Match(selectedAudio, "^(\d+)([^,; ]*)$", RegexOptions.IgnoreCase)
            Dim languageMatch = Regex.Match(selectedAudio, "^([a-z]{2,}|[a-z]{2,4}(?:-[a-z]{2,})-[a-z]{2,5})([^,; ]*)$", RegexOptions.IgnoreCase)

            If idMatch.Success Then
                Dim id = idMatch.Groups(1).Value
                Dim fileAdded = False

                For j = 0 To groupedAudioFiles.Count() - 1
                    Dim item = groupedAudioFiles(j)

                    If item Is Nothing Then Continue For

                    For k = 0 To item.Count() - 1
                        If Not Regex.IsMatch(item(k).Base(), $"ID{id}\D") Then Continue For

                        addAudioTrack(j, k)
                        fileAdded = True
                    Next
                Next

                If Not fileAdded Then
                    addAudioStream(id.ToInt() - 1)
                End If
            ElseIf languageMatch.Success Then
                Dim prefLangString = languageMatch.Groups(1).Value
                Dim prefLang = New Language(prefLangString)

                For j = 0 To groupedAudioFiles.Count() - 1
                    Dim item = groupedAudioFiles(j)

                    If item IsNot Nothing Then
                        If prefLangString.ToLowerInvariant() <> "all" AndAlso prefLang.ThreeLetterCode <> item.Key.ThreeLetterCode AndAlso prefLang.Name <> item.Key.Name Then Continue For
                        If prefLangString.ToLowerInvariant() = "all" AndAlso selectedAudios.Contains(item.Key.ThreeLetterCode, StringComparer.OrdinalIgnoreCase) Then Continue For

                        For k = 0 To item.Count() - 1
                            Dim index = k
                            If prefLangString.ToLowerInvariant() = "all" AndAlso audioTracks?.Any(Function(x) x.FilePath = item(index)) Then Continue For

                            addAudioTrack(j, k)
                        Next
                    End If
                Next

                For j = 0 To audioStreams.Count() - 1
                    Dim item = audioStreams(j)

                    If item IsNot Nothing Then
                        If prefLangString.ToLowerInvariant() <> "all" AndAlso prefLang.ThreeLetterCode <> item.Language.ThreeLetterCode AndAlso prefLang.Name <> item.Language.Name Then Continue For
                        If prefLangString.ToLowerInvariant() = "all" AndAlso selectedAudios.Contains(item.Language.ThreeLetterCode, StringComparer.OrdinalIgnoreCase) Then Continue For
                        If audioTracks?.Any(Function(x) x.FilePath.ContainsEx("ID" + (item.Index + 1).ToString()) AndAlso x.Language.ThreeLetterCode = item.Language.ThreeLetterCode) Then Continue For

                        addAudioStream(j)
                    End If
                Next
            End If
        Next

        Dim addTrack = Sub(source As (FilePath As String, Language As Language, Stream As AudioStream), dest As AudioTrack)
                           If source.FilePath = "" Then Return
                           If dest Is Nothing Then Return

                           dest.AudioProfile.Reset(dest.Index)

                           If source.Stream Is Nothing Then
                               dest.TextEdit.Text = source.FilePath
                               dest.AudioProfile.File = source.FilePath
                           Else
                               dest.AudioProfile.File = p.SourceFile
                               dest.AudioProfile.Stream = source.Stream
                               dest.TextEdit.Text = $"{source.Stream.Name} ({p.SourceFile?.Ext()})"
                           End If

                           dest.AudioProfile.Language = source.Language
                           dest.LanguageLabel.Invalidate()
                       End Sub

        For index = 0 To audioTracks.Count - 1
            Dim i = index
            If String.IsNullOrWhiteSpace(audioTracks(i).FilePath) Then Continue For

            Dim sameLanguages = p.AudioTracks.Where(Function(x) x.TextEdit.Text = "" AndAlso
                                                        TypeOf x.AudioProfile IsNot NullAudioProfile AndAlso
                                                        x.AudioProfile.Language.ThreeLetterCode = audioTracks(i).Language.ThreeLetterCode).ToList()

            If sameLanguages.Any() Then
                addTrack(audioTracks(i), sameLanguages.First())
            Else
                For index2 = 0 To p.AudioTracks.Count - 1
                    Dim j = index2

                    If TypeOf p.AudioTracks(j).AudioProfile Is NullAudioProfile Then Continue For
                    If p.AudioTracks(j).TextEdit.Text <> "" Then Continue For
                    If p.AudioTracks(j).AudioProfile.Language.IsDetermined AndAlso p.AudioTracks(j).AudioProfile.Language.ThreeLetterCode <> audioTracks(i).Language.ThreeLetterCode Then Continue For

                    addTrack(audioTracks(i), p.AudioTracks(j))
                    Exit For
                Next
            End If
        Next
    End Sub

    Function GetAudioTextBox(ap As AudioProfile) As TextEdit
        Return p.AudioTracks.FirstOrDefault(Function(x) x.AudioProfile Is ap)?.TextEdit
    End Function

    Dim BlockAudioTextChanged As Boolean

    Sub AudioTextEditChanged(audioTrack As AudioTrack)
        If audioTrack Is Nothing Then Exit Sub
        If BlockAudioTextChanged Then Exit Sub

        Dim te = audioTrack.TextEdit
        Dim ap = audioTrack.AudioProfile

        If te.Text.StartsWith("/") Then
            If te.Text <> ap.File Then
                ap.Reset(audioTrack.Index)
                ap.File = te.Text

                If FileTypes.Audio.Contains(ap.File.Ext) Then
                    If Not p.Script.GetFilter("Source").Script.Contains("DirectShowSource") Then
                        ap.Delay += g.ExtractDelay(ap.File)
                    End If

                    If p.TakeOverAudioName Then
                        If ap.StreamName = "" AndAlso ap.File.Contains("{") Then
                            ap.StreamName = g.ExtractTrackNameFromFilename(ap.File)
                        End If
                    End If

                    If p.TakeOverAudioDefault Then ap.Default = ( ap.Stream IsNot Nothing AndAlso ap.Stream.Default) OrElse g.ExtractDefaultFlagFromFilename(ap.File)
                    If p.TakeOverAudioForced Then ap.Forced = ( ap.Stream IsNot Nothing AndAlso ap.Stream.Forced) OrElse g.ExtractForcedFlagFromFilename(ap.File)
                End If

                ap.SetStreamOrLanguage()
            End If

            UpdateSizeOrBitrate()

            BlockAudioTextChanged = True
            te.Text = ap.DisplayName
            BlockAudioTextChanged = False
        ElseIf te.Text = "" Then
            audioTrack.Remove()
            UpdateSizeOrBitrate()
        End If

        audioTrack.LanguageLabel?.Invalidate()
        audioTrack.NameLabel.Text = g.ConvertPath(audioTrack.AudioProfile.Name)
    End Sub

    Sub AudioTextEditDoubleClick(audioTrack As AudioTrack)
        Using dialog As New OpenFileDialog
            Dim filter = FileTypes.Audio.ToList()
            filter.Insert(0, "avs")
            dialog.SetFilter(filter)
            dialog.SetInitDir(p.TempDir, s.LastSourceDir)

            If dialog.ShowDialog() = DialogResult.OK Then
                audioTrack.TextEdit.Text = dialog.FileName
            End If
        End Using
    End Sub

    Function GetPathFromIndexFile(sourcePath As String) As String
        For Each i In File.ReadAllLines(sourcePath)
            If i.StartsWith("/") Then
                If Regex.IsMatch(i, "^.+ \d+$") Then
                    i = i.LeftLast(" ")
                End If

                If File.Exists(i) AndAlso FileTypes.Video.Contains(i.Ext) Then
                    Return i
                End If
            End If
        Next
    End Function

    Function ShowSourceFilterSelectionDialog(inputFile As String) As VideoFilter
        Dim filters As New List(Of VideoFilter)

        If inputFile.Ext.EqualsAny("mp4", "m4v", "mov") Then
            AddSourceFilters({"LSMASHVideoSource", "LibavSMASHSource"}, filters)
        End If

        If FileTypes.Video.Contains(inputFile.Ext) AndAlso Not FileTypes.VideoText.Contains(inputFile.Ext) Then
            AddSourceFilters({"BestSource", "FFVideoSource", "LWLibavVideoSource", "ffms2", "LWLibavSource"}, filters)
        End If

        'DSS2 and AVISource use DirectShow and VFW, the FFmpeg based source filters replace them on Linux.
        'The d2v source filters replace DGDecode, D2V Witch indexes MPEG-2 sources for them like DGIndex.
        If inputFile.Ext = "d2v" OrElse (Package.D2VWitch.Path <> "" AndAlso MediaInfo.GetVideo(inputFile, "Format") = "MPEG Video") Then
            AddSourceFilters({"d2vsource", "d2v"}, filters)
        End If

        If p.Script.IsAviSynth Then
            filters = filters.Where(Function(filter) Not filter.Script.Replace(" ", "").Contains("clip=core.")).ToList
            filters.Insert(0, New VideoFilter("Source", "Automatic", "#avs"))
        Else
            filters = filters.Where(Function(filter) filter.Script.Replace(" ", "").Contains("clip=core.")).ToList
            filters.Insert(0, New VideoFilter("Source", "Automatic", "#vs"))
        End If

        Dim td As New TaskDialog(Of VideoFilter) With {
            .Title = If(p.Script.IsAviSynth, "Select an AviSynth source filter:", "Select a VapourSynth source filter:")
        }

        For Each f In filters
            td.AddCommand(f.Name, f)
        Next

        Dim ret = td.Show
        td.Dispose()

        If ret Is Nothing Then
            Throw New AbortException
        End If

        ret.Script = Macro.ExpandGUI(ret.Script, True).Value
        Return ret
    End Function

    Sub AddSourceFilters(filterNames As String(), filters As List(Of VideoFilter))
        Dim avsProfiles = s.AviSynthProfiles.Where(Function(cat) cat.Name = "Source").First.Filters
        Dim vsProfiles = s.VapourSynthProfiles.Where(Function(cat) cat.Name = "Source").First.Filters
        Dim allFilters = avsProfiles.Concat(vsProfiles)

        For Each f In allFilters
            For Each filterName In filterNames
                If f.Script.ToLowerInvariant.Contains(filterName.ToLowerInvariant + "(") OrElse
                    f.Script.ToLowerInvariant.Contains(filterName.ToLowerInvariant + ".") Then

                    If filters.Where(Function(val) val.Name = f.Name).Count = 0 Then
                        filters.Add(f.GetCopy)
                    End If
                End If
            Next
        Next
    End Sub

    Sub OpenAnyFile(files As IEnumerable(Of String), showTemplateSelection As Boolean, Optional errorTimeout As Integer = 0)
        If files Is Nothing Then Exit Sub
        If Not files.Any() Then Exit Sub

        SetLastModifiedTemplate()

        Dim showTemplateSelectionTimeout = If(s.ShowTemplateSelection <> ShowTemplateSelectionMode.Never, s.ShowTemplateSelectionTimeout, 0)
        files = files.Select(Function(filePath) New FileInfo(filePath.TrimQuotes()).FullName)

        If files(0).Ext = "srip" Then
            OpenProject(files(0))
        ElseIf FileTypes.Video.Contains(files(0).Ext.ToLowerInvariant) Then
            files = files.OrderBy(Function(x) FileTypes.Video.Contains(x.Ext)).ThenBy(Function(x) FileTypes.Audio.Contains(x.Ext))
            If showTemplateSelection Then
                If LoadTemplateWithSelectionDialog(files, showTemplateSelectionTimeout) Then
                    OpenVideoSourceFiles(files, True, errorTimeout)
                Else
                    Exit Sub
                End If
            Else
                OpenVideoSourceFiles(files, True, errorTimeout)
            End If
        ElseIf FileTypes.IsoImage.ContainsAny(files.Select(Function(s) s.Ext.ToLowerInvariant)) Then
            files = files.Where(Function(x) FileTypes.IsoImage.Contains(x.Ext().ToLowerInvariant())).OrderBy(Function(x) x, New StringLogicalComparer())
            If showTemplateSelection Then
                If LoadTemplateWithSelectionDialog(files(0), showTemplateSelectionTimeout) Then
                    OpenBlurayImage(files(0))
                Else
                    Exit Sub
                End If
            Else
                OpenBlurayImage(files(0))
            End If
        ElseIf FileTypes.Audio.ContainsAny(files.Select(Function(s) s.Ext.ToLowerInvariant)) Then
            Dim audioFiles = files.Where(Function(x) FileTypes.Audio.Contains(x.Ext().ToLowerInvariant())).OrderBy(Function(x) x, New StringLogicalComparer())
            Dim fileIndex = 0
            Dim freeAudioTracks = p.AudioTracks.Where(Function(x) x.TextEdit.Text = "")

            If audioFiles?.Any() AndAlso freeAudioTracks?.Any() Then
                For Each track In freeAudioTracks
                    If fileIndex >= audioFiles.Count() Then Exit For

                    track.TextEdit.Text = audioFiles(fileIndex)
                    fileIndex += 1
                Next
            End If
        Else
            files = files.OrderBy(Function(x) FileTypes.Video.Contains(x.Ext)).ThenBy(Function(x) FileTypes.Audio.Contains(x.Ext))

            Dim action = Sub()
                             If files(0).DirExists() Then
                                 OpenBlurayFolder(files(0))
                             Else
                                 OpenVideoSourceFiles(files, True, errorTimeout)
                             End If
                         End Sub

            If showTemplateSelection Then
                If LoadTemplateWithSelectionDialog(files, showTemplateSelectionTimeout) Then
                    action()
                Else
                    Exit Sub
                End If
            Else
                action()
            End If
        End If
    End Sub

    Function OpenBlurayFolder(srcPath As String) As Boolean
        If String.IsNullOrWhiteSpace(srcPath) Then Return False
        If Not srcPath.DirExists() Then Return False

        If Directory.Exists(Path.Combine(srcPath, "BDMV")) Then
            srcPath = Path.Combine(srcPath, "BDMV")
        End If
        If Directory.Exists(Path.Combine(srcPath, "PLAYLIST")) Then
            srcPath = Path.Combine(srcPath, "PLAYLIST")
        End If
        If New DirectoryInfo(srcPath).Name <> "PLAYLIST" Then
            MsgWarn("No playlist directory found.")
            Return False
        End If

        Dim minDuration = eac3toForm.GetPlaylistOptions(p)

        Log.WriteEnvironment()
        Log.Write("Process Blu-Ray folder using eac3to", """" + Package.eac3to.Path + """ """ + srcPath + """" + minDuration + BR2)
        Log.WriteLine("Source Drive Type: " + New DriveInfo(srcPath).DriveType.ToString + BR)

        Dim output = ProcessHelp.GetConsoleOutput(Package.eac3to.Path, WineHelp.ToWinePath(srcPath).Escape & minDuration).Replace(VB6.vbBack, "")
        Log.WriteLine(output)

        Dim a = Regex.Split(output, "^\d+\)", RegexOptions.Multiline).ToList
        If a(0) = "" Then a.RemoveAt(0)

        Using td As New TaskDialog(Of Integer)
            td.Title = "Please select a playlist."

            For Each i In a
                If Not i.Contains(BR) Then Continue For
                Dim match = Regex.Match(i, "(\+\d+)\1{8,}", RegexOptions.Multiline)
                If match.Success AndAlso match.Groups.Count > 1 Then Continue For

                Dim value = a.IndexOf(i) + 1
                Dim text = value & ")  " & i.Left(BR).Trim()
                Dim description = i.Right(BR).TrimEnd()

                td.AddCommand(text, description, value)
            Next

            If td.Show() <> 0 Then
                OpenEac3toDemuxForm(srcPath, td.SelectedValue)
            End If
        End Using

        Return True
    End Function

    Function OpenBlurayImage(isoPath As String) As Boolean
        If String.IsNullOrWhiteSpace(isoPath) Then Return False
        If Not isoPath.FileExists() Then Return False

        Try
            Dim srcPath = IsoHelp.Mount(isoPath)

            If srcPath = "" Then
                Return False
            End If

            If Directory.Exists(Path.Combine(srcPath, "BDMV")) Then
                srcPath = Path.Combine(srcPath, "BDMV")
            End If
            If Directory.Exists(Path.Combine(srcPath, "PLAYLIST")) Then
                srcPath = Path.Combine(srcPath, "PLAYLIST")
            End If
            If New DirectoryInfo(srcPath).Name <> "PLAYLIST" Then
                MsgWarn("No playlist directory found.")
                Return False
            End If

            Dim minDuration = eac3toForm.GetPlaylistOptions(p)

            Log.WriteEnvironment()
            Log.Write("Process mounted Blu-Ray folder using eac3to", """" + Package.eac3to.Path + """ """ + srcPath + """" + minDuration + BR2)
            Log.WriteLine("Source Drive Type: " + New DriveInfo(srcPath).DriveType.ToString + BR)
            Log.WriteLine("Mounted ISO File:  " + isoPath.Escape() + BR)

            Dim output = ProcessHelp.GetConsoleOutput(Package.eac3to.Path, WineHelp.ToWinePath(srcPath).Escape & minDuration).Replace(VB6.vbBack, "")
            Log.WriteLine(output)

            Dim a = Regex.Split(output, "^\d+\)", RegexOptions.Multiline).ToList
            If a(0) = "" Then a.RemoveAt(0)

            Using td As New TaskDialog(Of Integer)
                td.Title = "Please select a playlist."

                For Each i In a
                    If Not i.Contains(BR) Then Continue For
                    Dim match = Regex.Match(i, "(\+\d+)\1{8,}", RegexOptions.Multiline)
                    If match.Success AndAlso match.Groups.Count > 1 Then Continue For

                    Dim value = a.IndexOf(i) + 1
                    Dim text = value & ")  " & i.Left(BR).Trim()
                    Dim description = i.Right(BR).TrimEnd()

                    td.AddCommand(text, description, value)
                Next

                If td.Show() <> 0 Then
                    OpenEac3toDemuxForm(srcPath, td.SelectedValue)
                End If
            End Using

            Return True
        Catch ex As Exception
            g.ShowException(ex)
            Return False
        Finally
            IsoHelp.Dismount(isoPath)
        End Try
    End Function

    Sub OpenVideoSourceFile(fp As String, Optional demux As Boolean = True, Optional errorTimeout As Integer = 0)
        OpenVideoSourceFiles({fp}, demux, errorTimeout)
    End Sub

    Sub OpenVideoSourceFiles(files As IEnumerable(Of String), Optional demux As Boolean = True, Optional errorTimeout As Integer = 0)
        OpenVideoSourceFiles(files, demux, False, errorTimeout)
    End Sub

    Sub OpenVideoSourceFiles(files As IEnumerable(Of String), demuxSource As Boolean, isEncoding As Boolean, Optional errorTimeout As Integer = 0)
        If p.SourceFile = "" Then SetLastModifiedTemplate()

        Dim recoverPath = g.ProjectPath
        Dim recoverProjectPath = Path.Combine(Folder.Temp, Guid.NewGuid.ToString + ".bin")
        Dim recoverText = Text
        Dim saveCurrent = p.SourceFile <> ""

        If saveCurrent AndAlso Not IsLoading Then
            If IsSaveCanceled() Then
                Return
            End If
        End If

        Try
            SafeSerialization.Serialize(p, recoverProjectPath)
            AddHandler AppDomain.CurrentDomain.ProcessExit, Sub() FileHelp.Delete(recoverProjectPath)
        Catch ex As Exception
            MsgWarn("Recovery not saved!", $"The recovery project could not be saved at:{BR}{recoverProjectPath}")
        End Try

        SkipAssistant = True

        Try
            files = files.Select(Function(filePath) New FileInfo(filePath.TrimQuotes()).FullName).OrderBy(Function(filePath) filePath, StringComparer.InvariantCultureIgnoreCase)

            If Not g.VerifySource(files) Then
                Throw New AbortException
            End If

            For Each i In files
                Dim name = i.FileName

                If name.ToUpperInvariant Like "VTS_0#_0.VOB" Then
                    If MsgQuestion("Are you sure you want to open the file " + name + "," + BR + "the first VOB file usually contains a menu.") = DialogResult.Cancel Then
                        Throw New AbortException
                    End If
                End If

                If name.ToUpperInvariant = "VIDEO_TS.VOB" Then
                    MsgWarn("The file VIDEO_TS.VOB can't be opened.")
                    Throw New AbortException
                End If
            Next

            p.SourceFiles = files.ToList()
            p.SourceFile = files(0)

            Debug.WriteLine(isEncoding)
            Debug.WriteLine(p.SourceFile)

            If p.SourceFile.Ext.EqualsAny(FileTypes.Image) Then
                If p.SourceFile.Base(p.SourceFile.Base.Length - 1).IsDigit Then
                    If p.Script.IsAviSynth Then
                        Dim digitCount = 0

                        For i = p.SourceFile.Base.Length - 1 To 0 Step -1
                            If p.SourceFile.Base(i).IsDigit Then
                                digitCount += 1
                            End If
                        Next

                        Dim startText = p.SourceFile.Base.Substring(0, p.SourceFile.Base.Length - digitCount)
                        Dim images As New List(Of String)
                        Dim allFiles = Directory.GetFiles(p.SourceFile.Dir)

                        For Each file In allFiles
                            If file.Base.Length = p.SourceFile.Base.Length AndAlso file.Ext = p.SourceFile.Ext AndAlso file.Base.StartsWith(startText) Then
                                images.Add(file)
                            End If
                        Next

                        images.Sort()

                        Dim filter = p.Script.GetFilter("Source")
                        filter.Path = "Image"
                        filter.Script = "ImageSource(" + VideoScript.ToAviSynthString(Path.Combine(images(0).Dir, p.SourceFile.Base.Substring(0,
                            p.SourceFile.Base.Length - digitCount) + "%0" & digitCount & "d." + p.SourceFile.Ext)) +
                            ", " & images(0).Base.Substring(p.SourceFile.Base.Length - digitCount).ToInt &
                            ", " & images(images.Count - 1).Base.Substring(p.SourceFile.Base.Length - digitCount).ToInt & ", 25)"
                    End If
                Else
                    Dim filter = p.Script.GetFilter("Source")
                    filter.Path = "Image"
                    filter.Script = "ImageSource(""%source_file%"", 0, 1000, 25)"
                End If
            End If

            FiltersListView.IsLoading = True
            ClosePreviewForms()
            PreviewScript = Nothing

            Dim preferredSourceFilter As VideoFilter = Nothing

            If p.SourceFiles.Count = 1 AndAlso
                p.Script.Filters(0).Name = "Manual" AndAlso
                Not p.NoDialogs AndAlso Not p.BatchMode AndAlso
                Not p.SourceFile.Ext.EqualsAny("avs", "vpy") Then

                preferredSourceFilter = ShowSourceFilterSelectionDialog(files(0))
            End If

            If preferredSourceFilter IsNot Nothing Then
                Dim isVapourSynth = preferredSourceFilter.Script.Replace(" ", "").Contains("clip=core.") OrElse preferredSourceFilter.Script = "#vs"

                If isVapourSynth Then
                    If Not Package.Python.VerifyOK(True) OrElse
                        Not Package.VapourSynth.VerifyOK(True) OrElse
                        Not Package.vspipe.VerifyOK(True) Then

                        Throw New AbortException
                    End If

                    If p.Script.IsAviSynth Then
                        p.Script = VideoScript.GetDefaults()(1)
                    End If
                Else
                    If Not Package.AviSynth.VerifyOK(True) Then Throw New AbortException

                    If p.Script.Engine = ScriptEngine.VapourSynth Then
                        p.Script = VideoScript.GetDefaults()(0)
                    End If
                End If

                p.Script.SetFilter(preferredSourceFilter.Category, preferredSourceFilter.Name, preferredSourceFilter.Script)
            End If

            If Not g.VerifyRequirements() Then
                Throw New AbortException
            End If

            p.LastOriginalSourceFile = p.SourceFile
            p.FirstOriginalSourceFile = p.SourceFile

            If FileTypes.VideoIndex.Contains(p.SourceFile.Ext) Then
                Dim path = GetPathFromIndexFile(p.SourceFile)

                If path <> "" Then
                    p.LastOriginalSourceFile = path
                    p.FirstOriginalSourceFile = path
                End If
            ElseIf p.SourceFile.Ext.EqualsAny({"avs", "vpy"}) Then
                Dim code = p.SourceFile.ReadAllText
                Dim matches = Regex.Matches(code, "('|"")(.+?)\1", RegexOptions.IgnoreCase)

                For Each match As Match In matches
                    If match.Success Then
                        Dim path = match.Groups(2).Value

                        If File.Exists(path) AndAlso FileTypes.Video.Contains(path.Ext) Then
                            If FileTypes.VideoIndex.Contains(path.Ext) Then
                                path = GetPathFromIndexFile(path)

                                If path <> "" Then
                                    p.LastOriginalSourceFile = path
                                    p.FirstOriginalSourceFile = path
                                    Exit For
                                End If
                            Else
                                p.LastOriginalSourceFile = path
                                p.FirstOriginalSourceFile = path
                                Exit For
                            End If
                        End If
                    End If
                Next
            End If

            g.SetTempDir()

            Dim sourcePAR = MediaInfo.GetVideo(p.LastOriginalSourceFile, "PixelAspectRatio")

            If sourcePAR <> "" Then
                p.SourcePAR.X = CInt(Convert.ToSingle(sourcePAR, CultureInfo.InvariantCulture) * 1000)
                p.SourcePAR.Y = 1000
            End If

            p.SourceVideoHdrFormat = MediaInfo.GetVideo(p.LastOriginalSourceFile, "HDR_Format_Commercial")
            p.SourceVideoHdrFormat = If(String.IsNullOrWhiteSpace(p.SourceVideoHdrFormat), "SDR", p.SourceVideoHdrFormat)
            p.SourceVideoHdrFormat = If(p.SourceVideoHdrFormat = "Dolby Vision", "DV", p.SourceVideoHdrFormat)

            If p.SourceVideoHdrFormat.ContainsAny("SDR", "HDR10", "HDR10+", "Dolby Vision", "DV") Then
                Dim hdrFormat = MediaInfo.GetVideo(p.LastOriginalSourceFile, "HDR_Format/String")

                If hdrFormat.ContainsEx("Dolby Vision") Then
                    p.SourceVideoHdrFormat = "DV"

                    Dim profileMatch = Regex.Match(hdrFormat, "Profile (\d+(?:\.\d+)?),")
                    If profileMatch.Success Then
                        p.SourceVideoHdrFormat += " " + profileMatch.Groups(1).Value
                    End If

                    If Regex.IsMatch(hdrFormat, "HDR10 compatible") Then
                        p.SourceVideoHdrFormat += " / HDR10"
                    ElseIf Regex.IsMatch(hdrFormat, "HDR10\+ Profile [AB] compatible") Then
                        p.SourceVideoHdrFormat += " / HDR10+"
                    End If
                End If
            End If

            p.SourceVideoFormat = MediaInfo.GetVideoFormat(p.LastOriginalSourceFile)
            p.SourceVideoFormatProfile = MediaInfo.GetVideo(p.LastOriginalSourceFile, "Format_Profile")
            p.SourceVideoBitDepth = MediaInfo.GetVideo(p.LastOriginalSourceFile, "BitDepth").ToInt
            p.SourceColorSpace = MediaInfo.GetVideo(p.LastOriginalSourceFile, "ColorSpace")
            p.SourceChromaSubsampling = MediaInfo.GetVideo(p.LastOriginalSourceFile, "ChromaSubsampling")
            p.SourceSize = If(p.LastOriginalSourceFile.FileExists(), p.LastOriginalSourceFile.FileSize, If(p.LastOriginalSourceFile.DirExists(), p.LastOriginalSourceFile.DirSize(), 0L))
            p.SourceVideoSize = MediaInfo.GetVideo(p.LastOriginalSourceFile, "StreamSize").ToLong()
            p.SourceBitrate = CInt(MediaInfo.GetVideo(p.LastOriginalSourceFile, "BitRate").ToInt / 1000)
            p.SourceFrameRateMode = MediaInfo.GetVideo(p.LastOriginalSourceFile, "FrameRate_Mode")
            p.SourceScanType = MediaInfo.GetVideo(p.LastOriginalSourceFile, "ScanType")
            p.SourceScanOrder = MediaInfo.GetVideo(p.LastOriginalSourceFile, "ScanOrder")

            Dim mkvMuxer = TryCast(p.VideoEncoder.Muxer, MkvMuxer)

            If mkvMuxer IsNot Nothing Then
                If p.TakeOverVideoLanguage AndAlso Not mkvMuxer.VideoTrackLanguage?.IsDetermined Then
                    mkvMuxer.VideoTrackLanguage = New Language(MediaInfo.GetVideo(p.LastOriginalSourceFile, "Language"))
                End If
                If p.TakeOverTitle Then
                    If mkvMuxer.Title = "" Then mkvMuxer.Title = MediaInfo.GetGeneral(p.LastOriginalSourceFile, "Title")
                    If mkvMuxer.Title = "" Then mkvMuxer.Title = MediaInfo.GetGeneral(p.LastOriginalSourceFile, "Movie")
                End If
            End If

            If Not isEncoding AndAlso p.BatchMode Then
                Assistant()
                Exit Sub
            End If

            Log.WriteEnvironment()
            Log.WriteHeader("Media Info Source File")

            For Each i In p.SourceFiles
                Log.WriteLine(i)
            Next

            Log.WriteLine(BR + MediaInfo.GetSummary(p.SourceFile))

            For Each i In DriveInfo.GetDrives()
                If i.DriveType = DriveType.CDRom AndAlso
                    p.TempDir.ToUpperInvariant.StartsWith(i.RootDirectory.ToString.ToUpperInvariant) Then

                    MsgWarn("Opening files from an optical drive requires to set a temp files folder in the options.")
                    Throw New AbortException
                End If
            Next

            If Not p.BatchMode Then p.VideoEncoder.UpdateTargetFile(True)
            If demuxSource Then Demux()

            If String.IsNullOrWhiteSpace(p.Hdr10PlusMetadataFile) OrElse String.IsNullOrWhiteSpace(p.HdrDolbyVisionMetadataFile?.Path) Then
                Dim metadatas = Task.Run(Async Function() Await FindHdrMetadataAsync(p)).Result
                p.Hdr10PlusMetadataFile = If(String.IsNullOrWhiteSpace(metadatas.jsonFile), Nothing, metadatas.jsonFile)
                p.HdrDolbyVisionMetadataFile = If(String.IsNullOrWhiteSpace(metadatas.rpuFile), Nothing, New DolbyVisionMetadataFile(metadatas.rpuFile))
            End If

            If p.ExtractHdrmetadata <> HdrmetadataMode.None AndAlso
                Not p.SourceVideoHdrFormat?.EqualsAny("", "SDR") AndAlso
                (String.IsNullOrWhiteSpace(p.Hdr10PlusMetadataFile) OrElse String.IsNullOrWhiteSpace(p.HdrDolbyVisionMetadataFile?.Path)) Then

                Select Case p.ExtractHdrmetadata
                    Case HdrmetadataMode.DolbyVision
                        ExtractDolbyVisionMetadata(p)
                    Case HdrmetadataMode.HDR10Plus
                        ExtractHdr10PlusMetadata(p)
                    Case Else
                        ExtractHdrMetadata(p)
                End Select
            End If

            p.VideoEncoder.SetMetaData(p.LastOriginalSourceFile)

            If p.LastOriginalSourceFile <> p.SourceFile AndAlso Not FileTypes.VideoText.Contains(p.SourceFile.Ext) Then
                p.LastOriginalSourceFile = p.SourceFile
            End If

            s.LastSourceDir = p.SourceFile.Dir

            If p.SourceFile.Ext = "avs" AndAlso p.Script.IsAviSynth Then
                p.Script.Filters.Clear()
                p.Script.Filters.Add(New VideoFilter("Source", "AVS Script Import", "Import(" + VideoScript.ToAviSynthString(p.SourceFile) + ")"))
            ElseIf p.SourceFile.Ext = "vpy" Then
                p.Script.Engine = ScriptEngine.VapourSynth
                p.Script.Filters.Clear()
                Dim code = "import vapoursynth as vs" + BR +
                           "core = vs.core" + BR +
                           "from importlib.machinery import SourceFileLoader" + BR +
                           $"SourceFileLoader('clip', {VideoScript.ToPythonString(p.SourceFile)}).load_module()" + BR +
                           "clip = vs.get_output().clip"
                p.Script.Filters.Add(New VideoFilter("Source", "VS Script Import", code))
            End If

            ModifyFilters(errorTimeout)
            FiltersListView.IsLoading = False
            FiltersListView.Load()

            RenameDVDTracks()

            If FileTypes.VideoAudio.Contains(p.LastOriginalSourceFile.Ext) Then
                Dim streams = MediaInfo.GetAudioStreams(p.LastOriginalSourceFile)
                For Each track In p.AudioTracks
                    track.AudioProfile.Streams = streams
                Next
            End If

            If p.SourceFile.Ext = "d2v" Then
                Dim content = p.SourceFile.ReadAllText

                If content.Contains("Aspect_Ratio=16:9") Then
                    p.SourceAnamorphic = True
                Else
                    Dim ifoFile = GetIfoFile()

                    If ifoFile <> "" Then
                        Dim dar2 = MediaInfo.GetVideo(ifoFile, "DisplayAspectRatio")

                        If dar2 = "1.778" Then
                            p.SourceAnamorphic = True
                        End If
                    End If
                End If

                If content.Contains("Frame_Rate=29970") Then
                    Dim m = Regex.Match(content, "FINISHED\s+(\d{1,3}(?:\.\d{1,2})?)\s?%\s+FILM")

                    If m.Success Then
                        Dim film = m.Groups(1).Value.ToSingle()
                        If film >= p.D2VAutoForceFilmThreshold Then
                            content = content.Replace($"Field_Operation=0{BR}Frame_Rate=29970 (30000/1001)", $"Field_Operation=1{BR}Frame_Rate=23976 (24000/1001)")
                            content.WriteFileSystemEncoding(p.SourceFile)
                        End If
                    End If
                End If
            End If

            UpdateSourceParameters()
            SetSlider()
            AddAudioTracks()

            If p.UseScriptAsAudioSource Then
                If p.AudioTracks.Any() Then
                    p.AudioTracks(0).TextEdit.Text = p.Script.Path
                End If
            End If

            BlockSourceTextBoxTextChanged = True
            tbSourceFile.Text = p.SourceFile
            BlockSourceTextBoxTextChanged = False

            s.LastPosition = 0

            UpdateTargetParameters(p)
            DemuxVobSubSubtitles()
            ConvertBluRaySubtitles()
            ExtractForcedVobSubSubtitles()
            ConvertBitmapSubtitles()
            p.VideoEncoder.UpdateTargetFile(True)
            g.RaiseAppEvent(ApplicationEvent.BeforeMuxingWhenSourceOpening)
            p.VideoEncoder.Muxer.Init()

            If p.HardcodedSubtitle Then
                g.AddHardcodedSubtitle()
            End If

            AutoCrop()
            AutoResize()

            If p.Script.IsFilterActive("Crop") AndAlso (p.CropLeft Or p.CropTop Or p.CropRight Or p.CropBottom) <> 0 Then
                g.OvercropWidth()

                If p.AutoSmartCrop Then
                    g.SmartCrop()
                End If
            End If

            Dim nonDvdFiles = files.Where(Function(x) Not Regex.IsMatch(x, ".*VTS_\d\d_\d\.vob", RegexOptions.IgnoreCase))

            If nonDvdFiles.Count() > 1 Then
                Dim psi = New ProcessStartInfo(AppInfo.ExecutablePath, $"-NoFocus -LoadTemplate:""{p.TemplateName}"" -LoadSourceFiles:""{nonDvdFiles.Skip(1).Join(""";""", False)}""") With {
                    .UseShellExecute = False,
                    .CreateNoWindow = True
                }

                Process.Start(psi)?.Dispose()
            End If

            If p.AutoCompCheck AndAlso p.VideoEncoder.IsCompCheckEnabled Then
                p.VideoEncoder.RunCompCheck()
            End If

            g.RaiseAppEvent(ApplicationEvent.AfterSourceOpened)
            SkipAssistant = False
            Assistant()
            g.RaiseAppEvent(ApplicationEvent.AfterSourceLoaded)
            g.RaiseAppEvent(ApplicationEvent.AfterProjectOrSourceLoaded)
            Log.Save()
        Catch ex As AbortException
            Log.Save()
            SetSavedProject()
            OpenProject(recoverProjectPath)
            Text = recoverText
            g.ProjectPath = recoverPath

            If isEncoding Then
                Throw New AbortException
            End If
        Catch ex As Exception
            g.OnException(ex)
            OpenProject("", False)
        Finally
            SkipAssistant = False
            If Not isEncoding Then
                ProcController.Finished()
            End If
        End Try
    End Sub

    Sub ModifyFilters(Optional timeout As Integer = 0)
        If p.SourceFile = "" Then Exit Sub

        Dim profiles = If(p.Script.IsAviSynth, s.AviSynthProfiles, s.VapourSynthProfiles)
        Dim preferences = If(p.Script.IsAviSynth, s.AviSynthFilterPreferences, s.VapourSynthFilterPreferences)
        Dim editAVS = p.Script.IsAviSynth AndAlso p.SourceFile.Ext <> "avs"
        Dim editVS = p.Script.IsVapourSynth AndAlso p.SourceFile.Ext <> "vpy"

        If p.AutoRotation AndAlso (editAVS OrElse editVS) Then
            Dim rot = MediaInfo.GetVideo(p.SourceFile, "Rotation").ToDouble

            If rot <> 0 Then
                Dim name = ""

                Select Case rot
                    Case 90
                        name = "Right"
                    Case 180
                        name = "Upside Down"
                    Case 270
                        name = "Left"
                    Case Else
                End Select

                If name <> "" Then
                    Dim filter = VideoFilter.GetDefault("Rotation", name, p.Script.Engine)

                    If filter IsNot Nothing Then
                        p.Script.SetFilter(filter.Category, filter.Name, filter.Script)
                    End If
                End If
            End If
        End If

        Dim sourceFilter = p.Script.GetFilter("Source")

        SetSourceFilter(sourceFilter, preferences, profiles, True, True, False, False)
        SetSourceFilter(sourceFilter, preferences, profiles, False, True, True, False)
        SetSourceFilter(sourceFilter, preferences, profiles, True, False, False, True)
        SetSourceFilter(sourceFilter, preferences, profiles, False, False, False, False)

        If (editAVS OrElse editVS) AndAlso Not sourceFilter.Script.Contains("(") Then
            Dim names = If(editAVS, {"FFVideoSource", "BestSource", "LWLibavVideoSource"}, {"ffms2", "BestSource", "LWLibavSource"})
            Dim candidates = names.Select(Function(name) VideoFilter.GetDefault("Source", name, p.Script.Engine)).Where(Function(f) f IsNot Nothing)
            Dim filter = If(candidates.FirstOrDefault(Function(f) VideoScript.IsInstalled(f.Script, p.Script.Engine)), candidates.First)
            p.Script.SetFilter(filter.Category, filter.Name, filter.Script)
        End If

        For Each iFilter In p.Script.Filters
            If iFilter.Script.Contains("$") Then
                iFilter.Script = Macro.ExpandGUI(iFilter.Script, True).Value
            End If
        Next

        Dim errorMsg As String

        Try
            errorMsg = p.SourceScript.GetError
        Catch ex As AbortException
            errorMsg = ex.Message
            timeout = -1
        Catch ex As Exception
            errorMsg = ex.Message
        End Try

        If errorMsg <> "" Then
            Log.WriteHeader("Error opening source")
            Log.WriteLine(errorMsg + BR2)
            Log.WriteLine(p.SourceScript.GetFullScript)
            Log.Save()

            MsgError("Script Error", errorMsg, timeout)
            Throw New AbortException
        End If

        If Not editAVS AndAlso Not editVS Then
            Exit Sub
        End If

        Dim sourceInfo = p.SourceScript.Info

        If p.FixFrameRate Then
            Dim fixedFrameRate = FixFrameRate(sourceInfo.FrameRateNum, sourceInfo.FrameRateDen)

            If fixedFrameRate.num <> sourceInfo.FrameRateNum OrElse fixedFrameRate.den <> sourceInfo.FrameRateDen Then
                If editAVS Then
                    p.Script.GetFilter("Source").Script += BR + "AssumeFPS(" & fixedFrameRate.num & ", " & fixedFrameRate.den & ")"
                Else
                    p.Script.GetFilter("Source").Script += BR + "clip = core.std.AssumeFPS(clip, None, " & fixedFrameRate.num & ", " & fixedFrameRate.den & ")"
                End If

                p.SourceScript.Synchronize()
            End If
        End If

        If editAVS Then
            Dim miFPS = MediaInfo.GetFrameRate(p.FirstOriginalSourceFile, 25)
            Dim avsFPS = sourceInfo.FrameRate

            If (CInt(miFPS) * 2) = CInt(avsFPS) Then
                p.Script.GetFilter("Source").Script += BR + "SelectEven().AssumeFPS(" & miFPS.ToInvariantString + ")"
                p.SourceScript.Synchronize()
            End If
        End If

        Dim bitDepthAdjusted = False

        If p.SourceChromaSubsampling <> "4:2:0" AndAlso p.ConvertChromaSubsampling Then
            Dim interlaced = p.SourceScanType.EqualsAny("Interlaced", "MBAFF")

            If editVS Then
                Dim sourceHeight = MediaInfo.GetVideo(p.LastOriginalSourceFile, "Height").ToInt
                Dim matrix = If(sourceHeight = 0 OrElse sourceHeight > 576, "709", "470bg")
                Dim format = If(p.SourceVideoBitDepth = 10, "YUV420P10", "YUV420P8")
                If p.ConvertToBits <> ConvertTo420BitDepth.None AndAlso p.ConvertToBits <> p.SourceVideoBitDepth Then
                    format = "YUV420P" & (p.ConvertToBits + 0)
                    bitDepthAdjusted = True
                End If
                Dim category = "Color"
                Dim name = $"Convert To {format}"
                Dim script = $"clip = clip.resize.Bicubic(format = vs.{format})"

                If interlaced Then
                    p.Script.Filters.Add(New VideoFilter(category, name, script, True))
                Else
                    p.Script.Filters.Insert(1, New VideoFilter(category, name, script, True))
                End If
            ElseIf editAVS Then
                Dim category = "Color"
                Dim name = $"ConvertToYUV420()"
                Dim script = $"ConvertToYUV420()"

                If interlaced Then
                    p.Script.Filters.Add(New VideoFilter(category, name, script, True))
                Else
                    p.Script.Filters.Insert(1, New VideoFilter(category, name, script, True))
                End If
            End If
        End If

        If p.ConvertToBits <> ConvertTo420BitDepth.None AndAlso p.ConvertToBits <> p.SourceVideoBitDepth AndAlso Not bitDepthAdjusted Then
            Dim bits = (p.ConvertToBits + 0)
            Dim category = "BitDepth"
            Dim name = $"Convert To {bits}-bit"

            If editVS Then
                Dim script = $"clip = core.fmtc.bitdepth(clip, bits={bits})"
                p.Script.Filters.Add(New VideoFilter(category, name, script, True))
            ElseIf editAVS Then
                Dim script = $"ConvertBits({bits})"
                p.Script.Filters.Add(New VideoFilter(category, name, script, True))
            End If
        End If
    End Sub

    Function FixFrameRate(num As Integer, den As Integer) As (num As Integer, den As Integer)
        Dim rate = num / den
        Return If(rate < 50 AndAlso rate > 49, (50, 1), (num, den))
    End Function

    Sub SetSourceFilter(
        sourceFilter As VideoFilter,
        preferences As StringPairList,
        profiles As List(Of FilterCategory),
        skipAnyExtension As Boolean,
        skipAnyFormat As Boolean,
        skipNonAnyExtension As Boolean,
        skipNonAnyFormat As Boolean)

        If Not sourceFilter.Script.Contains("(") Then
            For Each pref In preferences
                Dim extensions = pref.Name.SplitNoEmptyAndWhiteSpace({",", " ", ";"})

                For Each extension In extensions
                    extension = extension.ToLowerInvariant
                    Dim format = "*"

                    If extension.Contains(":") Then
                        format = extension.Right(":").ToLowerInvariant
                        extension = extension.Left(":").ToLowerInvariant
                    End If

                    If skipAnyExtension AndAlso extension = "*" Then Continue For
                    If skipAnyFormat AndAlso format = "*" Then Continue For
                    If skipNonAnyExtension AndAlso extension <> "*" Then Continue For
                    If skipNonAnyFormat AndAlso format <> "*" Then Continue For

                    If (extension = p.SourceFile.Ext OrElse extension = "*") AndAlso
                        (format = "*" OrElse format = MediaInfo.GetVideo(p.SourceFile, "Format").ToLowerInvariant) Then

                        Dim filters = profiles.Where(
                            Function(cat) cat.Name = "Source").First.Filters.Where(
                            Function(cat) cat.Name = pref.Value)

                        If filters.Count > 0 AndAlso VideoScript.IsInstalled(filters(0).Script, p.Script.Engine) Then
                            p.Script.SetFilter("Source", filters(0).Name, filters(0).Script)
                            Exit Sub
                        End If
                    End If
                Next
            Next
        End If
    End Sub

    Sub AutoCrop()
        g.CheckForModifiedDolbyVisionLevel5Data()

        If p.AutoCropMode = AutoCropMode.DolbyVisionOnly OrElse p.AutoCropMode = AutoCropMode.Always Then
            p.SourceScript.Synchronize(True, True, True)

            Dim useDvOnly = p.HdrDolbyVisionMetadataFile IsNot Nothing AndAlso p.VideoEncoder.IsDolbyVisionSet AndAlso p.HdrDolbyVisionMetadataFile.Crop <> CropPadding.Empty

            If useDvOnly Then
                Dim c = p.HdrDolbyVisionMetadataFile.Crop
                Dim sm = If(p.AutoCropDolbyVisionSideMode = AutoCropDolbyVisionSideMode.NoOverride, p.AutoCropSideMode, AutoCropSideMode.All)
                If p.AutoCropDolbyVisionSideMode = AutoCropDolbyVisionSideMode.Horizontal Then sm = AutoCropSideMode.Horizontal
                If p.AutoCropDolbyVisionSideMode = AutoCropDolbyVisionSideMode.Vertical Then sm = AutoCropSideMode.Vertical
                g.SetCrop(c.Left, c.Top, c.Right, c.Bottom, sm, p.ForcedOutputModDirection, True)
            ElseIf p.AutoCropMode <> AutoCropMode.Disabled Then
                Dim info = p.SourceScript.Info
                Dim selectionMode = Convert.ToInt32(p.AutoCropFrameSelectionMode)
                Dim selectionValue = If(p.AutoCropFrameSelectionMode = AutoCropFrameSelectionMode.FrameInterval, p.AutoCropFrameIntervalFrameSelection, p.AutoCropFixedFramesFrameSelection)
                selectionValue = If(p.AutoCropFrameSelectionMode = AutoCropFrameSelectionMode.TimeInterval, p.AutoCropTimeIntervalFrameSelection, selectionValue)
                Dim considerationMode = Convert.ToInt32(p.AutoCropFrameRangeMode)
                Dim considerationThresholdBegin = 0
                Dim considerationThresholdEnd = 0
                Dim luminanceThreshold = CInt(p.AutoCropLuminanceThreshold * 100)
                Dim vfw = 0

                If p.AutoCropFrameRangeMode = AutoCropFrameRangeMode.Automatic Then
                    Dim threshold = CInt(Conversion.Fix(info.FrameCount * 0.05))
                    considerationThresholdBegin = threshold
                    considerationThresholdEnd = threshold
                ElseIf p.AutoCropFrameRangeMode = AutoCropFrameRangeMode.ManualThreshold Then
                    considerationThresholdBegin = p.AutoCropFrameRangeThresholdBegin
                    considerationThresholdEnd = p.AutoCropFrameRangeThresholdEnd
                End If

                Dim parameters = $"{selectionMode} {selectionValue} {considerationThresholdBegin} {considerationThresholdEnd} {luminanceThreshold.ToInvariantString()} {vfw}"
                Dim savingFilePath = Path.Combine(p.TempDir, "autocrop.tmp")
                Dim fileLines = If(savingFilePath.FileExists(), File.ReadAllLines(savingFilePath), {""})
                Dim fileParameters = If(fileLines.Length > 0, fileLines(0), "")
                Dim values = If(fileLines.Length > 1, fileLines(1), "")
                Dim valuesPattern = "(\d+),(\d+),(\d+),(\d+)"
                Dim redo = Not (Regex.IsMatch(values, valuesPattern) AndAlso parameters.Equals(fileParameters))

                If redo Then
                    Log.WriteHeader("Auto Crop")
                    Dim crop = g.DetectAutoCrop(Nothing)

                    If crop IsNot Nothing Then
                        values = String.Join(",", crop)
                        Log.WriteLine(values)

                        Try
                            Dim content = parameters & Environment.NewLine & values
                            content.WriteFileUTF8(savingFilePath)
                        Catch ex As Exception
                        End Try
                    Else
                        Log.WriteLine("AutoCrop failed...")
                    End If

                    Log.Save()
                End If

                Dim match = Regex.Match(values, valuesPattern)
                If match.Success Then
                    g.SetCrop(match.Groups(1).Value.ToInt, match.Groups(2).Value.ToInt, match.Groups(3).Value.ToInt, match.Groups(4).Value.ToInt, p.AutoCropSideMode, p.ForcedOutputModDirection, False)
                End If
            End If

            SetCropFilter()
            DisableCropFilter()
        End If
    End Sub

    Sub AutoResize()
        If p.Script.IsFilterActive("Resize") Then
            If p.AutoResizeImage <> 0 Then
                SetTargetImageSizeByPixel(p.AutoResizeImage)
            Else
                If p.AdjustHeight Then
                    Dim h = Calc.FixMod(CInt(p.TargetWidth / Calc.GetTargetDAR()), p.ForcedOutputMod)
                    tbTargetHeight.Text = h.ToString()
                End If
            End If
        Else
            Dim cropw = p.SourceWidth - p.CropLeft - p.CropRight
            tbTargetWidth.Text = cropw.ToString
            tbTargetHeight.Text = (p.SourceHeight - p.CropTop - p.CropBottom).ToString
        End If
    End Sub

    Sub ConvertBluRaySubtitles()
        If Not p.ConvertSup2Sub Then Exit Sub

        For Each i In g.GetFilesInTempDirAndParent
            If i.Ext = "sup" AndAlso g.IsSourceSameOrSimilar(i) AndAlso Not File.Exists(i.DirAndBase + ".idx") Then
                Using proc As New Proc
                    proc.Header = "Convert sup to sub"
                    proc.SkipStrings = {"#>", "#<", "Decoding frame"}
                    proc.File = Package.BDSup2SubPP.Path
                    proc.Arguments = "-o """ + i.DirAndBase + ".idx"" " + i.Escape
                    proc.Start()
                End Using
            End If
        Next
    End Sub

    Sub ConvertBitmapSubtitles()
        If Not p.ConvertBitmapSubtitles Then Exit Sub

        SubtitleOcr.Convert(g.GetFilesInTempDirAndParent.
            Where(Function(i) i.Ext.EqualsAny("sup", "idx") AndAlso g.IsSourceSameOrSimilar(i)).
            SelectMany(Function(i) Subtitle.Create(i)).
            Where(Function(i) i.Enabled))
    End Sub

    Sub ExtractForcedVobSubSubtitles()
        If Not p.ExtractForcedSubSubtitles Then Exit Sub

        For Each path In g.GetFilesInTempDirAndParent
            If path.ExtFull = ".idx" AndAlso g.IsSourceSameOrSimilar(path) AndAlso
                    Not path.Contains("_forced") AndAlso
                    Not File.Exists(path.DirAndBase + "_forced.idx") Then

                Dim idxContent = path.ReadAllTextDefault

                If idxContent.Contains(VB6.ChrW(&HA) + VB6.ChrW(&H0) + VB6.ChrW(&HD) + VB6.ChrW(&HA)) Then
                    idxContent = idxContent.FixBreak
                    idxContent = idxContent.Replace(BR + VB6.ChrW(&H0) + BR, BR + "langidx: 0" + BR)
                    idxContent.WriteFileSystemEncoding(path)
                End If

                'BDSup2Sub++ is optional on Linux
                If Package.BDSup2SubPP.Path = "" Then
                    Log.Write("Extract forced subtitles if existing", "Skipped, BDSup2Sub++ is not installed." + BR2)
                    Continue For
                End If

                Using proc As New Proc
                    proc.Header = "Extract forced subtitles if existing"
                    proc.SkipString = "# "
                    proc.WriteLog(path.FileName + BR2)
                    proc.File = Package.BDSup2SubPP.Path
                    proc.Arguments = "--forced-only -o " + (path.DirAndBase + "_forced.idx").Escape + " " + path.Escape
                    proc.AllowedExitCodes = {0, 1, 4}
                    proc.Start()
                End Using
            End If
        Next
    End Sub

    Sub DemuxVobSubSubtitles()
        If p.SubtitleMode = SubtitleMode.Disabled Then Exit Sub
        If Not {"vob", "m2v"}.Contains(p.LastOriginalSourceFile.Ext) Then Exit Sub

        Dim ifoPath = GetIfoFile()

        If ifoPath = "" Then Exit Sub
        If File.Exists(Path.Combine(p.TempDir, p.SourceFile.Base + ".idx")) Then Exit Sub

        Dim subtitleCount = MediaInfo.GetSubtitleCount(ifoPath)

        If subtitleCount > 0 Then
            If Package.VSRip.Path = "" Then
                Log.Write("Demux subtitles using VSRip", "Skipped, VSRip is only available on Windows." + BR2)
                Exit Sub
            End If

            Dim pgcCount As Byte
            Dim sectorPointer As Integer
            Dim buffer = File.ReadAllBytes(ifoPath)
            sectorPointer = (((buffer(&HCC) * &H1000000) + (buffer(&HCD) * &H10000)) + (buffer(&HCE) * &H100)) + (buffer(&HCF) * 1)
            pgcCount = CByte((buffer(sectorPointer * &H800) * &H100) + buffer((sectorPointer * &H800) + 1))

            If pgcCount <> 0 Then
                For i = 0 To pgcCount - 1
                    Dim a = ((sectorPointer * &H800) + (i * 8)) + 12
                    Dim b = (((buffer(a + 0) * &H1000000) + (buffer(a + 1) * &H10000)) + (buffer(a + 2) * &H100)) + (buffer(a + 3) * 1) + sectorPointer * &H800
                    Dim hour, min, sec As Integer

                    hour = CByte((((buffer(b + 4) And 240) >> 4) * 10) + (buffer(b + 4) And 15))
                    min = CByte((((buffer(b + 5) And 240) >> 4) * 10) + (buffer(b + 5) And 15))
                    sec = CByte((((buffer(b + 6) And 240) >> 4) * 10) + (buffer(b + 6) And 15))

                    If Math.Abs(p.SourceSeconds - ((hour * 60 ^ 2) + (min * 60) + sec)) < 30 Then
                        Dim args =
                            ifoPath + BR +
                            Path.Combine(p.TempDir, p.SourceFile.Base) + BR &
                            (i + 1) & BR +
                            "1" + BR +
                            "ALL" + BR +
                            "CLOSE"

                        Dim fileContent = Path.Combine(p.TempDir, p.TargetFile.Base + "_vsrip.txt")
                        args.WriteFileSystemEncoding(fileContent)

                        Using proc As New Proc
                            proc.Header = "Demux subtitles using VSRip"
                            proc.WriteLog(args + BR2)
                            proc.File = Package.VSRip.Path
                            proc.Arguments = fileContent.Escape
                            proc.AllowedExitCodes = {0, 1, 2}
                            proc.Start()
                        End Using

                        Exit For
                    End If
                Next
            End If
        End If
    End Sub

    Private TipMessage As String

    'the message identifies a skipped tip, the optional text is shown instead of it
    Function ProcessTip(message As String, Optional text As String = Nothing) As Boolean
        If message.Contains(BR2) Then
            message = message.Replace(BR2, BR)
        End If

        If message.Contains(VB6.vbLf + VB6.vbLf) Then
            message = message.FixBreak.Replace(BR2, BR)
        End If

        CurrentAssistantTipKey = message.MD5Hash

        If Not p.SkippedAssistantTips.Contains(CurrentAssistantTipKey) Then
            TipMessage = If(text, message)
            Return True
        End If
    End Function

    Function RemoveTip(message As String) As Boolean
        If message.Contains(BR2) Then
            message = message.Replace(BR2, BR)
        End If

        If message.Contains(VB6.vbLf + VB6.vbLf) Then
            message = message.FixBreak.Replace(BR2, BR)
        End If

        Return p.SkippedAssistantTips.Remove(message.MD5Hash)
    End Function

    'a reminder like checking the crop, it can be skipped
    Function Hint(title As String, clickAction As Action, actionText As String, ParamArray controls As Control()) As Boolean
        ShowTip(NextStepKind.Hint, title, clickAction, actionText, controls)
        Return False
    End Function

    Function Warn(title As String, ParamArray controls As Control()) As Boolean
        Return Warn(title, Nothing, Nothing, controls)
    End Function

    Function Warn(title As String, clickAction As Action, actionText As String, ParamArray controls As Control()) As Boolean
        ShowTip(NextStepKind.Warning, title, clickAction, actionText, controls)
        Return False
    End Function

    Function Block(title As String, ParamArray controls As Control()) As Boolean
        Return Block(title, Nothing, Nothing, controls)
    End Function

    Function Block(title As String, clickAction As Action, actionText As String, ParamArray controls As Control()) As Boolean
        CanIgnoreTip = False
        ShowTip(NextStepKind.Error, title, clickAction, actionText, controls)
        Return False
    End Function

    Sub ShowTip(kind As NextStepKind, title As String, clickAction As Action, actionText As String, controls As Control())
        AssistantClickAction = clickAction
        NextStep.Kind = kind
        NextStep.SetMessage(title, TipMessage)
        NextStep.SetAction(If(clickAction Is Nothing, Nothing, actionText))
        NextStep.ContinueButton.Visible = CanIgnoreTip
        Highlight(controls)
        UpdateNextButton()
    End Sub

    Private ReadOnly HighlightedControls As New List(Of Control)

    'outlines the controls a tip refers to, text edits by their entry
    Sub Highlight(ParamArray controls As Control())
        If controls.NothingOrEmpty Then Return
        ThemeRefresh = True

        For Each control In controls
            Dim target = If(TypeOf control Is TextEdit, DirectCast(control, TextEdit).InnerControl, control)

            If target IsNot Nothing AndAlso Not HighlightedControls.Contains(target) Then
                target.AddStyleClass("staxrip-attention")
                HighlightedControls.Add(target)
            End If
        Next
    End Sub

    Sub ResetHighlighting()
        For Each control In HighlightedControls
            'controls like those of the encoder panel are replaced while they are highlighted,
            'GTK destroys their widgets when they leave the window
            If control.IsDisposed OrElse control.ParentWindow Is Nothing Then Continue For
            control.RemoveStyleClass("staxrip-attention")
        Next

        HighlightedControls.Clear()
    End Sub

    Sub OpenTargetFolder()
        g.ShellExecute(p.TargetFile.Dir)
    End Sub

    Function AbortDueToLowDiskSpace() As Boolean
        Try
            If p.TargetFile = "" Then
                Exit Function
            End If

            Dim di As New DriveInfo(p.TargetFile.Dir)

            'network and FUSE file systems can report no size
            If di.TotalSize = 0 Then
                Exit Function
            End If

            If di.AvailableFreeSpace / PrefixedSize(3).Factor < s.MinimumDiskSpace Then
                Using td As New TaskDialog(Of String)
                    td.Title = "Low Disk Space"
                    td.Content = $"The target folder {p.TargetFile.Dir} has only " +
                                 $"{(di.AvailableFreeSpace / PrefixedSize(3).Factor):f2} {PrefixedSize(3).Unit} free disk space."
                    td.Icon = TaskIcon.Warning
                    td.AddButton("Continue")
                    td.AddButton("Abort")

                    If td.Show <> "Continue" Then
                        Return True
                    End If
                End Using
            End If
        Catch
        End Try
    End Function

    <Command("Creates a job and runs the job list.")>
    Sub StartEncoding()
        AssistantPassed = True
        AddJob(False, "")
        g.ProcessJobs()
    End Sub

    <Command("Extract dynamic HDR metadata from a source file.")>
    Sub ExtractHdrMetadata(sourcePath As String)
        Dim proj = New Project() With {
            .SourceFile = sourcePath,
            .FirstOriginalSourceFile = sourcePath
        }
        ExtractHdrMetadata(proj)
    End Sub

    Sub ExtractHdrMetadata(proj As Project)
        ExtractHdr10PlusMetadata(proj)
        ExtractDolbyVisionMetadata(proj)
    End Sub

    Sub ExtractHdr10PlusMetadata(proj As Project)
        If proj Is Nothing Then Return
        If String.IsNullOrWhiteSpace(proj.SourceFile) Then Return
        If Not File.Exists(proj.SourceFile) Then Return

        Dim sourcePaths = {proj.SourceFile, proj.FirstOriginalSourceFile, proj.LastOriginalSourceFile}

        For Each sourcePath As String In sourcePaths
            'the project of the ExtractHdrMetadata command only defines the first two paths
            If Not File.Exists(sourcePath) Then Continue For
            Dim mi = New MediaInfo(sourcePath)

            If Not mi.GetVideo("HDR_Format_Commercial")?.ContainsAny("HDR10+") Then Continue For
            If mi.GetVideo("Format") <> "HEVC" Then Continue For

            Dim jsonPath = sourcePath.ChangeExt("json")
            If Not String.IsNullOrWhiteSpace(proj.TempDir) Then
                jsonPath = If(sourcePath.Contains(proj.TempDir) AndAlso Not proj.TempDir.EndsWithEx("_temp"), jsonPath, If(proj.TempDir.DirExists(), $"{Path.Combine({proj.TempDir, "HDR10PlusMetadata.json"})}", jsonPath))
            End If

            If Not jsonPath.FileExists() AndAlso Package.HDR10PlusTool.Path = "" Then
                MsgWarn("HDR10+ metadata not extracted", "hdr10plus_tool was not found, install it to keep the HDR10+ metadata.")
                Continue For
            End If

            If Not jsonPath.FileExists() Then
                Dim commandLine = $"{Package.ffmpeg.Path.Escape} -hide_banner -probesize 50M -i {sourcePath.Escape} -an -sn -dn -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {Package.HDR10PlusTool.Path.Escape} extract -o {jsonPath.Escape} -"

                    Try
                        Using proc As New Proc
                            proc.Priority = s.ToolProcessPriority
                            proc.Package = Package.HDR10PlusTool
                            proc.Project = If(proj, p)
                            proc.Header = "Extract HDR10+ metadata"
                            proc.Encoding = Encoding.UTF8
                            proc.SetShellCommand(commandLine)
                            proc.SkipStrings = Proc.GetSkipStrings(commandLine)
                            proc.AllowedExitCodes = {0}
                            proc.OutputFiles = {jsonPath}
                            proc.Start()
                        End Using

                    If jsonPath?.FileExists() Then
                        Try
                            Dim fi = New FileInfo(jsonPath)
                            If fi.Length < 100 Then
                                File.Delete(jsonPath)
                            End If
                        Catch ex As Exception
                            jsonPath = ""
                        End Try
                    End If
                Catch ex As AbortException
                    Throw
                Catch ex As Exception
                    g.ShowException(ex)
                    Throw New AbortException
                Finally
                    Log.Save()
                End Try
            End If

            If jsonPath.FileExists() Then
                proj.Hdr10PlusMetadataFile = jsonPath
                Exit For
            End If
        Next
    End Sub

    Sub ExtractDolbyVisionMetadata(proj As Project)
        If proj Is Nothing Then Return
        If String.IsNullOrWhiteSpace(proj.SourceFile) Then Return
        If Not File.Exists(proj.SourceFile) Then Return

        Dim sourcePaths = {proj.SourceFile, proj.FirstOriginalSourceFile, proj.LastOriginalSourceFile}

        For Each sourcePath As String In sourcePaths
            If Not File.Exists(sourcePath) Then Continue For
            Dim isEL = False
            Dim files = Directory.GetFiles(sourcePath.Dir, sourcePath.Base + "*_EL*.*", SearchOption.TopDirectoryOnly).AsEnumerable()
            files = files?.Where(Function(x) {"h265", "hevc"}.Contains(x.Ext))

            If files?.Any() Then
                sourcePath = files.First()
                isEL = True
            End If

            Dim format = MediaInfo.GetVideoFormat(sourcePath)
            If format <> "HEVC" Then Continue For

            Dim fileHdrFormat = MediaInfo.GetVideo(sourcePath, "HDR_Format/String")
            If isEL OrElse (Regex.IsMatch(fileHdrFormat, "Dolby Vision.*Profile|HDR10+ Profile B")) Then
                Dim rpuPath = sourcePath.ChangeExt("rpu")
                Dim doviFile = proj.HdrDolbyVisionMetadataFile

                If Not String.IsNullOrWhiteSpace(proj.TempDir) Then
                    rpuPath = If(sourcePath.Contains(proj.TempDir), rpuPath, If(proj.TempDir.DirExists(), $"{Path.Combine({proj.TempDir, "HDRDVmetadata.rpu"})}", rpuPath))
                End If

                If Not rpuPath.FileExists() AndAlso Package.DoViTool.Path = "" Then
                    MsgWarn("Dolby Vision metadata not extracted", "dovi_tool was not found, install it to keep the Dolby Vision metadata.")
                    Continue For
                End If

                Try
                    If Not rpuPath.FileExists() Then
                        Dim commandLine = $"{Package.ffmpeg.Path.Escape} -hide_banner -probesize 50M -i {sourcePath.Escape} -an -sn -dn -c:v copy -bsf:v hevc_mp4toannexb -f hevc - | {Package.DoViTool.Path.Escape} extract-rpu - -o {rpuPath.Escape}"
                        Dim throwError As (Title As String, Content As String)

                        Using proc As New Proc
                            Dim throwIf = Sub(value As String)
                                              If value?.Contains("Unexpected RPU NALU") Then
                                                  throwError = ("Unexpected RPU NALU", "Unexpected RPU NALU found, extraction aborts...")
                                                  proc.Kill()
                                              End If
                                              If value?.Contains("Discarding") Then
                                                  throwError = ("Discarding", "Dovi_tool is discarding data, extraction aborts...")
                                                  proc.Kill()
                                              End If
                                          End Sub
                            proc.Priority = s.ToolProcessPriority
                            proc.Package = Package.DoViTool
                            proc.Project = If(proj, p)
                            proc.Header = "Extract Dolby Vision metadata"
                            proc.Encoding = Encoding.UTF8
                            proc.SetShellCommand(commandLine)
                            proc.SkipStrings = Proc.GetSkipStrings(commandLine)
                            proc.AllowedExitCodes = {0}
                            proc.OutputFiles = {rpuPath}
                            proc.ReadOutput = True
                            AddHandler proc.OutputDataReceived, throwIf
                            AddHandler proc.ErrorDataReceived, throwIf
                            proc.Start()
                        End Using

                        If throwError.Title IsNot Nothing AndAlso throwError.Content IsNot Nothing Then Throw New ErrorAbortException(throwError.Title, throwError.Content)

                        If rpuPath?.FileExists() Then
                            Try
                                Dim fi = New FileInfo(rpuPath)
                                If fi.Length > 100 Then
                                    doviFile = New DolbyVisionMetadataFile(rpuPath)
                                Else
                                    File.Delete(rpuPath)
                                End If
                            Catch ex As Exception
                                doviFile = New DolbyVisionMetadataFile(rpuPath)
                            End Try
                        End If
                    End If
                Catch ex As ErrorAbortException
                    g.ShowException(ex, Nothing, Nothing, s.ErrorMessageTimeout)
                    doviFile = Nothing
                Catch ex As AbortException
                    g.ShowException(ex)
                    doviFile = Nothing
                    Throw
                Catch ex As Exception
                    g.ShowException(ex)
                    doviFile = Nothing
                    Throw New AbortException
                Finally
                    Log.Save()
                End Try

                proj.HdrDolbyVisionMetadataFile = doviFile
                Exit For
            End If
        Next
    End Sub

    Async Function FindHdrMetadataAsync(proj As Project) As Task(Of (jsonFile As String, rpuFile As String))
        If proj Is Nothing Then Return Nothing
        If String.IsNullOrWhiteSpace(proj.SourceFile) Then Return Nothing

        Dim sourcePath = proj.SourceFile
        Dim jsonFile = ""
        Dim rpuFile = ""
        Dim files As IEnumerable(Of String)

        Dim searchTask = Task.Run(Sub()
                                      Try
                                          files = Directory.GetFiles(sourcePath.Dir(), $"{sourcePath.Base}*.*", SearchOption.TopDirectoryOnly)
                                          jsonFile = files.FirstOrDefault(Function(x) {"json"}.Contains(x.Ext) AndAlso Not x.Base.EndsWithAny("_L5", "_Config"))
                                          rpuFile = files.FirstOrDefault(Function(x) {"bin", "rpu"}.Contains(x.Ext) AndAlso Not x.Base.EndsWith("_Cropped"))

                                          If Not String.IsNullOrWhiteSpace(proj.TempDir) AndAlso String.IsNullOrWhiteSpace(jsonFile) AndAlso String.IsNullOrWhiteSpace(rpuFile) Then
                                              files = Directory.GetFiles(proj.TempDir, "*.*", SearchOption.TopDirectoryOnly)
                                              jsonFile = If(String.IsNullOrWhiteSpace(jsonFile), files.FirstOrDefault(Function(x) {"json"}.Contains(x.Ext) AndAlso Not x.Base.EndsWithAny("_L5", "_Config")), jsonFile)
                                              rpuFile = If(String.IsNullOrWhiteSpace(rpuFile), files.FirstOrDefault(Function(x) {"bin", "rpu"}.Contains(x.Ext) AndAlso Not x.Base.EndsWith("_Cropped")), rpuFile)
                                          End If
                                      Catch ex As Exception
                                          Log.WriteLine(ex.Message)
                                          Log.Save()
                                      End Try
                                  End Sub)

        Await searchTask
        Return (jsonFile, rpuFile)
    End Function

    Sub Demux()
        Dim getFormat = Function() As String
                            Dim ret = MediaInfo.GetVideo(p.SourceFile, "Format")

                            Select Case ret
                                Case "MPEG Video"
                                    ret = "mpeg2"
                                Case "VC-1"
                                    ret = "vc1"
                            End Select

                            Return ret.ToLowerInvariant
                        End Function

        Dim srcScript = p.Script.GetFilter("Source").Script.ToLowerInvariant

        For Each i In s.Demuxers
            If Not i.Active AndAlso (i.SourceFilters.NothingOrEmpty OrElse Not srcScript.ContainsAny(i.SourceFilters.Select(Function(val) val.ToLowerInvariant + "(").ToArray)) Then
                Continue For
            End If

            If i.InputExtensions?.Length = 0 OrElse i.InputExtensions.Contains(p.SourceFile.Ext) Then
                If Not srcScript?.Contains("(") OrElse i.SourceFilters.NothingOrEmpty OrElse
                    srcScript.ContainsAny(i.SourceFilters.Select(Function(val) val.ToLowerInvariant + "(").ToArray) Then

                    Dim inputFormats = i.InputFormats.NothingOrEmpty OrElse
                        i.InputFormats.Contains(getFormat())

                    If inputFormats Then
                        i.Run(p)
                        Refresh()

                        For Each outExt In i.OutputExtensions
                            Dim exitFor = False

                            For Each iFile In Directory.GetFiles(p.TempDir, "*." + outExt)
                                If g.IsSourceSame(iFile) AndAlso
                                    p.SourceFile <> iFile AndAlso
                                    Not iFile.Base.EndsWith("_CompCheck") AndAlso
                                    Not iFile.Base.EndsWith("_out") AndAlso
                                    Not iFile.Base.Matches("_chunk\d+$") AndAlso
                                    Not iFile.Base.ContainsEx("_cut_") Then

                                    p.SourceFile = iFile
                                    p.SourceFiles.Clear()
                                    p.SourceFiles.Add(p.SourceFile)

                                    BlockSourceTextBoxTextChanged = True
                                    tbSourceFile.Text = p.SourceFile
                                    BlockSourceTextBoxTextChanged = False

                                    exitFor = True
                                    Exit For
                                End If
                            Next

                            If exitFor Then
                                Exit For
                            End If
                        Next
                    End If
                End If
            End If
        Next

        p.Script.Synchronize()
    End Sub

    Sub Indexing()
        If p.SourceFile.Ext.EqualsAny("avs", "vpy") Then Exit Sub
        If Not p.Script.Path.FileExists Then p.Script.Synchronize(False, False)

        Dim codeLower = p.Script.GetFilter("Source").Script.ToLowerInvariant

        If codeLower.Contains("ffvideosource(") OrElse codeLower.Contains("ffms2(") OrElse codeLower.Contains("ffms2.source") Then
            If FileTypes.VideoIndex.Contains(p.SourceFile.Ext) Then
                p.SourceFile = p.LastOriginalSourceFile
                BlockSourceTextBoxTextChanged = True
                tbSourceFile.Text = p.SourceFile
                BlockSourceTextBoxTextChanged = False
            End If

            Dim cachePath = If(codeLower.Contains("cachefile") AndAlso p.TempDir <> "", Path.Combine(p.TempDir, g.GetSourceBase + ".ffindex"), p.SourceFile + ".ffindex")

            If Not cachePath.FileExists() Then
                g.ffmsindex(p.SourceFile, cachePath)
            End If
        ElseIf codeLower.Contains("lwlibavvideosource(") OrElse codeLower.Contains("lwlibavsource(") Then
            If FileTypes.VideoIndex.Contains(p.SourceFile.Ext) Then
                p.SourceFile = p.LastOriginalSourceFile
                BlockSourceTextBoxTextChanged = True
                tbSourceFile.Text = p.SourceFile
                BlockSourceTextBoxTextChanged = False
            End If

            If Not File.Exists(p.SourceFile + ".lwi") AndAlso
                Not File.Exists(Path.Combine(p.TempDir, g.GetSourceBase + ".lwi")) AndAlso
                File.Exists(p.Script.Path) AndAlso Not FileTypes.VideoText.Contains(p.SourceFile.Ext) Then

                Using proc As New Proc
                    proc.Header = "Index LWLibav"
                    proc.Encoding = Encoding.UTF8
                    proc.SkipString = "Creating lwi"

                    If p.Script.IsAviSynth Then
                        proc.File = Package.ffmpeg.Path
                        proc.Arguments = "-i " + p.Script.Path.LongPathPrefix.Escape + " -hide_banner"
                    Else
                        proc.File = Package.vspipe.Path
                        proc.Arguments = p.Script.Path.Escape + " - -i"
                    End If

                    proc.AllowedExitCodes = {0, 1}
                    proc.Start()
                End Using
            End If
        ElseIf codeLower.Contains("bsvideosource(") OrElse codeLower.Contains("bs.videosource(") Then
            If FileTypes.VideoIndex.Contains(p.SourceFile.Ext) Then
                p.SourceFile = p.LastOriginalSourceFile
                BlockSourceTextBoxTextChanged = True
                tbSourceFile.Text = p.SourceFile
                BlockSourceTextBoxTextChanged = False
            End If

            Dim cacheBase = If(codeLower.Contains("cachepath") AndAlso p.TempDir <> "", Path.Combine(p.TempDir, g.GetSourceBase), p.SourceFile)
            Dim cachePath = cacheBase + ".0.bsindex"

            If Not cachePath.FileExists() Then
                Using proc As New Proc
                    proc.Header = "Index BestSource"
                    proc.Encoding = Encoding.UTF8
                    proc.SkipStrings = {"Creating bsindex"}

                    If p.Script.IsAviSynth Then
                        proc.File = Package.ffmpeg.Path
                        proc.Arguments = "-i " + p.Script.Path.LongPathPrefix.Escape + " -hide_banner"
                    Else
                        proc.File = Package.vspipe.Path
                        proc.Arguments = p.Script.Path.Escape + " - -i"
                    End If

                    proc.AllowedExitCodes = {0, 1}
                    proc.Start()
                End Using
            End If
        ElseIf codeLower.Contains("lsmashvideosource(") OrElse
            codeLower.Contains("libavsmashsource(") OrElse
            codeLower.Contains("directshowsource(") OrElse
            codeLower.Contains("dss2(") Then

            If FileTypes.VideoIndex.Contains(p.SourceFile.Ext) Then
                p.SourceFile = p.LastOriginalSourceFile
                BlockSourceTextBoxTextChanged = True
                tbSourceFile.Text = p.SourceFile
                BlockSourceTextBoxTextChanged = False
            End If
        End If
    End Sub

    <Command("Shows a file browser to open a project file.")>
    Sub ShowFileBrowserToOpenProject()
        Using dialog As New OpenFileDialog
            dialog.Filter = "Project Files|*.srip"

            If dialog.ShowDialog() = DialogResult.OK Then
                Refresh()
                OpenProject(dialog.FileName)
            End If
        End Using
    End Sub

    <Command("Shows the Event Command dialog.")>
    Sub ShowEventCommandsDialog()
        Using form As New EventCommandsEditor(s.EventCommands)
            If form.ShowDialog() = DialogResult.OK Then
                s.EventCommands.Clear()

                For Each i As ListViewItem In form.lv.Items
                    s.EventCommands.Add(DirectCast(i.Tag, EventCommand))
                Next

                g.SaveSettings()
                g.SaveEvents()
            End If
        End Using
    End Sub

    <Command("Saves the current project.")>
    Sub SaveProject()
        If g.ProjectPath Is Nothing Then
            OpenSaveProjectDialog()
        Else
            SaveProjectPath(g.ProjectPath)
        End If
    End Sub

    <Command("Saves the current project at the specified path.")>
    Sub SaveProjectPath(<Description("The path may contain macros."),
        Editor(GetType(MacroStringTypeEditor), GetType(IValueEditor))> path As String)

        path = Macro.Expand(path)

        If path.StartsWith(Folder.Template) Then
            If p.SourceFile <> "" Then
                MsgWarn("A template cannot be created after a source file was opened.")
                Exit Sub
            End If
            p.Log.Clear()
            p.TemplateName = path.Base()
            p.TemplatePath = path
        Else
            g.ProjectPath = path
        End If

        Try
            SafeSerialization.Serialize(p, path)
            SetSavedProject()
            Text = $"{path.Base} - {g.DefaultCommands.GetApplicationDetails()}"
            s.UpdateRecentProjects(path)
            UpdateRecentProjectsMenu()
        Catch ex As Exception
            g.ShowException(ex)
        End Try
    End Sub

    <Command("Saves the current project.")>
    Sub SaveProjectAs()
        OpenSaveProjectDialog()
    End Sub

    <Command("Saves the current project as template.")>
    Sub SaveProjectAsTemplate()
        OpenSaveTemplateDialog()
    End Sub

    <Command("Closes the current project.")>
    Sub CloseProject()
        OpenProject(g.StartupTemplatePath)
    End Sub

    <Command("Starts the compressibility check.")>
    Sub StartCompCheck()
        p.VideoEncoder.RunCompCheck()
    End Sub

    <Command("Launches a new instance of StaxRipLinux.")>
    Sub StartNewInstance()
        Using p As New Process()
            p.StartInfo.FileName = AppInfo.ExecutablePath
            p.Start()
        End Using
    End Sub

    <Command("Exits StaxRipLinux.")>
    Sub [Exit]()
        Close()
    End Sub

    <Command("Exits StaxRipLinux without saving an unsaved project.")>
    Sub ExitWithoutSaving()
        s.ApplicationExitMode = ApplicationExitMode.BypassProjectSaving
        Close()
    End Sub

    <Command("Dialog to manage external tools.")>
    Sub ShowAppsDialog()
        Using form As New AppsForm
            Dim found As Boolean

            If s.StringDictionary.ContainsKey("RecentExternalApplicationControl") Then
                For Each pack In Package.Items.Values
                    If pack.Name + pack.Version = s.StringDictionary("RecentExternalApplicationControl") Then
                        form.ShowPackage(pack)
                        found = True
                        Exit For
                    End If
                Next
            End If

            If Not found Then
                form.ShowPackage(Package.x265)
            End If

            form.ShowDialog()
            g.SaveSettings()
        End Using
    End Sub

    <Command("Shows a dialog to manage video encoder profiles.")>
    Sub ShowEncoderProfilesDialog()
        g.LoadVideoEncoderProfiles()

        Using form As New ProfilesForm("Encoder Profiles", s.VideoEncoderProfiles, AddressOf g.LoadVideoEncoder, AddressOf GetNewVideoEncoderProfile, AddressOf VideoEncoder.GetDefaults)
            If form.ShowDialog() = DialogResult.OK Then
                g.SaveVideoEncoderProfiles()
            End If
        End Using

        PopulateProfileMenu(DynamicMenuItemID.EncoderProfiles)
    End Sub

    <Command("Dialog to manage Muxer profiles.")>
    Sub ShowMuxerProfilesDialog()
        If p.VideoEncoder.OpenMuxerProfilesDialog() = DialogResult.OK Then
            PopulateProfileMenu(DynamicMenuItemID.MuxerProfiles)
        End If

        Assistant()
    End Sub

    Sub RecalcBitrate()
        UpdateSizeOrBitrate()
    End Sub

    Function GetNewVideoEncoderProfile() As Profile
        Dim sb As New SelectionBox(Of VideoEncoder) With {
            .Title = "Add New Profile",
            .Text = "Please select a profile from the defaults."
        }
        sb.AddItem("Current Project", p.VideoEncoder)

        For Each i In VideoEncoder.GetDefaults()
            sb.AddItem(i)
        Next

        If sb.Show = DialogResult.OK Then
            Return sb.SelectedValue
        End If
    End Function

    Sub PopulateProfileMenu(id As DynamicMenuItemID)
        For Each i In CustomMainMenu.MenuItems
            If i.CustomMenuItem.MethodName = "DynamicMenuItem" AndAlso i.CustomMenuItem.Parameters(0).Equals(id) Then
                i.DropDownItems.ClearAndDisplose

                Select Case id
                    Case DynamicMenuItemID.EncoderProfiles
                        g.PopulateProfileMenu(i.DropDownItems, s.VideoEncoderProfiles, AddressOf ShowEncoderProfilesDialog, AddressOf g.LoadVideoEncoder)
                    Case DynamicMenuItemID.MuxerProfiles
                        g.PopulateProfileMenu(i.DropDownItems, s.MuxerProfiles, AddressOf ShowMuxerProfilesDialog, Sub(profile) p.VideoEncoder.LoadMuxer(profile))
                    Case DynamicMenuItemID.AudioProfiles
                        For j = 0 To p.AudioTracks.Count - 1
                            Dim index = j
                            Dim trackItem As New MenuItemEx($"Track {index + 1}")
                            i.DropDownItems.Add(trackItem)
                            g.PopulateProfileMenu(trackItem.DropDownItems, s.AudioProfiles, Sub() ShowAudioProfilesDialog(index), Sub(profile) g.LoadAudioProfile(profile, index))
                        Next
                    Case DynamicMenuItemID.FilterSetupProfiles
                        g.PopulateProfileMenu(i.DropDownItems, s.FilterSetupProfiles, AddressOf ShowFilterSetupProfilesDialog, AddressOf LoadFilterSetup)
                End Select

                Exit For
            End If
        Next
    End Sub

    <Command("Shows the crop dialog to crop borders.")>
    Sub ShowCropDialog()
        If p.SourceFile = "" Then
            ShowOpenSourceDialog()
        Else
            If Not g.VerifyRequirements OrElse Not g.IsValidSource Then
                Exit Sub
            End If

            Using form As New CropForm
                form.ShowDialog()
            End Using

            SetCropFilter()
            DisableCropFilter()
            Assistant()
        End If
    End Sub

    '<Command("Applies the current filter state.")>
    Sub ApplyFilters(Optional filters As List(Of VideoFilter) = Nothing)
        If PreviewScript Is Nothing Then Exit Sub

        PreviewScript.Filters = If(filters, p.Script.Filters)
    End Sub

    <Command("Dialog to preview or cut the video.")>
    Sub ShowPreviewDialog()
        ShowPreview(True)
    End Sub

    <Command("Window to preview or cut the video.")>
    Sub ShowPreview()
        ShowPreview(False)
    End Sub

    Sub ShowPreview(modal As Boolean)
        If p.SourceFile = "" Then
            ShowOpenSourceDialog()
        Else
            If Not g.VerifyRequirements OrElse Not g.IsValidSource Then Exit Sub

            If PreviewScript Is Nothing Then
                PreviewScript = p.Script.GetNewScript()
                If PreviewScript Is Nothing Then
                    Exit Sub
                End If
                PreviewScript.Path = Path.Combine(p.TempDir, p.TargetFile.Base + "_view." + PreviewScript.FileType)
                PreviewScript.RemoveFilter("Cutting")
            Else
                ApplyFilters()
            End If

            PreviewScript.RemoveFilter("Cutting")

            Dim err = PreviewScript.GetError()
            If err <> "" Then
                MsgError("Script Error", err)
                Exit Sub
            End If

            If modal Then
                Using form As New PreviewForm(PreviewScript)
                    form.ShowDialog()
                End Using
            Else
                Dim form As New PreviewForm(PreviewScript)
                form.Show()
            End If
        End If
    End Sub

    'previews cut the current project, so they must not outlive their source
    Sub ClosePreviewForms()
        For Each form In PreviewForm.Instances.ToArray
            form.Close()
        Next
    End Sub

    <Command("Dialog to configure the main menu.")>
    Sub ShowMainMenuEditor()
        s.CustomMenuMainForm = CustomMainMenu.Edit()
        UpdateTemplatesMenuAsync()
        UpdateScriptsMenuAsync()
        UpdateRecentProjectsMenu()
        UpdateDynamicMenuAsync()
        Refresh()
        g.SaveSettings()
    End Sub

    <Command("Dialog to manage batch jobs.")>
    Sub ShowJobsDialog()
        JobsForm.ShowForm()
    End Sub

    <Command("Clears the job list.")>
    Sub ClearJobs()
        FileHelp.Delete(Path.Combine(Folder.Settings, "Jobs.dat"))
    End Sub

    <Command("Loads an audio or video profile.")>
    Sub LoadProfile(<DispName("Video")> videoProfile As String,
                    <DispName("Audio 1")> audioProfile1 As String,
                    <DispName("Audio 2")> audioProfile2 As String)

        videoProfile = videoProfile.TrimQuotes().Trim()
        audioProfile1 = audioProfile1.TrimQuotes().Trim()
        audioProfile2 = audioProfile2.TrimQuotes().Trim()

        If videoProfile <> "" Then
            For Each i In s.VideoEncoderProfiles
                If i.Name = videoProfile Then
                    g.LoadVideoEncoder(i)
                End If
            Next
        End If

        If audioProfile1 <> "" Then
            For Each i In s.AudioProfiles
                If i.Name = audioProfile1 Then
                    g.LoadAudioProfile(i, 0)
                End If
            Next
        End If

        If audioProfile2 <> "" Then
            For Each i In s.AudioProfiles
                If i.Name = audioProfile2 Then
                    g.LoadAudioProfile(i, 1)
                End If
            Next
        End If
    End Sub

    Sub AddJob(Optional showJobsDialog As Boolean = True, Optional position As Integer = -1)
        If Not CanIgnoreTip Then
            MsgWarn("Assistant warning cannot be skipped.")
            Exit Sub
        End If

        If Not p.SkippedAssistantTips.Contains(CurrentAssistantTipKey) Then
            p.SkippedAssistantTips.Add(CurrentAssistantTipKey)
        End If

        If Not g.VerifyRequirements() Then
            Exit Sub
        End If

        ClosePreviewForms()

        If AssistantPassed Then
            If AbortDueToLowDiskSpace() Then Exit Sub

            g.RaiseAppEvent(ApplicationEvent.BeforeJobAdding)

            Dim jobPath = JobManager.JobPath
            Dim jobs = JobManager.GetJobs()?.Where(Function(x) x.Active AndAlso x.Path = jobPath)

            If jobs.Any() Then
                If MsgQuestion($"An active job for this project already exists.{BR}If you continue, it will be overwritten.", TaskButton.OkCancel) = DialogResult.Cancel Then
                    Exit Sub
                End If
            End If

            If TypeOf p.VideoEncoder IsNot NullEncoder Then
                Dim op = If(p.VideoEncoder.CanChunkEncode(), p.VideoEncoder.OutputPath.DirAndBase() + "_chunk1" + p.VideoEncoder.OutputPath.ExtFull(), p.VideoEncoder.OutputPath)

                If op.FileExists() Then
                    Select Case p.FileExistVideo
                        Case FileExistMode.Ask
                            Using td As New TaskDialog(Of String)
                                td.Title = "A video encoding output file already exists"
                                td.Content = "Would you like to skip video encoding and reuse the existing video encoder output file or would you like to re-encode and overwrite it?"
                                td.AddCommand("Reuse")
                                td.AddCommand("Re-encode")

                                Select Case td.Show
                                    Case "Reuse"
                                        p.SkipVideoEncoding = True
                                    Case "Re-encode"
                                        p.SkipVideoEncoding = False
                                    Case Else
                                        Exit Sub
                                End Select
                            End Using
                        Case FileExistMode.Overwrite
                            p.SkipVideoEncoding = False
                        Case FileExistMode.Skip
                            p.SkipVideoEncoding = True
                    End Select
                End If
            End If

            If p.AudioTracks.Any(Function(track) track.AudioProfile.File <> "" AndAlso
                                                  (TypeOf track.AudioProfile Is GUIAudioProfile OrElse TypeOf track.AudioProfile Is BatchAudioProfile) AndAlso
                                                  File.Exists(track.AudioProfile.GetOutputFile())) Then
                Select Case p.FileExistAudio
                    Case FileExistMode.Ask
                        Using td As New TaskDialog(Of String)
                            td.Title = "An audio encoding output file already exists"
                            td.Content = "Would you like to skip audio encoding and reuse existing audio encoding output files or would you like to re-encode and overwrite?"
                            td.AddCommand("Reuse")
                            td.AddCommand("Re-encode")

                            Select Case td.Show
                                Case "Reuse"
                                    p.SkipAudioEncoding = True
                                Case "Re-encode"
                                    p.SkipAudioEncoding = False
                                Case Else
                                    Exit Sub
                            End Select
                        End Using
                    Case FileExistMode.Overwrite
                        p.SkipAudioEncoding = False
                    Case FileExistMode.Skip
                        p.SkipAudioEncoding = True
                End Select
            End If

            AddJob(False, Nothing, position)

            If showJobsDialog Then
                Me.ShowJobsDialog()
            Else
                bnAddJob.ShowConfirmation("Job Added")
            End If
        Else
            Assistant()
        End If
    End Sub

    <Command("Adds a job to the job list.")>
    Sub AddJob(
        <DispName("Show Confirmation")>
        showConfirmation As Boolean,
        <DispName("Template Name"),
        Description("Name of the template to be loaded after the job was added. Empty to load no template.")>
        templateName As String,
        <DispName("Position to insert new job")>
        Optional position As Integer = -1)

        AddJob(showConfirmation, templateName, True, position)
    End Sub

    Sub AddJob(
        showConfirmation As Boolean,
        templateName As String,
        showAssistant As Boolean,
        Optional position As Integer = -1)

        If Not g.VerifyRequirements() Then
            Exit Sub
        End If

        If showAssistant AndAlso Not IsLoading AndAlso Not AssistantPassed Then
            MsgWarn("Please follow the assistant.")
            Exit Sub
        End If

        Dim jobPath = JobManager.JobPath
        SaveProjectPath(jobPath)
        JobManager.AddJob(jobPath, jobPath, position)

        If showConfirmation Then
            MsgInfo("Job added")
        End If

        If templateName <> "" Then
            LoadProject(Path.Combine(Folder.Template, templateName + ".srip"))
        End If
    End Sub

    <Command("Shows a dialog to compare different videos.")>
    Sub ShowVideoComparison()
        Dim form As New VideoComparisonForm
        form.Show()
    End Sub

    <Command("Dialog to edit filters.")>
    Sub ShowFiltersEditor()
        FiltersListView.ShowCodeEditor()
    End Sub

    <Command("Dialog to preview script code.")>
    Sub ShowCodePreview()
        g.ShowCodePreview(p.Script.GetFullScript)
    End Sub

    Sub SetCropFilter()
        If CInt(p.CropLeft Or p.CropTop Or p.CropRight Or p.CropBottom) <> 0 Then
            If Not g.EnableFilter("Crop") Then
                If p.Script.IsAviSynth Then
                    p.Script.InsertAfter("Source", New VideoFilter("Crop", "Crop", "Crop(%crop_left%, %crop_top%, -%crop_right%, -%crop_bottom%)"))
                Else
                    p.Script.InsertAfter("Source", New VideoFilter("Crop", "Crop", "clip = core.std.Crop(clip, %crop_left%, %crop_right%, %crop_top%, %crop_bottom%)"))
                End If
            End If

            FiltersListView.Load()
        End If
    End Sub

    Sub DisableCropFilter()
        Dim f = p.Script.GetFilter("Crop")

        If f IsNot Nothing AndAlso CInt(p.CropLeft Or p.CropTop Or p.CropRight Or p.CropBottom) = 0 Then
            f.Active = False
            FiltersListView.Load()
        End If
    End Sub

    <Command("Dialog to configure filter setup profiles.")>
    Sub ShowFilterSetupProfilesDialog()
        Using form As New ProfilesForm(
            "Filter Setup Profiles",
            s.FilterSetupProfiles,
            AddressOf LoadFilterSetup,
            AddressOf GetScriptAsProfile,
            AddressOf VideoScript.GetDefaults)

            If form.ShowDialog() = DialogResult.OK Then
                PopulateProfileMenu(DynamicMenuItemID.FilterSetupProfiles)
                FiltersListView.RebuildMenu()
            End If
        End Using
    End Sub

    <Command("Dialog to configure filter profiles.")>
    Sub ShowFilterProfilesDialog()
        Dim filterProfiles = If(p.Script.IsAviSynth, s.AviSynthProfiles, s.VapourSynthProfiles)
        Dim getDefaults = If(p.Script.IsAviSynth,
            Function() FilterCategory.GetAviSynthDefaults,
            Function() FilterCategory.GetVapourSynthDefaults)

        Using dialog As New MacroEditorDialog
            dialog.SetScriptDefaults()
            dialog.Text = $"Filter Profiles - {g.DefaultCommands.GetApplicationDetails()}"
            dialog.MacroEditorControl.Value = g.GetFilterProfilesText(filterProfiles)
            dialog.bnContext.Text = " Restore Defaults... "
            dialog.bnContext.Visible = True
            dialog.MacroEditorControl.rtbDefaults.Text = g.GetFilterProfilesText(getDefaults())
            dialog.bnContext.ClickAction = Sub()
                                               If Msg("Restore defaults?", Nothing, TaskIcon.Warning, TaskButton.OkCancel) = DialogResult.OK Then
                                                   dialog.MacroEditorControl.Value = g.GetFilterProfilesText(getDefaults())
                                                   MsgInfo("Defaults were restored.")
                                               End If
                                           End Sub

            If dialog.ShowDialog() = DialogResult.OK Then
                Try
                    filterProfiles = FilterCategory.ParseFilterProfilesIniContent(dialog.MacroEditorControl.Value)

                    For Each i In getDefaults()
                        Dim found = False

                        For Each i2 In filterProfiles
                            If i.Name = i2.Name Then
                                found = True
                            End If
                        Next

                        If Not found AndAlso {"Source", "Crop", "Resize"}.Contains(i.Name) Then
                            MsgWarn($"The category '{i.Name}' was recreated. A Source, Crop and Resize category is mandatory.")
                            filterProfiles.Add(i)
                        End If
                    Next

                    If p.Script.IsAviSynth Then
                        s.AviSynthProfiles = filterProfiles
                    Else
                        s.VapourSynthProfiles = filterProfiles
                    End If

                    g.SaveSettings()
                    g.MainForm.FiltersListView.RebuildMenu()
                Catch ex As Exception
                    g.ShowException(ex)
                End Try
            End If
        End Using
    End Sub

    Shared Function GetDefaultMainMenu() As CustomMenuItem
        Dim ret As New CustomMenuItem("Root")

        ret.Add("File|Open Video Source File(s)...", NameOf(ShowOpenSourceDialog), Keys.O Or Keys.Control, Symbol.Preview)
        ret.Add("File|Open Video Source File(s)...|Single File", NameOf(ShowOpenSourceSingleFileDialog))
        ret.Add("File|Open Video Source File(s)...|Multiple Files", NameOf(ShowOpenSourceMultipleFilesDialog))
        ret.Add("File|Open Video Source File(s)...|Batch Multiple Files", NameOf(ShowOpenSourceBatchFilesDialog))
        ret.Add("File|Open Video Source File(s)...|-")
        ret.Add("File|Open Video Source File(s)...|Merge Files", NameOf(ShowOpenSourceMergeFilesDialog))
        ret.Add("File|Open Video Source File(s)...|-")
        ret.Add("File|Open Video Source File(s)...|Blu-ray Image File", NameOf(ShowOpenSourceBlurayImageDialog))
        ret.Add("File|Open Video Source File(s)...|Blu-ray Folder", NameOf(ShowOpenSourceBlurayFolderDialog))
        ret.Add("File|Demux...", NameOf(g.DefaultCommands.ShowDemuxTool), Symbol.fa_sitemap)
        ret.Add("File|-")
        ret.Add("File|Video Comparison...", NameOf(ShowVideoComparison), Keys.F5, Symbol.VideoLegacy)
        ret.Add("File|-")
        ret.Add("File|Open Project...", NameOf(ShowFileBrowserToOpenProject), Symbol.OpenFile)
        ret.Add("File|Save Project", NameOf(SaveProject), Keys.S Or Keys.Control, Symbol.Save)
        ret.Add("File|Save Project As...", NameOf(SaveProjectAs), Keys.S Or Keys.Control Or Keys.Shift, Symbol.SaveAs)
        ret.Add("File|Save Project As Template...", NameOf(SaveProjectAsTemplate))
        ret.Add("File|Close Project", NameOf(CloseProject), Keys.W Or Keys.Control, Symbol.fa_close)
        ret.Add("File|-")
        ret.Add("File|Project Templates", NameOf(g.DefaultCommands.DynamicMenuItem), Symbol.Library, {DynamicMenuItemID.TemplateProjects})
        ret.Add("File|Recent Projects", NameOf(g.DefaultCommands.DynamicMenuItem), Symbol.History, {DynamicMenuItemID.RecentProjects})
        ret.Add("File|-")
        ret.Add("File|Launch New Instance", NameOf(StartNewInstance), Keys.N Or Keys.Control, Symbol.fa_clone)
        ret.Add("File|-")
        ret.Add("File|Exit", NameOf([Exit]), Keys.X Or Keys.Alt, Symbol.fa_window_close)

        ret.Add("Crop", NameOf(ShowCropDialog), Keys.F3, Symbol.fa_crop)
        ret.Add("Preview", NameOf(ShowPreview), Keys.F4, Symbol.fa_eye)

        ret.Add("Project|Add Hardcoded Subtitle...", NameOf(ShowHardcodedSubtitleDialogFromTempDir), Keys.Control Or Keys.H, Symbol.Subtitles)
        ret.Add("Project|Script Info...", NameOf(ShowScriptInfo), Keys.F2, Symbol.Info)
        ret.Add("Project|Advanced Script Info...", NameOf(ShowAdvancedScriptInfo), Keys.Control Or Keys.F2, Symbol.AddTo)
        ret.Add("Project|-")
        ret.Add("Project|Log File", NameOf(g.DefaultCommands.ShowLogFile), Keys.F8, Symbol.Page)
        ret.Add("Project|-")
        ret.Add("Project|Folders", Symbol.Library)
        ret.Add("Project|Folders|Source", NameOf(g.DefaultCommands.ExecuteCommandLine), {"""%source_dir%"""})
        ret.Add("Project|Folders|Target", NameOf(g.DefaultCommands.ExecuteCommandLine), {"""%target_dir%"""})
        ret.Add("Project|Folders|Temp", NameOf(g.DefaultCommands.ExecuteCommandLine), {"""%temp_dir%"""})
        ret.Add("Project|Folders|Working", NameOf(g.DefaultCommands.ExecuteCommandLine), {"""%working_dir%"""})
        ret.Add("Project|-")
        ret.Add("Project|Options", NameOf(ShowOptionsDialog), Keys.F9, Symbol.Project)

        ret.Add("Tools|Jobs...", NameOf(ShowJobsDialog), Keys.F6, Symbol.MultiSelectLegacy)
        ret.Add("Tools|Folders", Symbol.Library)
        ret.Add("Tools|Folders|Log Files", NameOf(g.DefaultCommands.ExecuteCommandLine), {$"""%settings_dir%{IO.Path.DirectorySeparatorChar}Log Files"""})
        ret.Add("Tools|Folders|Plugins", NameOf(g.DefaultCommands.ExecuteCommandLine), {"""%plugin_dir%"""})
        ret.Add("Tools|Folders|Programs", NameOf(g.DefaultCommands.ExecuteCommandLine), {"""%programs_dir%"""})
        ret.Add("Tools|Folders|Scripts", NameOf(g.DefaultCommands.ExecuteCommandLine), {"""%script_dir%"""})
        ret.Add("Tools|Folders|Settings", NameOf(g.DefaultCommands.ExecuteCommandLine), {"""%settings_dir%"""})
        ret.Add("Tools|Folders|Startup", NameOf(g.DefaultCommands.ExecuteCommandLine), {"""%startup_dir%"""})
        ret.Add("Tools|Folders|System", NameOf(g.DefaultCommands.ExecuteCommandLine), {"""%system_dir%"""})
        ret.Add("Tools|Folders|Templates", NameOf(g.DefaultCommands.ExecuteCommandLine), {$"""%settings_dir%{IO.Path.DirectorySeparatorChar}Templates"""})

        ret.Add("Tools|Scripts", NameOf(g.DefaultCommands.DynamicMenuItem), Symbol.Code, {DynamicMenuItemID.Scripts})

        ret.Add("Tools|Advanced", Symbol.More)
        ret.Add("Tools|Advanced|Event Commands...", NameOf(ShowEventCommandsDialog), Keys.Control Or Keys.E, Symbol.LightningBolt)
        ret.Add("Tools|Advanced|Macros...", NameOf(g.DefaultCommands.ShowMacrosDialog), Keys.Control Or Keys.M, Symbol.CalculatorPercentage)
        ret.Add("Tools|Advanced|Reset Settings...", NameOf(g.DefaultCommands.ResetSettings))
        ret.Add("Tools|Advanced|-")
        ret.Add("Tools|Advanced|Terminal", NameOf(g.DefaultCommands.OpenTerminal), Keys.Control Or Keys.T, Symbol.fa_terminal, {"", "%working_dir%"})
        ret.Add("Tools|Advanced|PowerShell Terminal", NameOf(g.DefaultCommands.OpenTerminal), Symbol.fa_terminal, {"pwsh -NoLogo", "%working_dir%"})

        ret.Add("Tools|Advanced|-")
        ret.Add("Tools|Advanced|Generate Wiki Content", NameOf(g.DefaultCommands.GenerateWikiContent))
        ret.Add("Tools|Advanced|Ingest HDR", NameOf(g.DefaultCommands.SaveMKVHDR))

        ret.Add("Tools|-")
        ret.Add("Tools|Edit Menu...", NameOf(ShowMainMenuEditor), Symbol.Settings)
        ret.Add("Tools|Settings...", NameOf(ShowSettingsDialog), Keys.F10, Symbol.fa_cogs, {""})

        ret.Add("Apps|Subtitles|Subtitle Edit", NameOf(g.DefaultCommands.StartTool), {"Subtitle Edit"})
        ret.Add("Apps|Subtitles|BDSup2Sub++", NameOf(g.DefaultCommands.StartTool), {"BDSup2Sub++"})
        ret.Add("Apps|Media Info|mkvinfo", NameOf(g.DefaultCommands.ShowMkvInfo))
        ret.Add("Apps|Media Info|MediaInfo File", NameOf(g.DefaultCommands.ShowMediaInfo))
        ret.Add("Apps|Media Info|MediaInfo Folder", NameOf(g.DefaultCommands.ShowMediaInfoFolderViewDialog))
        ret.Add("Apps|Players|mpv", NameOf(g.DefaultCommands.StartTool), {"mpv"})
        ret.Add("Apps|Indexing|D2V Witch", NameOf(g.DefaultCommands.StartTool), {"D2V Witch"})
        ret.Add("Apps|Thumbnails|StaxRipLinux Thumbnailer (using project options)", NameOf(g.DefaultCommands.ShowThumbnailerDialogAsync))
        ret.Add("Apps|Animation|Animated GIF", NameOf(g.DefaultCommands.SaveGIF))
        ret.Add("Apps|Animation|Animated PNG", NameOf(g.DefaultCommands.SavePNG))
        ret.Add("Apps|Other|MKVToolnix GUI", NameOf(g.DefaultCommands.StartTool), {"MKVToolnix GUI"})
        ret.Add("Apps|Other|chapterEditor", NameOf(g.DefaultCommands.StartTool), {"chapterEditor"})
        ret.Add("Apps|-")
        ret.Add("Apps|Manage...", NameOf(ShowAppsDialog), Keys.F11, Symbol.AllApps)

        If g.IsDevelopmentPC Then
            ret.Add("Apps|Test...", NameOf(g.DefaultCommands.Test), Keys.F12)
        End If

        ret.Add("Help|Apps", NameOf(g.DefaultCommands.DynamicMenuItem), Symbol.AllApps, {DynamicMenuItemID.HelpApplications})
        ret.Add("Help|-")
        ret.Add("Help|Website", NameOf(g.DefaultCommands.ExecuteCommandLine), Symbol.fa_github, {"https://github.com/staxrip/staxrip"})
        ret.Add("Help|Documentation", NameOf(g.DefaultCommands.ExecuteCommandLine), Keys.F1, Symbol.fa_book, {"https://github.com/staxrip/staxrip/tree/master/Docs/README.md"})
        ret.Add("Help|Changelog", NameOf(g.DefaultCommands.ExecuteCommandLine), Symbol.Bookmarks, {"https://github.com/staxrip/staxrip/blob/master/CHANGELOG.md"})
        ret.Add("Help|Support", NameOf(g.DefaultCommands.ExecuteCommandLine), Symbol.Heart, {"https://github.com/staxrip/staxrip?tab=readme-ov-file#contribution--support"})
        ret.Add("Help|-")
        ret.Add("Help|Report an issue", NameOf(g.DefaultCommands.ExecuteCommandLine), Symbol.fa_bug, {"https://github.com/staxrip/staxrip/issues/new/choose"})
        ret.Add("Help|-")
        ret.Add("Help|Discord Server", NameOf(g.DefaultCommands.ExecuteCommandLine), Symbol.People, {"https://discord.gg/uz8pVR79Bd"})
        ret.Add("Help|-")
        ret.Add("Help|Check for Updates", NameOf(g.DefaultCommands.CheckForUpdate), Symbol.fa_recycle)
        ret.Add("Help|-")
        ret.Add("Help|What's new...", NameOf(ShowChangelog), Symbol.fa_shield, {True})
        ret.Add("Help|Info...", NameOf(g.DefaultCommands.OpenHelpTopic), Symbol.Info, {"info"})

        Return ret
    End Function

    <Command("Shows a dialog, opening from the last source directory, to add a hardcoded subtitle.")>
    Sub ShowHardcodedSubtitleDialogFromLastSourceDir()
        ShowHardcodedSubtitleDialog(s.LastSourceDir)
    End Sub

    <Command("Shows a dialog, opening from the temp folder, to add a hardcoded subtitle.")>
    Sub ShowHardcodedSubtitleDialogFromTempDir()
        ShowHardcodedSubtitleDialog(p.TempDir)
    End Sub

    Sub ShowHardcodedSubtitleDialog(initDir As String)
        Using dialog As New OpenFileDialog
            dialog.SetFilter(FileTypes.SubtitleExludingContainers)
            dialog.SetInitDir(initDir)

            If dialog.ShowDialog = DialogResult.OK Then
                'AssRender for AviSynth+ doesn't render idx subtitles
                If dialog.FileName.Ext = "idx" AndAlso p.Script.IsVapourSynth Then
                    Dim subs = Subtitle.Create(dialog.FileName)

                    If subs.Count = 0 Then
                        MsgInfo("No subtitles found.")
                        Exit Sub
                    End If

                    Dim sb As New SelectionBox(Of Subtitle) With {
                        .Title = "Language",
                        .Text = "Please select a subtitle."
                    }

                    For Each i In subs
                        sb.AddItem(i.Language.EnglishName, i)
                    Next

                    If sb.Show = DialogResult.Cancel Then
                        Exit Sub
                    End If

                    Regex.Replace(dialog.FileName.ReadAllText, "langidx: \d+", "langidx: " +
                                  sb.SelectedValue.IndexIDX.ToString).WriteFileSystemEncoding(dialog.FileName)
                End If

                p.AddHardcodedSubtitleFilter(dialog.FileName, True)
            End If
        End Using
    End Sub

    Sub tbResize_MouseUp(sender As Object, e As MouseEventArgs) Handles tbResize.MouseUp
        Assistant()
    End Sub

    Sub tbResize_Scroll() Handles tbResize.Scroll
        SkipAssistant = True
        g.AddResizeFilter()
        tbTargetWidth.Text = CInt(p.SourceWidth \ 2 + tbResize.Value * Math.Max(TrackBarInterval, p.ForcedOutputMod)).ToString
        SetImageHeight()
        SkipAssistant = False
        Assistant(False)
    End Sub

    Sub SetImageHeight()
        Try
            Dim modval = p.ForcedOutputMod
            Dim ar As Double

            If Keyboard.Modifiers = Keys.Control Then
                Dim cropw = p.SourceWidth - p.CropLeft - p.CropRight
                Dim croph = p.SourceHeight - p.CropTop - p.CropBottom

                ar = cropw / croph
            Else
                ar = Calc.GetTargetDAR
            End If

            tbTargetHeight.Text = (CInt(p.TargetWidth / ar / modval) * modval).ToString()

            If p.TargetWidth = 1920 AndAlso p.TargetHeight = 1088 Then
                tbTargetHeight.Text = CStr(1080)
            End If
        Catch
        End Try
    End Sub

    Sub SetImageWidth()
        Try
            Dim modval = p.ForcedOutputMod
            tbTargetWidth.Text = (CInt(p.TargetHeight * Calc.GetTargetDAR / modval) * modval).ToString()
        Catch
        End Try
    End Sub

    Sub tbBitrate_KeyDown(sender As Object, e As KeyEventArgs) Handles tbBitrate.KeyDown
        If tbBitrate.ReadOnly Then
            Exit Sub
        End If

        If e.KeyData = Keys.Up Then
            e.Handled = True
            tbBitrate.Text = Math.Max(0, Calc.GetNextMod(tbBitrate.Text.ToInt, 50)).ToString
        ElseIf e.KeyData = Keys.Down Then
            e.Handled = True
            tbBitrate.Text = Math.Max(0, Calc.GetPreviousMod(tbBitrate.Text.ToInt, 50)).ToString
        End If
    End Sub

    Sub tbTargetSize_KeyDown(sender As Object, e As KeyEventArgs) Handles tbTargetSize.KeyDown
        If tbTargetSize.ReadOnly Then
            Exit Sub
        End If

        Dim modValue As Integer

        Select Case p.TargetSeconds
            Case Is > 50 * 60
                modValue = 50
            Case Is > 10 * 60
                modValue = 10
            Case Is > 60
                modValue = 5
            Case Else
                modValue = 1
        End Select

        If e.KeyData = Keys.Up Then
            e.Handled = True
            tbTargetSize.Text = Math.Max(0, Calc.GetNextMod(tbTargetSize.Text.ToInt, modValue)).ToString
        ElseIf e.KeyData = Keys.Down Then
            e.Handled = True
            tbTargetSize.Text = Math.Max(0, Calc.GetPreviousMod(tbTargetSize.Text.ToInt, modValue)).ToString
        End If
    End Sub

    Sub UpdateSizeOrBitrate()
        If p.VideoEncoder.QualityMode Then Return

        If p.BitrateIsFixed Then
            tbBitrate_TextChanged()
        Else
            tbSize_TextChanged()
        End If
    End Sub

    Sub tbSize_TextChanged() Handles tbTargetSize.TextChanged
        Try
            If tbTargetSize.TextBox.Focused Then
                p.BitrateIsFixed = False
            End If

            If Integer.TryParse(tbTargetSize.Text, Nothing) Then
                p.TargetSize = Math.Max(0, CInt(tbTargetSize.Text))
                BlockSize = True

                If Not BlockBitrate Then
                    tbBitrate.Text = CInt(Calc.GetVideoBitrate).ToString
                End If

                BlockSize = False
                Assistant()
            End If
        Catch
        End Try
    End Sub

    Sub tbBitrate_TextChanged() Handles tbBitrate.TextChanged
        Try
            If tbBitrate.TextBox.Focused Then
                p.BitrateIsFixed = True
            End If

            If Integer.TryParse(tbBitrate.Text, Nothing) Then
                p.VideoBitrate = Math.Max(0, CInt(tbBitrate.Text))
                p.VideoEncoder.Bitrate = p.VideoBitrate
                BlockBitrate = True

                If Not BlockSize Then
                    tbTargetSize.Text = $"{CInt(Calc.GetSizeInBytes \ PrefixedSize(2).Factor)}"
                End If

                p.VideoEncoder.UpdateTargetFile()
                BlockBitrate = False
                Assistant()
            End If
        Catch
        End Try
    End Sub

    Sub tbTargetWidth_TextChanged() Handles tbTargetWidth.TextChanged
        Try
            If Integer.TryParse(tbTargetWidth.Text, Nothing) Then
                p.TargetWidth = CInt(tbTargetWidth.Text)
                Assistant()
            End If
        Catch
        End Try
    End Sub

    Sub tbTargetHeight_TextChanged() Handles tbTargetHeight.TextChanged
        Try
            If Integer.TryParse(tbTargetHeight.Text, Nothing) Then
                p.TargetHeight = CInt(tbTargetHeight.Text)
                Assistant()
            End If
        Catch
        End Try
    End Sub

    Sub tbTargetWidth_KeyDown(sender As Object, e As KeyEventArgs) Handles tbTargetWidth.KeyDown
        If e.KeyData = Keys.Up Then
            e.Handled = True
            Dim modVal = p.ForcedOutputMod
            tbTargetWidth.Text = CInt(((p.TargetWidth + modVal) / modVal) * modVal).ToString()
        ElseIf e.KeyData = Keys.Down Then
            e.Handled = True
            Dim modVal = p.ForcedOutputMod
            tbTargetWidth.Text = CInt(((p.TargetWidth - modVal) / modVal) * modVal).ToString()
        End If
    End Sub

    Sub tbTargetHeight_KeyDown(sender As Object, e As KeyEventArgs) Handles tbTargetHeight.KeyDown
        If e.KeyData = Keys.Up Then
            e.Handled = True
            Dim modVal = p.ForcedOutputMod
            tbTargetHeight.Text = CInt(((p.TargetHeight + modVal) / modVal) * modVal).ToString()
        ElseIf e.KeyData = Keys.Down Then
            e.Handled = True
            Dim modVal = p.ForcedOutputMod
            tbTargetHeight.Text = CInt(((p.TargetHeight - modVal) / modVal) * modVal).ToString()
        End If
    End Sub

    Sub tbTargetWidth_MouseWheel(sender As Object, e As MouseEventArgs) Handles tbTargetWidth.MouseWheel
        If e.Delta.Height < 0 Then
            Dim modVal = p.ForcedOutputMod
            tbTargetWidth.Text = CInt(((p.TargetWidth - modVal) / modVal) * modVal).ToString()
        Else
            Dim modVal = p.ForcedOutputMod
            tbTargetWidth.Text = CInt(((p.TargetWidth + modVal) / modVal) * modVal).ToString()
        End If
    End Sub

    Sub tbTargetHeight_MouseWheel(sender As Object, e As MouseEventArgs) Handles tbTargetHeight.MouseWheel
        If e.Delta.Height < 0 Then
            Dim modVal = p.ForcedOutputMod
            tbTargetHeight.Text = CInt(((p.TargetHeight - modVal) / modVal) * modVal).ToString()
        Else
            Dim modVal = p.ForcedOutputMod
            tbTargetHeight.Text = CInt(((p.TargetHeight + modVal) / modVal) * modVal).ToString()
        End If
    End Sub

    Shared Function GetDefaultMenuSize() As CustomMenuItem
        Dim ret = New CustomMenuItem("Root")
        ret.Add("DVD/BD-5 (4482 MiB / 4700 MB)", NameOf(SetSize), {CInt(4700_000_000 \ PrefixedSize(2).Factor)})
        ret.Add("DVD-DL/BD-9 (8145 MiB / 8540 MB)", NameOf(SetSize), {CInt(8540_000_000 \ PrefixedSize(2).Factor)})
        ret.Add("-")
        ret.Add("BD (23842 MiB / 25000 MB)", NameOf(SetSize), {CInt(25000_000_000 \ PrefixedSize(2).Factor)})
        ret.Add("BD-DL (47684 MiB / 50000 MB)", NameOf(SetSize), {CInt(50000_000_000 \ PrefixedSize(2).Factor)})
        ret.Add("-")
        ret.Add("50%", NameOf(SetPercent), {50})
        ret.Add("60%", NameOf(SetPercent), {60})
        ret.Add("-")
        ret.Add("Edit Menu...", NameOf(ShowSizeMenuEditor))
        Return ret
    End Function

    <Command("Sets the target image size.")>
    Sub SetTargetImageSize(width As Integer, height As Integer)
        g.AddResizeFilter()

        If height = 0 AndAlso width > 0 Then
            tbTargetWidth.Text = width.ToString
            SetImageHeight()
        ElseIf width = 0 AndAlso height > 0 Then
            tbTargetHeight.Text = height.ToString
            SetImageWidth()
        Else
            tbTargetWidth.Text = width.ToString
            tbTargetHeight.Text = height.ToString
        End If

        Assistant()
    End Sub

    <Command("Sets the target image size by pixels (width x height).")>
    Sub SetTargetImageSizeByPixel(pixel As Integer)
        g.AddResizeFilter()

        Dim cropw = p.SourceWidth - p.CropLeft - p.CropRight
        Dim ar = Calc.GetTargetDAR()
        Dim w = Calc.FixMod16(CInt(Math.Sqrt(pixel * ar)))

        If w > cropw Then
            w = (cropw \ 16) * 16
        End If

        Dim h = Calc.FixMod16(CInt(w / ar))

        tbTargetWidth.Text = w.ToString()
        tbTargetHeight.Text = h.ToString()

        Assistant()
    End Sub

    <Command("Sets the target file size in MB.")>
    Sub SetSize(<DispName("Target File Size")> targetSize As Integer)
        tbTargetSize.Text = targetSize.ToString
        p.BitrateIsFixed = False
    End Sub

    <Command("Shows info about the output AviSynth/VapourSynth script.")>
    Sub ShowScriptInfo()
        g.ShowScriptInfo(p.Script)
    End Sub

    <Command("Shows advanced AviSynth/VapourSynth output script info using various tools.")>
    Sub ShowAdvancedScriptInfo()
        g.ShowAdvancedScriptInfo(p.Script)
    End Sub

    <Command("Sets the bitrate according to the compressibility.")>
    Sub SetPercent(<DispName("Percent Value")> value As Integer)
        tbTargetSize.Text = g.GetAutoSize(value).ToString
        p.BitrateIsFixed = False
    End Sub

    <Command("Sets the target video bitrate in Kbps.")>
    Sub SetBitrate(<DispName("Target Video Bitrate")> bitrate As Integer)
        tbBitrate.Text = bitrate.ToString
    End Sub

    <Command("Crops borders automatically.")>
    Sub StartAutoCrop()
        g.RunAutoCrop(Nothing)
        SetCropFilter()
        DisableCropFilter()
        Assistant()
    End Sub

    <Command("Crops borders automatically until the proper aspect ratio is found.")>
    Sub StartSmartCrop()
        g.SmartCrop()
        SetCropFilter()
        DisableCropFilter()
        Assistant()
    End Sub

    <Command("Menu editor for the size menu.")>
    Sub ShowSizeMenuEditor()
        s.CustomMenuSize = CustomSizeMenu.Edit()
    End Sub

    Sub UpdateSizeMenu()
        For Each i As MenuItemEx In CustomSizeMenu.MenuItems
            If i.CustomMenuItem.MethodName = "SetPercent" Then
                i.Enabled = p.Compressibility > 0
            End If
        Next
    End Sub

    Function GetScriptAsProfile() As Profile
        Dim sb As New SelectionBox(Of TargetVideoScript) With {
            .Title = "New Profile",
            .Text = "Please select a profile."
        }

        sb.AddItem("Current Project", p.Script)

        For Each i In VideoScript.GetDefaults
            sb.AddItem(i)
        Next

        If sb.Show = DialogResult.OK Then
            Return sb.SelectedValue
        End If
    End Function

    Sub LoadFilterSetup(profileInterface As Profile)
        Dim profile = DirectCast(ObjectHelp.GetCopy(profileInterface), TargetVideoScript)

        If profile.Engine = ScriptEngine.AviSynth OrElse (Package.Python.VerifyOK(True) AndAlso Package.VapourSynth.VerifyOK(True) AndAlso Package.vspipe.VerifyOK(True)) Then
            Dim currentSetup = p.Script

            Try
                p.Script = profile
                SetCropFilter()
                PreviewScript = Nothing
                ModifyFilters()
                FiltersListView.OnChanged()
                Assistant()
            Catch ex As Exception
                p.Script = currentSetup
                PreviewScript = Nothing
                ModifyFilters()
                FiltersListView.OnChanged()
                Assistant()
            End Try
        End If

        FiltersListView.RebuildMenu()
    End Sub

    Function ParseCommandLine(commandLine As String, Optional delimiter As Char = " "c) As String()
        If String.IsNullOrWhiteSpace(commandLine) Then Return New List(Of String)().ToArray()

        Dim args = New List(Of String)
        Dim sb = New StringBuilder()
        Dim insideQuote = False
        Dim insideCurlyBracketsLevel = 0
        Dim insideRoundBracketsLevel = 0
        Dim insideSquareBracketsLevel = 0

        For i = 0 To commandLine.Length - 1
            Dim c = commandLine.Chars(i)

            If c = """" Then
                sb.Append(c)

                Dim lc = If(i > 0, commandLine.Chars(i - 1), "_"c)
                If lc <> "`" Then
                    insideQuote = Not insideQuote
                End If
            ElseIf c = "{" Then
                sb.Append(c)
                insideCurlyBracketsLevel += 1
            ElseIf c = "}" Then
                sb.Append(c)
                If insideCurlyBracketsLevel > 0 Then insideCurlyBracketsLevel -= 1
            ElseIf c = "(" Then
                sb.Append(c)
                insideRoundBracketsLevel += 1
            ElseIf c = ")" Then
                sb.Append(c)
                If insideRoundBracketsLevel > 0 Then insideRoundBracketsLevel -= 1
            ElseIf c = "[" Then
                sb.Append(c)
                insideSquareBracketsLevel += 1
            ElseIf c = "]" Then
                sb.Append(c)
                If insideSquareBracketsLevel > 0 Then insideSquareBracketsLevel -= 1
            ElseIf c = delimiter Then
                If insideQuote Then
                    sb.Append(c)
                ElseIf insideCurlyBracketsLevel > 0 Then
                    sb.Append(c)
                ElseIf insideRoundBracketsLevel > 0 Then
                    sb.Append(c)
                ElseIf insideSquareBracketsLevel > 0 Then
                    sb.Append(c)
                Else
                    Dim str = sb.ToString()
                    If Not String.IsNullOrWhiteSpace(str) Then
                        args.Add(str)
                    End If
                    sb = New StringBuilder()
                End If
            Else
                sb.Append(c)
            End If
        Next

        If sb.Length > 0 Then args.Add(sb.ToString())

        Return args.ToArray()
    End Function

    Sub ProcessCommandLine(commandLine As String)
        If String.IsNullOrWhiteSpace(commandLine) Then Exit Sub

        Dim args = ParseCommandLine(commandLine)
        If args.Any() Then
            Package.LoadConfAll()
        Else
            Exit Sub
        End If

        Dim files As New List(Of String)
        Dim showTemplateSelection = (s.ShowTemplateSelection And (ShowTemplateSelectionMode.Always Or ShowTemplateSelectionMode.CommandLine)) <> 0
        Dim forcedTemplateLoading = args.Where(Function(s) s.ToLowerInvariant().TrimQuotes().StartsWith("-" & NameOf(LoadTemplate).ToLowerInvariant())).Any()

        For Each arg In args.Skip(1)
            Try
                If Not arg.FileExists() AndAlso files.Count > 0 Then
                    Dim files2 As New List(Of String)(files)
                    Refresh()
                    If Not showTemplateSelection AndAlso Not forcedTemplateLoading Then OpenProject(g.LastModifiedTemplate)
                    OpenAnyFile(files2, showTemplateSelection AndAlso Not forcedTemplateLoading)
                    files.Clear()
                End If

                If arg.FileExists() Then
                    files.Add(arg)
                Else
                    If Not CommandManager.ProcessCommandLineArgument(arg) Then
                        Throw New Exception
                    End If
                End If
            Catch ex As Exception
                MsgWarn("Error parsing argument:" + BR2 + arg + BR2 + ex.Message)
            End Try
        Next

        If files.Count > 0 Then
            Refresh()
            If Not showTemplateSelection AndAlso Not forcedTemplateLoading Then OpenProject(g.LastModifiedTemplate)
            OpenAnyFile(files, showTemplateSelection AndAlso Not forcedTemplateLoading)
        End If
    End Sub

    <Command("Sets the project option 'Hide dialogs asking to demux, source filter etc.'")>
    Sub SetHideDialogsOption(hide As Boolean)
        p.NoDialogs = hide
    End Sub

    <Command("Standby PC.")>
    Sub Standby()
        g.ShutdownPC(ShutdownMode.Standby)
    End Sub

    <Command("Shut down PC.")>
    Sub Shutdown()
        g.ShutdownPC(ShutdownMode.Shutdown)
    End Sub

    Sub SetEncoderControl(c As Control)
        Dim old = pnEncoder.Content
        pnEncoder.Content = c

        If old IsNot Nothing AndAlso old IsNot c Then
            old.Dispose()
        End If
    End Sub

    Sub UpdateEncoderButtons(encoder As VideoEncoder)
        bnEncoder.Tag = encoder.Name
        bnEncoder.Text = g.ConvertPath(encoder.Name, 45).Shorten(40)
        bnEncoderOptions.Visible = TypeOf encoder IsNot NullEncoder

        Dim ext = encoder.Muxer.OutputExt.ToUpperInvariant()
        bnMuxer.Tag = encoder.Muxer.Name
        bnMuxer.Text = If(String.IsNullOrWhiteSpace(ext), "[ ? ]", ext)
        bnMuxerOptions.Visible = encoder.Muxer.CanEdit
    End Sub

    Sub UpdateEncoderStateRelatedControls()
        TargetSizeRow.Visible = Not p.VideoEncoder.QualityMode
    End Sub

    <Command("Dialog to open a single file source.")>
    Sub ShowOpenSourceSingleFileDialog()
        SetLastModifiedTemplate()
        If p.SourceFile <> "" AndAlso IsSaveCanceled() Then Exit Sub

        Using dialog As New OpenFileDialog
            dialog.SetFilter(FileTypes.Video.Concat(FileTypes.Image))
            dialog.SetInitDir(s.LastSourceDir)

            If dialog.ShowDialog() = DialogResult.OK Then
                Dim showTemplateSelection = (s.ShowTemplateSelection And (ShowTemplateSelectionMode.Always Or ShowTemplateSelectionMode.OpeningMenu)) <> 0
                Dim showTemplateSelectionTimeout = If(s.ShowTemplateSelection <> ShowTemplateSelectionMode.Never, s.ShowTemplateSelectionTimeout, 0)

                If showTemplateSelection Then
                    If LoadTemplateWithSelectionDialog(dialog.FileNames, showTemplateSelectionTimeout) Then
                        OpenVideoSourceFiles(dialog.FileNames)
                    Else
                        Exit Sub
                    End If
                Else
                    OpenProject(g.LastModifiedTemplate)
                    OpenVideoSourceFiles(dialog.FileNames)
                End If
            End If
        End Using
    End Sub

    <Command("Dialog to open multiple file sources.")>
    Sub ShowOpenSourceMultipleFilesDialog()
        SetLastModifiedTemplate()
        If p.SourceFile <> "" AndAlso IsSaveCanceled() Then Exit Sub

        Using dialog As New OpenFileDialog
            dialog.SetFilter(FileTypes.Video)
            dialog.SetInitDir(s.LastSourceDir)
            dialog.Multiselect = True

            If dialog.ShowDialog() = DialogResult.OK Then
                Dim showTemplateSelection = (s.ShowTemplateSelection And (ShowTemplateSelectionMode.Always Or ShowTemplateSelectionMode.OpeningMenu)) <> 0
                Dim showTemplateSelectionTimeout = If(s.ShowTemplateSelection <> ShowTemplateSelectionMode.Never, s.ShowTemplateSelectionTimeout, 0)

                If showTemplateSelection Then
                    If LoadTemplateWithSelectionDialog(dialog.FileNames, showTemplateSelectionTimeout) Then
                        OpenVideoSourceFiles(dialog.FileNames)
                    Else
                        Exit Sub
                    End If
                Else
                    OpenProject(g.LastModifiedTemplate)
                    OpenVideoSourceFiles(dialog.FileNames)
                End If
            End If
        End Using
    End Sub

    <Command("Dialog to open a Blu-ray folder source.")>
    Sub ShowOpenSourceBlurayFolderDialog()
        SetLastModifiedTemplate()
        If p.SourceFile <> "" AndAlso IsSaveCanceled() Then Exit Sub

        Using dialog As New FolderBrowserDialog
            dialog.Description = "Please select a Blu-ray source folder."
            dialog.SetSelectedPath(s.Storage.GetString("last blu-ray source folder"))
            dialog.ShowNewFolderButton = False

            If dialog.ShowDialog = DialogResult.OK Then
                s.Storage.SetString("last blu-ray source folder", dialog.SelectedPath)

                Dim srcPath = dialog.SelectedPath.FixDir
                Dim showTemplateSelection = (s.ShowTemplateSelection And (ShowTemplateSelectionMode.Always Or ShowTemplateSelectionMode.OpeningMenu)) <> 0
                Dim showTemplateSelectionTimeout = If(s.ShowTemplateSelection <> ShowTemplateSelectionMode.Never, s.ShowTemplateSelectionTimeout, 0)

                If showTemplateSelection Then
                    If LoadTemplateWithSelectionDialog(srcPath, showTemplateSelectionTimeout) Then
                        OpenBlurayFolder(srcPath)
                    Else
                        Exit Sub
                    End If
                Else
                    OpenProject(g.LastModifiedTemplate)
                    OpenBlurayFolder(srcPath)
                End If
            End If
        End Using
    End Sub

    <Command("Dialog to open a Blu-ray ISO image.")>
    Sub ShowOpenSourceBlurayImageDialog()
        SetLastModifiedTemplate()
        If p.SourceFile <> "" AndAlso IsSaveCanceled() Then Exit Sub

        Using dialog As New OpenFileDialog
            dialog.SetFilter(FileTypes.IsoImage)
            dialog.SetInitDir(s.LastSourceDir)

            If dialog.ShowDialog = DialogResult.OK Then
                Dim showTemplateSelection = (s.ShowTemplateSelection And (ShowTemplateSelectionMode.Always Or ShowTemplateSelectionMode.OpeningMenu)) <> 0
                Dim showTemplateSelectionTimeout = If(s.ShowTemplateSelection <> ShowTemplateSelectionMode.Never, s.ShowTemplateSelectionTimeout, 0)

                If showTemplateSelection Then
                    If LoadTemplateWithSelectionDialog(dialog.FileName, showTemplateSelectionTimeout) Then
                        OpenBlurayImage(dialog.FileName)
                    Else
                        Exit Sub
                    End If
                Else
                    OpenProject(g.LastModifiedTemplate)
                    OpenBlurayImage(dialog.FileName)
                End If
            End If
        End Using
    End Sub

    <Command("Dialog to open a merged files source.")>
    Sub ShowOpenSourceMergeFilesDialog()
        SetLastModifiedTemplate()
        If p.SourceFile <> "" AndAlso IsSaveCanceled() Then Exit Sub

        Using form As New SourceFilesForm()
            form.Text = $"Merge - {g.DefaultCommands.GetApplicationDetails()}"
            form.IsMerge = True

            If form.ShowDialog() = DialogResult.OK AndAlso form.lb.Items.Count > 0 Then
                Dim files = form.GetFiles

                Select Case files(0).Ext
                    Case "mpg", "vob"
                        OpenVideoSourceFiles(files)
                    Case Else
                        Using proc As New Proc
                            proc.Header = "Merge source files"

                            For Each i In files
                                Log.WriteLine(MediaInfo.GetSummary(i) + "---------------------------------------------------------" + BR2)
                            Next

                            proc.Encoding = Encoding.UTF8
                            proc.Package = Package.mkvmerge
                            Dim outFile = files(0).DirAndBase + "_merged.mkv"
                            proc.Arguments = "-o " + outFile.Escape + " " + files.Select(Function(i) i.Escape).Join(" + ")

                            Try
                                proc.Start()
                            Catch ex As Exception
                                g.ShowException(ex)
                                MsgInfo("Manual Merging", "Please merge the files manually with an appropriate tool or visit the support forum.")
                                Throw New AbortException
                            End Try

                            If Not g.FileExists(outFile) Then
                                Log.Write("Error merged output file is missing", outFile)
                                Exit Sub
                            Else
                                Dim showTemplateSelection = (s.ShowTemplateSelection And (ShowTemplateSelectionMode.Always Or ShowTemplateSelectionMode.OpeningMenu)) <> 0
                                Dim showTemplateSelectionTimeout = If(s.ShowTemplateSelection <> ShowTemplateSelectionMode.Never, s.ShowTemplateSelectionTimeout, 0)

                                If showTemplateSelection Then
                                    If LoadTemplateWithSelectionDialog(outFile, showTemplateSelectionTimeout) Then
                                        OpenVideoSourceFile(outFile)
                                    Else
                                        Exit Sub
                                    End If
                                Else
                                    OpenProject(g.LastModifiedTemplate)
                                    OpenVideoSourceFile(outFile)
                                End If
                            End If
                        End Using
                End Select
            End If
        End Using
    End Sub

    <Command("Dialog to open a file batch source.")>
    Sub ShowOpenSourceBatchFilesDialog()
        SetLastModifiedTemplate()
        If p.SourceFile <> "" AndAlso IsSaveCanceled() Then Exit Sub
        If AbortDueToLowDiskSpace() Then Exit Sub

        Using form As New SourceFilesForm()
            form.Text = $"File Batch - {g.DefaultCommands.GetApplicationDetails()}"

            If p.DefaultTargetName = "%source_dir_name%" Then
                p.DefaultTargetName = "%source_name%"
            End If

            If form.ShowDialog() = DialogResult.OK AndAlso form.lb.Items.Count > 0 Then
                Dim showTemplateSelection = (s.ShowTemplateSelection And (ShowTemplateSelectionMode.Always Or ShowTemplateSelectionMode.OpeningMenu)) <> 0
                Dim showTemplateSelectionTimeout = If(s.ShowTemplateSelection <> ShowTemplateSelectionMode.Never, s.ShowTemplateSelectionTimeout, 0)

                If showTemplateSelection Then
                    If LoadTemplateWithSelectionDialog(form.GetFiles(), showTemplateSelectionTimeout) Then
                        For Each filepath In form.GetFiles()
                            AddBatchJob(filepath)
                        Next
                    Else
                        Exit Sub
                    End If
                Else
                    OpenProject(g.LastModifiedTemplate, "", False)

                    For Each filepath In form.GetFiles
                        AddBatchJob(filepath)
                    Next
                End If

                ShowJobsDialog()
            End If
        End Using
    End Sub

    <Command("Dialog to open source files.")>
    Sub ShowOpenSourceDialog()
        Using td As New TaskDialog(Of String)
            td.Title = "Select a method for opening a source:"
            td.AddCommand("Single File")
            td.AddCommand("Multiple Files")
            td.AddCommand("Merge Files")
            td.AddCommand("File Batch")
            td.AddCommand("Blu-ray Folder")
            td.AddCommand("Blu-ray Image File")

            Select Case td.Show
                Case "Single File"
                    ShowOpenSourceSingleFileDialog()
                Case "Multiple Files"
                    ShowOpenSourceMultipleFilesDialog()
                Case "Merge Files"
                    ShowOpenSourceMergeFilesDialog()
                Case "File Batch"
                    ShowOpenSourceBatchFilesDialog()
                Case "Blu-ray Folder"
                    ShowOpenSourceBlurayFolderDialog()
                Case "Blu-ray Image File"
                    ShowOpenSourceBlurayImageDialog()
            End Select
        End Using
    End Sub

    Sub OpenEac3toDemuxForm(playlistFolder As String, playlistID As Integer)
        Using form As New eac3toForm(p)
            form.PlaylistFolder = playlistFolder
            form.PlaylistID = playlistID

            Dim workDir = playlistFolder.Parent.Parent
            Dim title = InputBox.Show("Enter a short title used as filename", playlistFolder.Parent.Parent.DirName)
            If title = "" Then Exit Sub

            If p.TempDir <> "" Then
                workDir = p.TempDir.Replace("%source_name%", title).FixDir
            Else
                workDir = Path.Combine(workDir, title)
            End If

            If Not g.IsFixedDrive(workDir) Then
                Using dialog As New FolderBrowserDialog
                    dialog.Description = "Please select a folder for temporary files on a fixed local drive."
                    dialog.SetSelectedPath(s.Storage.GetString("last blu-ray target folder").Parent)

                    If dialog.ShowDialog = DialogResult.OK Then
                        workDir = dialog.SelectedPath
                    Else
                        Exit Sub
                    End If
                End Using
            End If

            form.teTempDir.Text = workDir

            If form.ShowDialog() = DialogResult.OK Then
                If Not Directory.Exists(form.OutputFolder) Then
                    Try
                        Directory.CreateDirectory(form.OutputFolder)
                    Catch ex As Exception
                        MsgError("The temp folder could not be created." + BR2 + form.OutputFolder)
                        Exit Sub
                    End Try
                End If

                If Not g.IsFixedDrive(form.OutputFolder) Then
                    MsgError("Only fixed local drives are supported as temp dir.")
                    Exit Sub
                End If

                Try
                    Dim di As New DriveInfo(form.OutputFolder)

                    If di.AvailableFreeSpace / PrefixedSize(3).Factor < s.MinimumDiskSpace Then
                        Using td As New TaskDialog(Of String)
                            td.Title = "Low Disk Space"
                            td.Content = $"The temp folder {form.OutputFolder} has only " +
                                         $"{(di.AvailableFreeSpace / PrefixedSize(3).Factor):f2} {PrefixedSize(3).Unit} free disk space."
                            td.Icon = TaskIcon.Warning
                            td.AddButton("Continue")
                            td.AddButton("Abort")

                            If td.Show <> "Continue" Then
                                Exit Sub
                            End If
                        End Using
                    End If

                    Using pr As New Proc
                        pr.Header = "Demux M2TS"
                        pr.TrimChars = {"-"c, " "c}
                        pr.SkipStrings = {"analyze: ", "process: "}
                        pr.Package = Package.eac3to
                        Dim outFiles As New List(Of String)
                        pr.Process.StartInfo.Arguments = form.GetArgs(WineHelp.ToWinePath(playlistFolder).Escape + " " & playlistID & ")", title, outFiles) + eac3toForm.GetPlaylistOptions(p)
                        pr.OutputFiles = outFiles

                        Try
                            pr.Start()
                        Catch ex As AbortException
                            Exit Sub
                        Catch ex As Exception
                            g.ShowException(ex)
                            Exit Sub
                        End Try
                    End Using

                    s.Storage.SetString("last blu-ray target folder", form.OutputFolder)
                    Dim fs = Path.Combine(form.OutputFolder, title + "." + form.cbVideoOutput.Text.ToLowerInvariant)

                    If File.Exists(fs) Then
                        p.TempDir = form.OutputFolder
                        OpenVideoSourceFile(fs, False)
                    End If
                Catch
                End Try
            End If
        End Using
    End Sub

    Sub tbSource_DoubleClick() Handles tbSourceFile.DoubleClick
        SetLastModifiedTemplate()

        Using dialog As New OpenFileDialog
            dialog.SetFilter(FileTypes.Video)
            dialog.Multiselect = True
            dialog.SetInitDir(s.LastSourceDir)

            If dialog.ShowDialog() = DialogResult.OK Then
                Refresh()

                Dim files = dialog.FileNames
                files.Sort()

                If Not files.NothingOrEmpty Then
                    Dim showTemplateSelection = (s.ShowTemplateSelection And (ShowTemplateSelectionMode.Always Or ShowTemplateSelectionMode.OpeningMenu)) <> 0

                    If p.SourceFile <> "" AndAlso IsSaveCanceled() Then Exit Sub
                    If Not showTemplateSelection Then OpenProject(g.LastModifiedTemplate)
                    BeginInvoke(Sub() OpenAnyFile(files.ToList, showTemplateSelection))
                End If
            End If
        End Using
    End Sub

    Function GetNewAudioProfile(currentProfile As AudioProfile) As AudioProfile
        Dim sb As New SelectionBox(Of AudioProfile) With {
            .Title = "New Profile",
            .Text = "Please select a profile."
        }

        If currentProfile IsNot Nothing Then
            sb.AddItem("Current Project", currentProfile)
        End If

        For Each i In AudioProfile.GetDefaults()
            sb.AddItem(i)
        Next

        If sb.Show = DialogResult.OK Then
            Return sb.SelectedValue
        End If
    End Function

    Sub UpdateTargetParameters(proj As Project)
        If proj Is Nothing Then Exit Sub
        If proj.Script Is Nothing Then Exit Sub

        Dim info = proj.Script.GetInfo()
        Dim frameCount = 0
        Dim frameRate = 0D
        Dim seconds = 0

        If info.FrameRate > 0 Then
            frameCount = info.FrameCount
            frameRate = info.FrameRate
            seconds = CInt(frameCount / frameRate)
        Else
        End If

        UpdateTargetParameters(seconds, frameCount, frameRate)
    End Sub

    Sub UpdateTargetParameters(seconds As Integer, frames As Integer, frameRate As Double)
        p.TargetSeconds = seconds
        p.TargetFrames = frames
        p.TargetFrameRate = frameRate

        UpdateSizeOrBitrate()
    End Sub

    Sub pEncoder_MouseLeave() Handles pnEncoder.MouseLeave
        Assistant()
    End Sub

    Sub ShowAudioTip(ap As AudioProfile)
        If TypeOf ap Is GUIAudioProfile Then
            Dim gap = DirectCast(ap, GUIAudioProfile)

            If ap.Decoder = AudioDecoderMode.ffmpeg AndAlso gap.GetEncoder = GuiAudioEncoder.ffmpeg Then
                Documentation.ShowTip(
                    "Please note that defining an audio decoder will always result in a separate " +
                    "decoding step, which is usually not necessary if the decoder and encoder are identical, " +
                    "it's a waste of time and disk space.")
            End If
        End If
    End Sub

    <Command("Dialog to manage audio profiles.")>
    Sub ShowAudioProfilesDialog(<DispName("Track Number")> number As Integer)
        g.LoadAudioProfiles()

        Dim form = New ProfilesForm("Audio Profiles", s.AudioProfiles, Sub(profile) g.LoadAudioProfile(profile, number), Function() GetNewAudioProfile(p.AudioTracks(number).AudioProfile), AddressOf AudioProfile.GetDefaults)

        If form.ShowDialog() = DialogResult.OK Then
            g.SaveAudioProfiles()
        End If
        form.Dispose()

        UpdateAudioMenus()
        PopulateProfileMenu(DynamicMenuItemID.AudioProfiles)
    End Sub

    Sub UpdateAudioMenus()
        If p.AudioTracksAvailable < 1 Then Exit Sub
        If p.AudioTracks Is Nothing Then Exit Sub
        Dim nameControls = p.AudioTracks.Select(Function(x) x.NameLabel).ToList
        If nameControls.Count < 1 Then Exit Sub

        For i = 0 To nameControls.Count - 1
            Dim index = i
            Dim control = nameControls(index)

            control.ContextMenuStripEx = If(control.ContextMenuStripEx, New ContextMenuStripEx)

            control.ContextMenuStripEx.Items.ClearAndDisplose()

            g.PopulateProfileMenu(control.ContextMenuStripEx.Items, s.AudioProfiles, Sub() ShowAudioProfilesDialog(index), Sub(profile) g.LoadAudioProfile(profile, index))
        Next
    End Sub

    Sub AviSynthListView_ScriptChanged() Handles FiltersListView.Changed
        If Not IsLoading AndAlso Not FiltersListView.IsLoading Then
            If g.IsValidSource(False) Then
                UpdateSourceParameters()
                UpdateTargetParameters(p)
                p.VideoEncoder?.UpdateTargetFile()
            End If

            Assistant()
        End If
    End Sub

    Sub UpdateFilters()
        FiltersListView.Load()

        If g.IsValidSource(False) Then
            UpdateTargetParameters(p)
        End If
    End Sub

    Sub AviSynthListView_DoubleClick() Handles FiltersListView.DoubleClick
        FiltersListView.ShowCodeEditor()
    End Sub

    Sub bnFilters_Click() Handles bnFilters.Click
        FiltersListView.ContextMenuStrip.Show(bnFilters, 0, bnFilters.Height)
    End Sub

    Sub bnEncoder_Click() Handles bnEncoder.Click
        EncoderMenu.Items.ClearAndDisplose
        g.PopulateProfileMenu(EncoderMenu.Items, s.VideoEncoderProfiles, AddressOf ShowEncoderProfilesDialog, AddressOf g.LoadVideoEncoder)
        EncoderMenu.Show(bnEncoder, 0, bnEncoder.Height)
    End Sub

    Sub bnEncoderOptions_Click() Handles bnEncoderOptions.Click
        p.VideoEncoder.ShowConfigDialog()
    End Sub

    Sub llSize_Click() Handles blFilesize.Click
        UpdateSizeMenu()
        SizeContextMenuStrip.Show(blFilesize, 0, blFilesize.Height)
    End Sub

    Sub bnMuxer_Click() Handles bnMuxer.Click
        ContainerMenu.Items.ClearAndDisplose
        g.PopulateProfileMenu(ContainerMenu.Items, s.MuxerProfiles, AddressOf ShowMuxerProfilesDialog, AddressOf p.VideoEncoder.LoadMuxer)
        ContainerMenu.Show(bnMuxer, 0, bnMuxer.Height)
    End Sub

    Sub bnMuxerOptions_Click() Handles bnMuxerOptions.Click
        p.VideoEncoder.OpenMuxerConfigDialog()
    End Sub

    Sub bnPreview_Click() Handles bnPreview.Click
        ShowPreview()
    End Sub

    Sub bnCrop_Click() Handles bnCrop.Click
        ShowCropDialog()
    End Sub

    Sub bnJobs_Click() Handles bnJobs.Click
        ShowJobsDialog()
    End Sub

    'the buttons only show the menus, menu holders keep them from opening the menus by themselves
    Private ReadOnly ResizeMenuHolder As New MenuHolder
    Private ReadOnly SourceParMenuHolder As New MenuHolder
    Private ReadOnly SourceDarMenuHolder As New MenuHolder
    Private ReadOnly TargetParMenuHolder As New MenuHolder
    Private ReadOnly TargetDarMenuHolder As New MenuHolder

    Sub bnResize_Click() Handles bnResize.Click
        Dim cms = TextCustomMenu.GetMenu(s.TargetImageSizeMenu, ResizeMenuHolder, AddressOf TargetImageMenuClick)
        cms.Add("-")
        cms.Add("Image Options...", Sub() ShowOptionsDialog("Image"))
        cms.Add("Edit Menu...", Sub() s.TargetImageSizeMenu = TextCustomMenu.EditMenu(s.TargetImageSizeMenu, ApplicationSettings.GetDefaultTargetImageSizeMenu, Me))
        cms.Show(bnResize, 0, bnResize.Height)
    End Sub

    Sub blSourceParText_Click(sender As Object, e As EventArgs) Handles blSourceParText.Click
        Dim menu = TextCustomMenu.GetMenu(s.ParMenu, SourceParMenuHolder, AddressOf SourceParMenuClick)
        menu.Add("-")
        menu.Items.Add(New MenuItemEx("Edit Menu...", Sub() s.ParMenu = TextCustomMenu.EditMenu(s.ParMenu, ApplicationSettings.GetParMenu, Me)))
        menu.Show(blSourceParText, 0, blSourceParText.Height)
    End Sub

    Sub blSourceDarText_Click(sender As Object, e As EventArgs) Handles blSourceDarText.Click
        Dim menu = TextCustomMenu.GetMenu(s.DarMenu, SourceDarMenuHolder, AddressOf SourceDarMenuClick)
        menu.Add("-")
        menu.Items.Add(New MenuItemEx("Edit Menu...", Sub() s.DarMenu = TextCustomMenu.EditMenu(s.DarMenu, ApplicationSettings.GetDarMenu, Me)))
        menu.Show(blSourceDarText, 0, blSourceDarText.Height)
    End Sub

    Sub blTargetDarText_Click(sender As Object, e As EventArgs) Handles blTargetDarText.Click
        Dim menu = TextCustomMenu.GetMenu(s.DarMenu, TargetDarMenuHolder, AddressOf TargetDarMenuClick)
        menu.Add("-")
        menu.Items.Add(New MenuItemEx("Edit Menu...", Sub() s.DarMenu = TextCustomMenu.EditMenu(s.DarMenu, ApplicationSettings.GetDarMenu, Me)))
        menu.Show(blTargetDarText, 0, blTargetDarText.Height)
    End Sub

    Sub blTargetParText_Click(sender As Object, e As EventArgs) Handles blTargetParText.Click
        Dim menu = TextCustomMenu.GetMenu(s.ParMenu, TargetParMenuHolder, AddressOf TargetParMenuClick)
        menu.Add("-")
        menu.Items.Add(New MenuItemEx("Edit Menu...", Sub() s.ParMenu = TextCustomMenu.EditMenu(s.ParMenu, ApplicationSettings.GetParMenu, Me)))
        menu.Show(blTargetParText, 0, blTargetParText.Height)
    End Sub

    Sub SourceDarMenuClick(value As String)
        value = Macro.Expand(value)
        Dim tup = Macro.ExpandGUI(value)

        If tup.Cancel Then
            Exit Sub
        End If

        p.CustomSourcePAR = ""
        p.CustomSourceDAR = tup.Value

        If Calc.ParseCustomAR(tup.Value, 0, 0).X = 0 Then
            p.CustomSourceDAR = ""
        End If

        Assistant()
    End Sub

    Sub SourceParMenuClick(value As String)
        value = Macro.Expand(value)
        Dim tup = Macro.ExpandGUI(value)

        If tup.Cancel Then
            Exit Sub
        End If

        p.CustomSourcePAR = tup.Value
        p.CustomSourceDAR = ""

        If value <> "force" AndAlso Calc.ParseCustomAR(tup.Value, 0, 0).X = 0 Then
            p.CustomSourcePAR = ""
        End If

        Assistant()
    End Sub

    Sub TargetDarMenuClick(value As String)
        value = Macro.Expand(value)
        Dim tup = Macro.ExpandGUI(value)

        If tup.Cancel Then
            Exit Sub
        End If

        p.CustomTargetPAR = ""
        p.CustomTargetDAR = tup.Value

        If Calc.ParseCustomAR(tup.Value, 0, 0).X = 0 Then
            p.CustomTargetDAR = ""
        End If

        Assistant()
    End Sub

    Sub TargetParMenuClick(value As String)
        value = Macro.Expand(value)
        Dim tup = Macro.ExpandGUI(value)

        If tup.Cancel Then
            Exit Sub
        End If

        p.CustomTargetPAR = tup.Value
        p.CustomTargetDAR = ""

        If value <> "force" AndAlso Calc.ParseCustomAR(tup.Value, 0, 0).X = 0 Then
            p.CustomTargetPAR = ""
        End If

        Assistant()
    End Sub

    Sub TargetImageMenuClick(value As String)
        g.EnableFilter("Resize")

        value = Macro.Expand(value)
        Dim tup = Macro.ExpandGUI(value)

        If tup.Cancel Then
            Exit Sub
        End If

        value = tup.Value

        If value.IsInt Then
            SetTargetImageSizeByPixel(CInt(value))
            Exit Sub
        End If

        If value?.Contains("x") Then
            Dim a = value.SplitNoEmptyAndWhiteSpace("x")

            If a.Length = 2 AndAlso a(0).IsInt AndAlso a(1).IsInt Then
                SetTargetImageSize(a(0).ToInt, a(1).ToInt)
                Exit Sub
            End If
        End If

        MsgWarn("Invalid format")
    End Sub

    Sub SetTip()
        With TipProvider
            .SetTip("Target Display Aspect Ratio", lDAR, blTargetDarText)
            .SetTip("Target Pixel Aspect Ratio", lPAR, blTargetParText)
            .SetTip("Target Storage Aspect Ratio", lSAR, lSarText)
            .SetTip("Imagesize in pixel (width x height)", lPixel, lPixelText)
            .SetTip("Zoom factor between source and target image size", lZoom, lZoomText)
            .SetTip("Press Ctrl while using the slider to resize anamorphic.", tbResize)
            .SetTip("Aspect Ratio Error", lAspectRatioError, lAspectRatioErrorText)
            .SetTip("Cropped image size", lCrop, lCropText)
            .SetTip("Cropped sides in format: Left, Right / Top, Bottom", blCropText, lCrop2)
            .SetTip("Source Display Aspect Ratio", lSourceDar, blSourceDarText)
            .SetTip("Source Pixel Aspect Ratio", lSourcePAR, blSourceParText)
            .SetTip("Target Video Bitrate in Kbps (Up/Down key)", tbBitrate, laBitrate)
            .SetTip("Target Image Width in pixel (Up/Down key)", tbTargetWidth, lTargetWidth)
            .SetTip("Target Image Height in pixel (Up/Down key)", tbTargetHeight, lTargetHeight)
            .SetTip("Target File Size (Up/Down key)", tbTargetSize)
            .SetTip("Source file", tbSourceFile)
            .SetTip("Target file", tbTargetFile)
            .SetTip("Container profile", bnMuxer)
            .SetTip("Container options like subtitles, chapters and attachments", bnMuxerOptions)
            .SetTip("Video encoder profile", bnEncoder)
            .SetTip("Options of the video encoder", bnEncoderOptions)
            .SetTip("Filter setup and filter options", bnFilters)
            .SetTip("Image size presets", bnResize)
            .SetTip("Source file options", bnSourceMenu)
            .SetTip("Target file options", bnTargetMenu)
            .SetTip("Shows the preview to check filters and to cut (F4)", bnPreview)
            .SetTip("Shows the crop dialog (F3)", bnCrop)
            .SetTip("Shows the job list (F6)", bnJobs)
        End With
    End Sub

    Sub UpdateSourceParameters()
        If p.SourceScript Is Nothing Then Exit Sub

        Try
            Dim info = p.SourceScript.GetInfo

            p.SourceWidth = info.Width
            p.SourceHeight = info.Height
            p.SourceSeconds = CInt(info.FrameCount / info.FrameRate)
            p.SourceFrameRate = info.FrameRate
            p.SourceFrames = info.FrameCount
        Catch ex As Exception
            MsgError("Source filter returned invalid parameters", p.SourceScript.GetFullScript)
            Throw New AbortException()
        End Try
    End Sub

    Sub tbSource_TextChanged(sender As Object, e As EventArgs) Handles tbSourceFile.TextChanged
        If Not BlockSourceTextBoxTextChanged Then
            If File.Exists(tbSourceFile.Text) Then
                OpenVideoSourceFile(tbSourceFile.Text)
            End If
        End If
    End Sub

    Sub bnTargetMenu_Click() Handles bnTargetMenu.Click
        UpdateTargetFileMenu()
        TargetFileMenu.Show(bnTargetMenu, 0, bnTargetMenu.Height)
    End Sub

    Sub bnSourceMenu_Click() Handles bnSourceMenu.Click
        UpdateSourceFileMenu()
        SourceFileMenu.Show(bnSourceMenu, 0, bnSourceMenu.Height)
    End Sub

    Sub tbTargetFile_DoubleClick() Handles tbTargetFile.DoubleClick
        Using dialog As New SaveFileDialog
            dialog.FileName = p.TargetFile.Base
            dialog.SetInitDir(p.TargetFile.Dir)

            If dialog.ShowDialog() = DialogResult.OK Then
                Dim ext = p.VideoEncoder.Muxer.OutputExtFull
                p.TargetFile = If(dialog.FileName.Ext = ext, dialog.FileName, dialog.FileName + ext)
            End If
        End Using
    End Sub

    Sub tbTargetFile_TextChanged(sender As Object, e As EventArgs) Handles tbTargetFile.TextChanged
        If BoundProject IsNot Nothing AndAlso BoundProject.TargetFile <> tbTargetFile.Text Then
            BoundProject.TargetFile = tbTargetFile.Text
        End If

        If LastTbTargetFileText <> tbTargetFile.Text Then
            LastTbTargetFileText = tbTargetFile.Text
            If Not p.BatchMode AndAlso Not String.IsNullOrWhiteSpace(tbTargetFile.Text) Then p.VideoEncoder.OverridesTargetFileName = False
        End If
    End Sub

    Sub tbTargetFile_MouseDown(sender As Object, e As MouseEventArgs) Handles tbTargetFile.MouseDown
        If e.Buttons = MouseButtons.Alternate Then
            UpdateTargetFileMenu()
        End If
    End Sub

    Sub tbSourceFile_MouseDown(sender As Object, e As MouseEventArgs) Handles tbSourceFile.MouseDown
        If e.Buttons = MouseButtons.Alternate Then
            UpdateSourceFileMenu()
        End If
    End Sub

    Sub UpdateAudioFileMenu(audioTrack As AudioTrack, index As Integer, a As Action)
        Dim cms = DirectCast(audioTrack.TextEdit.TextBox.ContextMenuStrip, ContextMenuStripEx)
        Dim ap = audioTrack.AudioProfile
        Dim te = audioTrack.TextEdit
        Dim exist = File.Exists(ap.File)

        cms.Items.ClearAndDisplose()

        If ap.Streams IsNot Nothing AndAlso ap.Streams.Count > 0 Then
            For Each i In ap.Streams
                Dim temp = i
                Dim menuAction = Sub()
                                     If ap.File <> p.LastOriginalSourceFile Then
                                         te.Text = p.LastOriginalSourceFile
                                     End If

                                     te.Text = temp.Name + " (" + ap.File.Ext + ")"
                                     ap.Stream = temp

                                     audioTrack.LanguageLabel.Invalidate()
                                     UpdateSizeOrBitrate()
                                 End Sub

                If ap.Streams.Count > 10 Then
                    cms.Add("Streams | " + g.ConvertPath(i.Name), menuAction)
                Else
                    cms.Add(g.ConvertPath(i.Name), menuAction)
                End If
            Next

            cms.Items.Add(New SeparatorMenuItem)
        End If

        If p.SourceFile.Ext = "avs" OrElse p.Script.GetScript.ToLowerInvariant.Contains("audiodub(") Then
            cms.Add(p.Script.Path.FileName, Sub() te.Text = p.Script.Path)
        End If

        If p.TempDir <> "" AndAlso Directory.Exists(p.TempDir) Then
            Dim audioFiles = Directory.GetFiles(p.TempDir).Where(
                Function(audioPath) FileTypes.Audio.Contains(audioPath.Ext)).ToList

            audioFiles.Sort(New StringLogicalComparer)

            If audioFiles.Count > 0 Then
                For Each i In audioFiles
                    Dim temp = i

                    Dim menuAction = Sub()
                                         audioTrack.Remove()
                                         te.Text = temp
                                     End Sub

                    If audioFiles.Count > 10 Then
                        cms.Add("Files | " + i.FileName, menuAction)
                    Else
                        cms.Add(i.FileName, menuAction)
                    End If
                Next

                cms.Items.Add(New SeparatorMenuItem)
            End If
        End If

        Dim moreFilesAction = Sub()
                                  s.Storage.SetInt("last selected muxer tab", 0) 'audio tab
                                  p.VideoEncoder.OpenMuxerConfigDialog()
                              End Sub

        cms.Add("Open File", a, "Change the audio source file.").SetImage(Symbol.OpenFile)
        cms.Add("Add more files...", moreFilesAction, exist)
        cms.Add("Play", Sub() g.PlayAudio(ap), exist, "Plays the audio source file with a media player.").SetImage(Symbol.Play)
        cms.Add("Media Info...", Sub() g.DefaultCommands.ShowMediaInfo(ap.File), exist, "Show MediaInfo for the audio source file.").SetImage(Symbol.Info)
        cms.Add("Explore", Sub() g.SelectFileWithExplorer(ap.File), exist, "Open the audio source file directory with File Explorer.").SetImage(Symbol.FileExplorer)
        cms.Add("-")
        cms.Add("Execute", Sub() ExecuteAudio(ap), exist, "Processes the audio profile.").SetImage(Symbol.LightningBolt)
        cms.Add("Execute All", Sub() ExecuteAllAudio(), p.AudioTracks.Any(Function(x) x.IsRelevant), "Processes all audio profiles.")
        cms.Add("-")
        cms.Add("Copy Path", Sub() Clipboard.SetText(ap.File), te.Text <> "")
        cms.Add("Copy Selection", Sub() Clipboard.SetText(te.TextBox.SelectedText), te.Text <> "").SetImage(Symbol.Copy)
        cms.Add("Paste", Sub() te.TextBox.Paste(), Clipboard.GetText.Trim <> "").SetImage(Symbol.Paste)
        cms.Add("-")
        cms.Add("Remove", Sub() audioTrack.Remove(), te.Text <> "", "Remove audio file").SetImage(Symbol.Remove)
    End Sub

    Sub ExecuteAudio(ap As AudioProfile)
        If MsgQuestion("Confirm to process the track.") = DialogResult.OK Then
            Dim cleanTemp = False

            Try
                If p.TempDir = "" Then
                    p.TempDir = ap.File.Dir
                    cleanTemp = True
                End If

                ap = ObjectHelp.GetCopy(ap)
                Audio.Process(ap)
                ap.Encode()
            Catch ex As AbortException
            Catch ex As Exception
                g.ShowException(ex)
            Finally
                If cleanTemp Then p.TempDir = ""
            End Try
        End If
    End Sub

    Sub ExecuteAllAudio()
        If MsgQuestion("Confirm to process ALL audio tracks.") = DialogResult.OK Then
            For Each track In p.AudioTracks
                Dim cleanTemp = False

                Try
                    If track.TextEdit.Text = "" Then Continue For
                    If TypeOf track.AudioProfile Is NullAudioProfile Then Continue For
                    If p.TempDir = "" Then
                        p.TempDir = track.AudioProfile.File.Dir()
                        cleanTemp = True
                    End If

                    Dim ap = ObjectHelp.GetCopy(track.AudioProfile)
                    Audio.Process(ap)
                    ap.Encode()
                Catch ex As AbortException
                Catch ex As Exception
                    g.ShowException(ex)
                Finally
                    If cleanTemp Then p.TempDir = ""
                End Try
            Next
        End If
    End Sub

    Sub UpdateTargetFileMenu()
        TargetFileMenu.Items.ClearAndDisplose
        TargetFileMenu.Add("Browse File...", AddressOf tbTargetFile_DoubleClick, File.Exists(p.SourceFile), "Change the path of the target file.")
        TargetFileMenu.Add("Play", Sub() g.Play(p.TargetFile), File.Exists(p.TargetFile), "Play the target file.").SetImage(Symbol.Play)
        TargetFileMenu.Add("Media Info...", Sub() g.DefaultCommands.ShowMediaInfo(p.TargetFile), File.Exists(p.TargetFile), "Show MediaInfo for the target file.").SetImage(Symbol.Info)
        TargetFileMenu.Add("Explore...", Sub() g.SelectFileWithExplorer(p.TargetFile), Directory.Exists(p.TargetFile.Dir), "Open the target file directory with File Explorer.").SetImage(Symbol.FileExplorer)
        TargetFileMenu.Add("-")
        TargetFileMenu.Add("Copy", Sub() tbTargetFile.TextBox.Copy(), tbTargetFile.Text <> "").SetImage(Symbol.Copy)
        TargetFileMenu.Add("Paste", Sub() tbTargetFile.TextBox.Paste(), Clipboard.GetText.Trim <> "" AndAlso File.Exists(p.SourceFile)).SetImage(Symbol.Paste)
    End Sub

    Sub UpdateSourceFileMenu()
        SourceFileMenu.Items.ClearAndDisplose
        Dim isIndex = FileTypes.VideoIndex.Contains(p.LastOriginalSourceFile.Ext)

        SourceFileMenu.Add("Open...", AddressOf ShowOpenSourceDialog, "Open source files").SetImage(Symbol.OpenFile)
        SourceFileMenu.Add("Play", Sub() g.Play(p.LastOriginalSourceFile), File.Exists(p.LastOriginalSourceFile) AndAlso Not isIndex, "Play the source file.").SetImage(Symbol.Play)
        SourceFileMenu.Add("Media Info...", Sub() g.DefaultCommands.ShowMediaInfo(p.LastOriginalSourceFile), File.Exists(p.LastOriginalSourceFile) AndAlso Not isIndex, "Show MediaInfo for the source file.").SetImage(Symbol.Info)
        SourceFileMenu.Add("Explore...", Sub() g.SelectFileWithExplorer(p.SourceFile), File.Exists(p.SourceFile), "Open the source file directory with File Explorer.").SetImage(Symbol.FileExplorer)
        SourceFileMenu.Items.Add(New SeparatorMenuItem)
        SourceFileMenu.Add("Copy", Sub() tbSourceFile.Copy(), tbSourceFile.Text <> "", "Copies the selected text to the clipboard.").SetImage(Symbol.Copy)
        SourceFileMenu.Add("Paste", Sub() tbSourceFile.Paste(), Clipboard.GetText.Trim <> "", "Copies the full source file path to the clipboard.").SetImage(Symbol.Paste)
    End Sub

    Sub MainForm_FilesDropped(files As String()) Handles Me.FilesDropped
        SetLastModifiedTemplate()

        If Not files.NothingOrEmpty Then
            Dim showTemplateSelection = (s.ShowTemplateSelection And (ShowTemplateSelectionMode.Always Or ShowTemplateSelectionMode.DragDrop)) <> 0

            If p.SourceFile <> "" AndAlso IsSaveCanceled() Then Exit Sub
            If Not showTemplateSelection Then OpenProject(g.LastModifiedTemplate)
            BeginInvoke(Sub() OpenAnyFile(files.ToList, showTemplateSelection))
        End If
    End Sub

    Protected Overrides Sub OnActivated(e As EventArgs)
        MyBase.OnActivated(e)
        UpdateNextButton()

        ProcController.SetLastActivation()

        BeginInvoke(New Action(Sub()
                                   DoEvents()
                                   Assistant()
                                   UpdateScriptsMenuAsync()
                               End Sub))
    End Sub

    Protected Overrides Sub OnLoad(args As EventArgs)
        MyBase.OnLoad(args)
    End Sub

    Private WasShown As Boolean

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)

        If WasShown Then
            Exit Sub
        End If

        WasShown = True
        UpdateDynamicMenuAsync()
        UpdateRecentProjectsMenu()
        UpdateTemplatesMenuAsync()
        IsLoading = False
        Refresh()
        g.CheckForLongPathSupport()
        g.PreloadValuesAsync()
        ShowChangelog(False)
        ProcessCommandLine(Environment.CommandLine)
        StaxRipUpdate.SetFirstRunOnCurrentVersion()
        StaxRipUpdate.ShowUpdateQuestion()
        StaxRipUpdate.CheckForUpdateAsync(False, Environment.Is64BitProcess)
        g.RunTask(AddressOf g.LoadPowerShellScripts)
        'Text = $"{Size.Width}x{Size.Height}"
    End Sub

    Protected Overrides Sub OnClosing(args As CancelEventArgs)
        MyBase.OnClosing(args)

        If IsSaveCanceled() Then
            args.Cancel = True
        End If
    End Sub

    Protected Overrides Sub OnClosed(e As EventArgs)
        MyBase.OnClosed(e)

        If g.ProcForm IsNot Nothing Then
            If g.ProcForm.InvokeRequired Then
                g.ProcForm.Invoke(Sub() g.ProcForm.Close())
            Else
                g.ProcForm.Close()
            End If
        End If

        Try
            g.RaiseAppEvent(ApplicationEvent.ApplicationExit)
        Catch ex As Exception
        End Try

        g.SaveSettings()
    End Sub

    Protected Overrides Sub OnLostFocus(e As EventArgs)
        MyBase.OnLostFocus(e)
        UpdateNextButton()
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        UpdateNextButton()
    End Sub

    Protected Overrides Sub OnKeyUp(e As KeyEventArgs)
        MyBase.OnKeyUp(e)
        UpdateNextButton()
    End Sub

    Function GetShowWithoutActivation() As Boolean
            If g.IsAppStart Then
                g.IsAppStart = False
                If s.StartWithoutFocus OrElse ParseCommandLine(Environment.CommandLine)?.Contains("-" & NameOf(GlobalCommands.NoFocus), StringComparer.OrdinalIgnoreCase) Then
                    ProcController.SetLastActivation(-s.PreventFocusStealAfter * 2)
                    Return True
                End If
            End If

            If ProcController.BlockActivation Then
                ProcController.BlockActivation = False
                Return True
            End If

            Return False
    End Function

    Sub UpdateTargetSizeLabel()
        Me.blFilesize.Text = $"Size ({PrefixedSize(2).Unit})"
    End Sub

    Sub UpdateNextButton()
        Dim ready = AssistantPassed AndAlso CanIgnoreTip
        bnAddJob.Enabled = ready

        If ready Then
            Dim toTop = Keyboard.Modifiers.HasFlag(Keys.Shift) <> s.InvertShiftKeyOnNextButton
            Dim hideList = Keyboard.Modifiers.HasFlag(Keys.Control) <> s.InvertCtrlKeyOnNextButton
            bnAddJob.Text = If(toTop, "Add to Top of Jobs", "Add to Jobs") + If(hideList, " Without Opening Them", "")
        Else
            bnAddJob.Text = "Add to Jobs"
        End If
    End Sub

    'the modifier keys change where the job is added and whether the job list opens
    Sub AddJobFromButton()
        Dim toTop = Keyboard.Modifiers.HasFlag(Keys.Shift) <> s.InvertShiftKeyOnNextButton
        Dim hideList = Keyboard.Modifiers.HasFlag(Keys.Control) <> s.InvertCtrlKeyOnNextButton
        AddJob(Not hideList, If(toTop, 0, -1))
    End Sub

    Sub ShowReadyStep()
        Dim top = s.InvertShiftKeyOnNextButton
        Dim hide = s.InvertCtrlKeyOnNextButton

        NextStep.Kind = NextStepKind.Ready
        NextStep.SetMessage("Ready to add the job",
            $"Add to Jobs adds it to the {If(top, "top", "bottom")} of the job list and {If(hide, "keeps the list closed", "opens the list")}. " +
            $"Hold Shift to add it to the {If(top, "bottom", "top")}, or Ctrl to {If(hide, "open the list", "keep the list closed")}.")
        NextStep.SetAction(Nothing)
        NextStep.ContinueButton.Visible = False
    End Sub

    Sub NextStepAction()
        If AssistantClickAction IsNot Nothing Then
            AssistantClickAction.Invoke()
            Assistant()
        End If
    End Sub

    Sub SkipAssistantTip()
        If CanIgnoreTip AndAlso Not p.SkippedAssistantTips.Contains(CurrentAssistantTipKey) Then
            p.SkippedAssistantTips.Add(CurrentAssistantTipKey)
        End If

        Assistant()
    End Sub

    <Command("Shows the changes of all versions of the port.")>
    Sub ShowChangelog(force As Boolean)
        Dim appDetails = g.DefaultCommands.GetApplicationDetails(True, True, False)
        If Not force AndAlso s.ShowChangelog = appDetails Then Exit Sub

        Using td As New TaskDialog(Of String)()
            td.Title = $"What's new in {appDetails}:"
            td.Icon = TaskIcon.Shield
            td.Content = GetChangelog()
            td.AddButton("OK")
            td.Show()
        End Using

        s.ShowChangelog = appDetails
        If Not force Then g.SaveSettings()
    End Sub

    'the versions of the port, newest first, followed by the notes of the StaxRip release it's based on
    Shared Function GetChangelog() As String
        Dim ass = Assembly.GetExecutingAssembly()
        Dim versions As New List(Of String())
        Dim sb As New StringBuilder()

        Using stream = ass.GetManifestResourceStream("StaxRip.CHANGELOG-LINUX.txt")
            If stream IsNot Nothing Then
                Using reader As New StreamReader(stream)
                    For Each line In reader.ReadToEnd().SplitLinesNoEmpty()
                        Dim fields = line.Split(ChrW(9))

                        If fields.Length = 3 Then
                            versions.Add(fields)
                        End If
                    Next
                End Using
            End If
        End Using

        If versions.Count > 0 Then
            Dim width = versions.Max(Function(i) i(0).Length)
            sb.AppendLine("Versions of the port, newest first:" + BR)

            For Each i In versions
                sb.AppendLine($"{i(0).PadRight(width)}  {i(1)}  {i(2)}")
            Next
        Else
            sb.AppendLine("Only builds from a git clone with the tags of the port list its versions.")
        End If

        Dim baseVersion = ass.GetName().Version.ToString(3)
        Dim notes = GetStaxRipReleaseNotes(baseVersion)

        If notes.Notes <> "" Then
            sb.AppendLine()
            sb.AppendLine($"The port is based on StaxRip v{baseVersion} ({notes.Date}):" + BR)
            sb.Append(notes.Notes)
        End If

        Return sb.ToString().Trim()
    End Function

    Shared Function GetStaxRipReleaseNotes(version As String) As (Notes As String, [Date] As String)
        Dim sb As New StringBuilder()
        Dim releaseDate = ""
        Dim inRelease = False
        Dim inComment = False

        Using stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StaxRip.CHANGELOG.md")
            Using reader As New StreamReader(stream)
                Do While Not reader.EndOfStream
                    Dim line = reader.ReadLine()

                    If line.Trim() = "<!--" Then inComment = True
                    If inComment Then
                        If line.Trim() = "-->" Then inComment = False
                        Continue Do
                    End If

                    Dim heading = Regex.Match(line, "^v(?<version>\d+\.\d+(?:\.\d+)*)\S*\s+\((?<date>[^)]*)\)")

                    If heading.Success Then
                        If inRelease Then Exit Do
                        inRelease = heading.Groups("version").Value = version
                        releaseDate = heading.Groups("date").Value
                        Continue Do
                    End If

                    If inRelease AndAlso Not line.StartsWith("====") Then
                        'issue links of GitHub like ([#1741](/../../issues/1741)) keep only the number
                        sb.AppendLine(Regex.Replace(line, "\[(#\d+)\]\([^)]*\)", "$1"))
                    End If
                Loop
            End Using
        End Using

        Return (sb.ToString().Trim(), releaseDate)
    End Function
End Class
