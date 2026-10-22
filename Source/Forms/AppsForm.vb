
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports Microsoft.VisualBasic

Imports StaxRip.UI

Public Class AppsForm
    Inherits SizeSavingDialogBase
    Implements IUpdateUI

    Public WithEvents tv As TreeViewEx
    Public WithEvents tsbLaunch, tsbHelp, tsbWebsite, tsbDownload, tsbVersion, tsbExplore, ddbTools As ButtonEx
    Public flp As StackLayout
    Public WithEvents SearchTextBox As SearchTextBox
    Public WithEvents cmsTools As ContextMenuStripEx
    Public WithEvents miEditPath, miClearPaths, miFindPath, miShowGrid, miStatus, miStatusRequired, miAutoUpdate As MenuItemEx
    Public WithEvents miEditVersion, miEditChangelog, miDownload, miWebsite, miExplore, miLaunch, miHelp, miCopyPath, miPATHEnvVar, miUpdateRequest As MenuItemEx
    Private ToolBarRow As Control

    Private Sub InitializeComponent()
        tv = New TreeViewEx
        SearchTextBox = New SearchTextBox
        flp = New StackLayout With {.Orientation = Orientation.Vertical, .Spacing = FH(0.15)}

        Dim toolButton = Function(text As String, tip As String) New ButtonEx With {.Text = text, .ToolTip = tip}
        tsbLaunch = toolButton("Launch", "Launches the app (Ctrl+L)")
        tsbExplore = toolButton("Explore", "Opens the apps folder in the file manager (Ctrl+E)")
        tsbWebsite = toolButton("Web", "Opens the apps website (Ctrl+W)")
        tsbDownload = toolButton("Download", "Opens the apps download web page (Ctrl+D)")
        tsbVersion = toolButton("Version", "Edits the apps version (F12)")
        tsbHelp = toolButton("Help", "Opens the apps help (F1)")

        Dim menuItem = Function(text As String, key As Keys, tip As String) As MenuItemEx
                           Dim ret = New MenuItemEx(text) With {.Help = tip}
                           ret.Form = Me
                           ret.Shortcut = key
                           Return ret
                       End Function

        miEditPath = menuItem("Edit Path...", Keys.Control Or Keys.P, "Show Open File dialog to customize the path")
        miFindPath = menuItem("Find Path...", Keys.Control Or Keys.F, "Searches the file system for the app")
        miClearPaths = menuItem("Clear Paths...", Keys.Control Or Keys.X, "Clear custom paths")
        miCopyPath = menuItem("Copy Path", Keys.None, Nothing)
        miPATHEnvVar = menuItem("PATH Env Var...", Keys.None, Nothing)
        miEditVersion = menuItem("Edit Version", Keys.F12, "Edits the apps version")
        miEditChangelog = menuItem("Edit Changelog", Keys.F10, Nothing)
        miShowGrid = menuItem("Show Grid", Keys.Control Or Keys.G, "Show tools as CSV table")
        miStatus = menuItem("Check All", Keys.Control Or Keys.S, "Check status of all tools")
        miStatusRequired = menuItem("Check Required Only", Keys.Control Or Keys.Y, "Check status of all required tools")
        miAutoUpdate = menuItem("Auto Update", Keys.Control Or Keys.U, "Full automatic update")
        miUpdateRequest = menuItem("Update Request", Keys.Control Or Keys.R, Nothing)
        miDownload = menuItem("Download", Keys.Control Or Keys.D, "Opens the apps download web page")
        miWebsite = menuItem("Website", Keys.Control Or Keys.W, "Opens the apps website")
        miExplore = menuItem("Explore", Keys.Control Or Keys.E, "Opens the apps folder in the file manager")
        miLaunch = menuItem("Launch", Keys.Control Or Keys.L, "Launches the app")
        miHelp = menuItem("Help", Keys.F1, "Opens the apps help")

        cmsTools = New ContextMenuStripEx

        For Each i In {miEditPath, miFindPath, miClearPaths, miCopyPath, miPATHEnvVar}
            cmsTools.Items.Add(i)
        Next

        cmsTools.Items.Add(New SeparatorMenuItem)

        For Each i In {miEditVersion, miEditChangelog, miShowGrid, miStatus, miStatusRequired, miAutoUpdate, miUpdateRequest, miDownload, miWebsite, miExplore, miLaunch, miHelp}
            cmsTools.Items.Add(i)
        Next

        ddbTools = New ButtonEx With {.Text = "Tools", .ShowMenuSymbol = True, .ContextMenuStrip = cmsTools}
        ToolBarRow = HRow(tsbLaunch, tsbExplore, tsbWebsite, tsbDownload, tsbVersion, ddbTools, tsbHelp)

        Dim left As New TableLayout(Row(SearchTextBox), ScaledRow(tv)) With {.Spacing = DefaultSpacing}
        Dim details As New Scrollable With {.Content = flp, .Border = BorderType.None, .Padding = New Padding(FH(0.4), 0)}

        'wrapping labels in a scrollable area take the width of the whole text unless it's limited
        AddHandler details.SizeChanged, Sub()
                                            Dim width = details.Width - details.Padding.Horizontal - FH(1.5)

                                            If width > FH(10) Then
                                                For Each label In Contents.Values.OfType(Of LabelEx)().Where(Function(i) i.Wrap <> WrapMode.None)
                                                    label.Width = width
                                                Next
                                            End If
                                        End Sub
        Dim right As New TableLayout(Row(ToolBarRow), ScaledRow(details)) With {.Spacing = DefaultSpacing}

        left.Width = FH(15)
        Content = New TableLayout(ScaledRow(left, Scaled(right))) With {.Spacing = New Size(FH(0.6), 0), .Padding = DefaultPadding}
        Text = $"Apps - {g.DefaultCommands.GetApplicationDetails()}"
        FileDrop = True
    End Sub

    Private CurrentPackage As Package
    Private ReadOnly Nodes As New List(Of TreeNode)
    Private ReadOnly Headers As New Dictionary(Of String, Control)
    Private ReadOnly Contents As New Dictionary(Of String, Control)
    Private ReadOnly SetupButton As New ButtonEx
    Private ToolUpdate As ToolUpdate

    Sub New()
        MyBase.New()
        InitializeComponent()
        RestoreClientSize(56, 34)
        SetMinimumSize(53, 20)

        AddHandler SearchTextBox.KeyDown, AddressOf SearchTextBoxKeyDown

        miEditChangelog.Visible = g.IsDevelopmentPC

        AddHandler SetupButton.Click, Sub()
                                          CurrentPackage.SetupAction.Invoke
                                          ShowActivePackage()
                                      End Sub

        SetupButton.Font = FontManager.GetDefaultFont(1)

        Dim titleHeaderLabel = New LabelEx With {
            .Font = FontManager.GetDefaultFont(5, FontStyle.Bold)
        }

        Headers("Title") = titleHeaderLabel
        flp.Items.Add(New StackLayoutItem(titleHeaderLabel))
        AddSection("Status")
        flp.Items.Add(New StackLayoutItem(SetupButton))
        AddSection("Location", Sub() tsbOpenDir_Click(Nothing, Nothing))
        AddSection("Version", Sub() tsbVersion_Click(Nothing, Nothing))
        AddSection("AviSynth Filters")
        AddSection("VapourSynth Filters")
        AddSection("Filters")
        AddSection("Description")
        AddSection("Website", Sub() g.ShellExecute(CurrentPackage.WebURL))
        AddSection("Help", Sub() g.ShellExecute(CurrentPackage.HelpURL))
        AddSection("Download", Sub() g.ShellExecute(CurrentPackage.DownloadURL))

        SearchTextBox_TextChanged()
    End Sub

    Sub SearchTextBoxKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyData = Keys.Down Then
            tv.MoveSelectionDown()
            e.Handled = True
        ElseIf e.KeyData = Keys.Up Then
            tv.MoveSelectionUp()
            e.Handled = True
        End If
    End Sub

    Sub SetText(key As String, value As String)
        Dim c = Contents(key)

        If TypeOf c Is LinkButton Then
            DirectCast(c, LinkButton).Text = value
        ElseIf TypeOf c Is Label Then
            DirectCast(c, Label).Text = value
        End If
    End Sub

    Function GetText(key As String) As String
        Dim c = Contents(key)

        If TypeOf c Is LinkButton Then
            Return DirectCast(c, LinkButton).Text
        ElseIf TypeOf c Is Label Then
            Return DirectCast(c, Label).Text
        End If

        Return ""
    End Function

    Sub SetTextColor(key As String, color As Color)
        Dim c = Contents(key)

        If TypeOf c Is LinkButton Then
            DirectCast(c, LinkButton).TextColor = color
        ElseIf TypeOf c Is Label Then
            DirectCast(c, Label).TextColor = color
        End If
    End Sub

    Sub ShowActivePackage()
        If IsDisposed OrElse CurrentPackage Is Nothing OrElse Not Headers.ContainsKey("Title") Then Exit Sub

        Dim path = CurrentPackage.Path

        DirectCast(Headers("Title"), Label).Text = CurrentPackage.Name

        SetupButton.Text = "Install " + CurrentPackage.Name
        SetupButton.Visible = CurrentPackage.SetupAction IsNot Nothing AndAlso CurrentPackage.GetStatus <> ""

        tsbExplore.Enabled = path <> ""
        miExplore.Enabled = tsbExplore.Enabled
        miCopyPath.Enabled = path <> ""
        tsbLaunch.Enabled = CurrentPackage.LaunchAction IsNot Nothing AndAlso CurrentPackage.Path <> ""
        miLaunch.Enabled = tsbLaunch.Enabled
        tsbDownload.Enabled = CurrentPackage.DownloadURL <> ""
        miDownload.Enabled = tsbDownload.Enabled
        miAutoUpdate.Enabled = tsbDownload.Enabled AndAlso CurrentPackage.SupportsAutoUpdate
        tsbWebsite.Enabled = CurrentPackage.URL <> ""

        tsbVersion.Enabled = CurrentPackage.Path.FileExists AndAlso
            Not (CurrentPackage.IsVersionOld() AndAlso Not CurrentPackage.VersionAllowOld)

        miEditPath.Enabled = CurrentPackage.IsCustomPathAllowed
        miFindPath.Enabled = miEditPath.Enabled

        s.StringDictionary("RecentExternalApplicationControl") = CurrentPackage.Name + CurrentPackage.Version

        Dim status = CurrentPackage.GetStatus()
        SetText("Status", CurrentPackage.GetStatusDisplay())
        SetTextColor("Status", If(status <> "",
                                  If(CurrentPackage.Required, Color.FromArgb(220, 50, 50), Color.FromArgb(200, 140, 0)),
                                  Color.FromArgb(40, 160, 70)))

        Dim correctVersion = CurrentPackage.IsVersionCorrect()
        SetText("Version", "-")

        If File.Exists(CurrentPackage.Path) Then
            If CurrentPackage.IsSystemInstalled Then
                SetText("Version", "System (" + File.GetLastWriteTimeUtc(CurrentPackage.Path).ToShortDateString() + ")")
            Else
                SetText("Version", If(correctVersion, CurrentPackage.Version, "Unknown") +
                    " (" + File.GetLastWriteTimeUtc(CurrentPackage.Path).ToShortDateString() + ")")
            End If
        End If

        SetText("Location", If(path = "", "Not found", path))
        SetText("Description", CurrentPackage.Description)

        Dim visible = Not String.IsNullOrWhiteSpace(CurrentPackage.WebURL)
        Headers("Website").Visible = visible
        SetText("Website", CurrentPackage.WebURL)
        Contents("Website").Visible = visible

        visible = Not String.IsNullOrWhiteSpace(CurrentPackage.HelpURL)
        Headers("Help").Visible = visible
        SetText("Help", CurrentPackage.HelpURL)
        Contents("Help").Visible = visible

        visible = Not String.IsNullOrWhiteSpace(CurrentPackage.DownloadURL)
        Headers("Download").Visible = visible
        SetText("Download", CurrentPackage.DownloadURL)
        Contents("Download").Visible = visible

        Headers("AviSynth Filters").Visible = False
        Contents("AviSynth Filters").Visible = False

        Headers("VapourSynth Filters").Visible = False
        Contents("VapourSynth Filters").Visible = False

        Headers("Filters").Visible = False
        Contents("Filters").Visible = False

        If TypeOf CurrentPackage Is PluginPackage Then
            Dim plugin = DirectCast(CurrentPackage, PluginPackage)

            If plugin.AvsFilterNames IsNot Nothing AndAlso plugin.VsFilterNames IsNot Nothing Then
                Headers("AviSynth Filters").Visible = True
                SetText("AviSynth Filters", plugin.AvsFilterNames.Join(", "))
                Contents("AviSynth Filters").Visible = True

                Headers("VapourSynth Filters").Visible = True
                SetText("VapourSynth Filters", plugin.VsFilterNames.Join(", "))
                Contents("VapourSynth Filters").Visible = True
            ElseIf plugin.AvsFilterNames IsNot Nothing Then
                Headers("Filters").Visible = True
                SetText("Filters", plugin.AvsFilterNames.Join(", "))
                Contents("Filters").Visible = True
            ElseIf plugin.VsFilterNames IsNot Nothing Then
                Headers("Filters").Visible = True
                SetText("Filters", plugin.VsFilterNames.Join(", "))
                Contents("Filters").Visible = True
            End If
        End If
    End Sub

    Sub AddSection(title As String, Optional clickAction As Action = Nothing)
        If String.IsNullOrWhiteSpace(title) Then Return

        Dim headerLabel = New LabelEx With {
            .Text = title,
            .Font = FontManager.GetDefaultFont(0, FontStyle.Bold)}

        Headers(title) = headerLabel
        flp.Items.Add(New StackLayoutItem(headerLabel))

        Dim contentLabel As Control

        If clickAction IsNot Nothing Then
            contentLabel = New ButtonLabel With {.ClickAction = clickAction}
        Else
            contentLabel = New LabelEx With {.Wrap = WrapMode.Word}
        End If

        Contents(title) = contentLabel
        'wrapping labels get the visible width, they would follow long paths otherwise
        flp.Items.Add(New StackLayoutItem(contentLabel, If(TypeOf contentLabel Is LabelEx, HorizontalAlignment.Left, HorizontalAlignment.Stretch)))
    End Sub

    Sub ShowPackage(package As Package)
        For Each node As TreeNode In tv.Nodes
            If node.IsExpanded Then
                node.Collapse()
            End If
        Next

        For Each i In tv.GetNodes
            If package Is i.Tag Then
                tv.SelectedNode = i
            End If
        Next
    End Sub

    Sub ShowPackage(tn As TreeNode)
        If Not tn Is Nothing AndAlso Not tn.Tag Is Nothing Then
            Dim newPackage = DirectCast(tn.Tag, Package)

            If Not newPackage Is CurrentPackage Then
                CurrentPackage = newPackage
                ShowActivePackage()
            End If
        End If
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        AviSynthToolPathAsync()
    End Sub

    Async Sub AviSynthToolPathAsync()
        Await Task.Run(AddressOf FrameServerHelp.AviSynthToolPath)
        ShowActivePackage()
    End Sub

    Protected Overrides Sub OnGotFocus(e As EventArgs)
        ShowActivePackage()
        MyBase.OnGotFocus(e)
    End Sub

    Sub AppsForm_FilesDropped(files As String()) Handles Me.FilesDropped
        BeginInvoke(Sub()
                        If files.NothingOrEmpty OrElse CurrentPackage Is Nothing Then
                            Exit Sub
                        End If

                        'the update replaces all files in the folder of the tool
                        If CurrentPackage.IsSystemInstalled Then
                            MsgInfo(CurrentPackage.Name + " is installed system wide, please update it with the package manager.", CurrentPackage.Path)
                            Exit Sub
                        End If

                        Try
                            If files.Length = 1 AndAlso (files(0).Ext.EqualsAny("zip", "7z") OrElse
                                files(0).ToLowerInvariant.EndsWithAny(".tar.gz", ".tar.xz", ".tar.bz2", ".tar.zst", ".tgz")) Then

                                ToolUpdate = New ToolUpdate(CurrentPackage, Me)
                                ToolUpdate.DownloadFile = files(0)
                                ToolUpdate.Extract()
                            Else
                                ToolUpdate = New ToolUpdate(CurrentPackage, Me)
                                ToolUpdate.ExtractDir = Path.Combine(Folder.Temp, Guid.NewGuid.ToString())
                                Directory.CreateDirectory(ToolUpdate.ExtractDir)

                                For Each i In files
                                    FileHelp.Copy(i, Path.Combine(ToolUpdate.ExtractDir, i.FileName))
                                Next

                                ToolUpdate.DeleteOldFiles()
                                FolderHelp.Delete(ToolUpdate.ExtractDir)
                            End If
                        Catch ex As Exception
                            g.ShowException(ex, "Update of " + CurrentPackage.Name + " failed")
                        End Try
                    End Sub)
    End Sub

    Sub tv_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles tv.AfterSelect
        If e.Node.Tag Is Nothing AndAlso e.Node.Nodes.Count > 0 Then
            tv.SelectedNode = e.Node.Nodes(0)
        End If

        If Not e.Node.Tag Is Nothing Then
            ShowPackage(e.Node)
        End If
    End Sub

    Sub SearchTextBox_TextChanged() Handles SearchTextBox.TextChanged
        Dim current = CurrentPackage

        tv.BeginUpdate()
        tv.Nodes.Clear()

        For Each pack In Package.Items.Values
            If Not pack.IsLinuxSupported Then
                Continue For
            End If

            Dim plugin = TryCast(pack, PluginPackage)
            Dim searchString = pack.Name + pack.Description + pack.Version + pack.WebURL + plugin?.VsFilterNames.Join(" ") + pack.Path + plugin?.AvsFilterNames.Join(" ")
            Dim newVersion = Not s.AllowToolsWithWrongVersion AndAlso Not pack.IsVersionValid()
            Dim notFound = pack.GetStatusLocation() <> ""

            If searchString?.ToLowerInvariant.Contains(SearchTextBox.Text?.ToLowerInvariant) OrElse
            (newVersion AndAlso SearchTextBox.Text?.ToLowerInvariant.Contains("<newversion>")) OrElse
            (notFound AndAlso SearchTextBox.Text?.ToLowerInvariant.Contains("<notfound>")) Then
                If plugin Is Nothing Then
                    If pack.TreePath <> "" Then
                        Dim n = tv.AddNode(pack.TreePath + "|" + pack.Name)
                        Nodes.Add(n)
                        n.Tag = pack
                    Else
                        Dim n = tv.AddNode("Apps|" + pack.Name)
                        Nodes.Add(n)
                        n.Tag = pack
                    End If
                Else
                    If plugin.AvsFilterNames?.Length >= 0 Then
                        Dim n = tv.AddNode("AviSynth|" + pack.Name)
                        Nodes.Add(n)
                        n.Tag = pack
                    End If

                    If plugin.VsFilterNames?.Length >= 0 Then
                        Dim n = tv.AddNode("VapourSynth|" + pack.Name)
                        Nodes.Add(n)
                        n.Tag = pack
                    End If
                End If
            End If
        Next

        If tv.Nodes.Count > 0 Then
            tv.SelectedNode = tv.Nodes(0)
        End If

        ToolBarRow.Enabled = tv.Nodes.Count > 0
        flp.Visible = tv.Nodes.Count > 0

        If SearchTextBox.Text <> "" Then
            tv.ExpandAll()
        Else
            ShowPackage(current)
        End If

        tv.EndUpdate()
    End Sub

    Sub tsbLaunch_Click(sender As Object, e As EventArgs) Handles tsbLaunch.Click
        CurrentPackage.LaunchAction?.Invoke()
    End Sub

    <DebuggerNonUserCode()>
    Sub tsbOpenDir_Click(sender As Object, e As EventArgs) Handles tsbExplore.Click
        g.SelectFileWithExplorer(CurrentPackage.Path)
    End Sub

    Sub tsbHelp_Click(sender As Object, e As EventArgs) Handles tsbHelp.Click
        CurrentPackage.ShowHelp()
    End Sub

    Sub tsbWebsite_Click(sender As Object, e As EventArgs) Handles tsbWebsite.Click
        g.ShellExecute(CurrentPackage.URL)
    End Sub

    Sub tsbDownload_Click(sender As Object, e As EventArgs) Handles tsbDownload.Click
        If CurrentPackage.DownloadURL <> "" Then
            g.ShellExecute(CurrentPackage.DownloadURL)
        End If
    End Sub

    Sub tsbVersion_Click(sender As Object, e As EventArgs) Handles tsbVersion.Click
        If Not File.Exists(CurrentPackage.Path) Then
            Exit Sub
        End If

        Dim version = FileVersionInfo.GetVersionInfo(CurrentPackage.Path)

        Dim fileVersionString = version.FileMajorPart & "." & version.FileMinorPart & "." &
                                version.FileBuildPart & "." & version.FilePrivatePart

        Dim productVersionString = version.ProductMajorPart & "." & version.ProductMinorPart & "." &
                                   version.ProductBuildPart & "." & version.ProductPrivatePart
        Dim msg = ""

        If fileVersionString <> "0.0.0.0" Then
            msg += BR2 + "File Version: " + fileVersionString + " (often not correct!)"
        End If

        If productVersionString <> "0.0.0.0" Then
            msg += BR2 + "Product Version: " + productVersionString + " (often not correct!)"
        End If

        Dim input = InputBox.Show("What's the name of this version?", CurrentPackage.Version, msg?.Trim)

        If input <> "" Then
            CurrentPackage.SetVersion(input.Replace(";", "_").Trim)
            ShowActivePackage()
            DoEvents()
        End If
    End Sub

    Sub miShowGridView_Click(sender As Object, e As EventArgs) Handles miShowGrid.Click
        If CurrentPackage Is Nothing Then Exit Sub
        Dim rows As New List(Of Object)

        For Each pack In Package.Items.Values.OrderBy(Function(i) i.GetTypeName)
            Dim row = New With {.Name = "", .Type = "", .Filename = "",
                .Version = "", .ModifiedDate = "", .Folder = ""}

            row.Name = pack.Name
            row.Type = pack.GetTypeName
            row.Filename = pack.Filename
            row.Folder = pack.Directory

            If pack.IsVersionCorrect Then
                row.Version = "'" + pack.Version + "'"
            End If

            If File.Exists(pack.Path) Then
                row.ModifiedDate = File.GetLastWriteTime(pack.Path).ToString("yyyy-MM-dd")
            End If

            rows.Add(row)
        Next

        Dim csvFile = Path.Combine(Folder.Temp, "staxrip tools.csv")
        g.ConvertToCSV(";", rows).WriteFileUTF8(csvFile)
        g.ShellExecute(csvFile)
    End Sub

    Sub miStatus_Click(sender As Object, e As EventArgs) Handles miStatus.Click
        Dim counter As Integer = 0
        Dim txt = ""

        For Each pair In Package.Items
            Dim pack = pair.Value

            If pack.GetStatus <> "" Then
                txt += pack.Name + ": " + pack.GetStatus + BR2
                counter += 1
            End If
        Next

        If txt = "" Then
            MsgInfo("OK!", "All tools have OK status!")
        Else
            MsgInfo($"{counter} apps found!", txt)
        End If
    End Sub

    Sub miStatusRequired_Click(sender As Object, e As EventArgs) Handles miStatusRequired.Click
        Dim counter As Integer = 0
        Dim txt = ""

        For Each pair In Package.Items
            Dim pack = pair.Value

            If pack.GetStatus <> "" AndAlso pack.Required Then
                txt += pack.Name + ": " + pack.GetStatus + BR2
                counter += 1
            End If
        Next

        If txt = "" Then
            MsgInfo("OK!", "All required tools have OK status!")
        Else
            MsgInfo($"{counter} apps found!", txt)
        End If
    End Sub

    Sub miBrowsePath_Click(sender As Object, e As EventArgs) Handles miEditPath.Click
        Using dialog As New OpenFileDialog
            dialog.SetInitDir(s.Storage.GetString(CurrentPackage.Name + "custom path"))

            If CurrentPackage.Filter <> "" Then
                dialog.Filter = CurrentPackage.Filter
            Else
                dialog.Filter = "|" + CurrentPackage.Filename + "|All Files|*.*"
            End If

            If dialog.ShowDialog = DialogResult.OK Then
                If Not s.AllowCustomPathsInStartupFolder AndAlso
                    dialog.FileName.ToLowerEx.StartsWithEx(Folder.Startup.ToLowerEx + Path.DirectorySeparatorChar) AndAlso
                    Not dialog.FileName.ToLowerEx.StartsWithEx(Folder.Settings.ToLowerEx + Path.DirectorySeparatorChar) Then

                    MsgError("Custom paths within the startup folder are not permitted " +
                             "because it would prevent a simple update process." + BR2 +
                             "Please put the file somewhere else outside the startup folder.")
                    Exit Sub
                End If

                s.Storage.SetString(CurrentPackage.Name + "custom path", dialog.FileName)
                ShowActivePackage()
            End If
        End Using
    End Sub

    Sub miClearCustomPath_Click(sender As Object, e As EventArgs) Handles miClearPaths.Click
        Dim packs = Package.Items.Values.Where(Function(pack) pack.GetStoredPath() <> "")

        If packs.Count > 0 Then
            Using td As New TaskDialog(Of Package)
                td.Title = "Choose a path to be cleared."

                For Each pack In packs
                    td.AddCommand(pack.Name, pack.GetStoredPath, pack)
                Next

                If Not td.Show Is Nothing Then
                    td.SelectedValue.SetStoredPath(Nothing)
                    ShowActivePackage()
                End If
            End Using
        Else
            MsgInfo("No custom paths defined.")
        End If
    End Sub

    Sub miFindPath_Click(sender As Object, e As EventArgs) Handles miFindPath.Click
        Everything()
    End Sub

    'searches the app with locate if available, otherwise in the common binary and library folders
    Async Sub Everything()
        Dim paths As New List(Of String)
        Dim fileName = CurrentPackage.Filename

        Dim run = Sub()
                      If ShellHelp.Which("locate") <> "" Then
                          Dim output = ProcessHelp.GetConsoleOutput("locate", {"-b", "-l", "20", "\" + fileName})

                          For Each line In output.SplitLinesNoEmpty
                              If File.Exists(line) AndAlso line.FileName = fileName Then
                                  paths.Add(line)
                              End If
                          Next
                      End If

                      If paths.Count = 0 Then
                          Dim found = Package.FindEverywhere(fileName)

                          If found <> "" Then
                              paths.Add(found)
                          End If
                      End If
                  End Sub

        Await Task.Run(run)

        If IsDisposed Then
            Exit Sub
        End If

        If paths.Count > 0 Then
            Using td As New TaskDialog(Of String)
                td.Title = "Choose Path"

                For Each path In paths.Distinct.Take(10)
                    td.AddCommand("", path, path)
                Next

                If td.Show.FileExists Then
                    CurrentPackage.SetStoredPath(td.SelectedValue)
                    ShowActivePackage()
                ElseIf td.SelectedValue <> "" Then
                    MsgError("File not found", td.SelectedValue)
                End If
            End Using
        Else
            MsgInfo("Nothing found.")
        End If
    End Sub

    Sub miEditVersion_Click(sender As Object, e As EventArgs) Handles miEditVersion.Click
        tsbVersion.PerformClick()
    End Sub

    Sub miAutoUpdate_Click(sender As Object, e As EventArgs) Handles miAutoUpdate.Click
        Dim url = CurrentPackage.DownloadURL

        If url = "" Then
            Exit Sub
        End If

        If url.Contains("mediafire") Then
            MsgError("The auto update feature does currently not support MediaFire.")
            Exit Sub
        End If

        If MsgQuestion("Experimental feature not working for all tools, continue?") = DialogResult.OK Then
            ToolUpdate = New ToolUpdate(CurrentPackage, Me)
            ToolUpdate.Update()
        End If
    End Sub

    Sub miEditChangelog_Click(sender As Object, e As EventArgs) Handles miEditChangelog.Click
        Dim path = IO.Path.Combine(Folder.Startup, "..", "Changelog.md")

        If File.Exists(path) Then
            g.ShellExecute(path)
        End If
    End Sub

    Sub UpdateUI() Implements IUpdateUI.UpdateUI
        ShowActivePackage()
        DoEvents()
    End Sub

    Sub miHelp_Click(sender As Object, e As EventArgs) Handles miHelp.Click
        tsbHelp.PerformClick()
    End Sub

    Sub miDownload_Click(sender As Object, e As EventArgs) Handles miDownload.Click
        tsbDownload.PerformClick()
    End Sub

    Sub miWebsite_Click(sender As Object, e As EventArgs) Handles miWebsite.Click
        tsbWebsite.PerformClick()
    End Sub

    Sub miExplore_Click(sender As Object, e As EventArgs) Handles miExplore.Click
        tsbExplore.PerformClick()
    End Sub

    Sub miLaunch_Click(sender As Object, e As EventArgs) Handles miLaunch.Click
        tsbLaunch.PerformClick()
    End Sub

    Sub miCopyPath_Click(sender As Object, e As EventArgs) Handles miCopyPath.Click
        Clipboard.SetText(CurrentPackage.Path)
        MsgInfo("The path was copied to the clipboard.")
    End Sub

    Sub miPATHEnvVar_Click(sender As Object, e As EventArgs) Handles miPATHEnvVar.Click
        'crash report by user
        Try
            Dim pathTarget = EnvironmentVariableTarget.Process
            Dim dir = CurrentPackage.Directory
            Dim pathVar = If(Environment.GetEnvironmentVariable("path", pathTarget), "")
            Dim pathItems = pathVar.Split(New String() {Path.PathSeparator}, StringSplitOptions.RemoveEmptyEntries).ToList()

            Using td As New TaskDialog(Of String)
                td.Title = "Modify the process PATH environment variable"
                td.AddCommand("Add", $"Add {CurrentPackage.Name} to process PATH environment variable", "add")
                td.AddCommand("Remove", $"Remove {CurrentPackage.Name} from process PATH environment variable", "remove")

                Select Case td.Show
                    Case "add"
                        If pathItems.Contains(dir) Then
                            MsgError("Folder is already in PATH")
                        Else
                            pathItems.Add(dir)
                            Environment.SetEnvironmentVariable("path", String.Join(Path.PathSeparator, pathItems), pathTarget)
                            MsgInfo("Folder was added to PATH")
                        End If
                    Case "remove"
                        If pathItems.Contains(dir) Then
                            pathItems.Remove(dir)
                            Environment.SetEnvironmentVariable("path", String.Join(Path.PathSeparator, pathItems), pathTarget)
                            MsgInfo("Folder was removed from PATH")
                        Else
                            MsgError("Folder is not in PATH")
                        End If
                End Select
            End Using
        Catch ex As Exception
            g.ShowException(ex)
        End Try
    End Sub

    Sub miUpdateRequest_Click(sender As Object, e As EventArgs) Handles miUpdateRequest.Click
        g.ShowWikiPage("Tool-Update-Requests")
    End Sub
End Class
