
Imports StaxRip.UI

Public Class SourceFilesForm
    Inherits DialogBase

    Public WithEvents lb As ListBoxEx
    Public WithEvents bnDown As ButtonEx
    Public WithEvents bnUp As ButtonEx
    Public WithEvents bnRemove As ButtonEx
    Public WithEvents bnAddFiles As ButtonEx
    Public WithEvents bnAddFolder As ButtonEx
    Public WithEvents bnCancel As ButtonEx
    Public WithEvents bnOK As ButtonEx

    Private Sub InitializeComponent()
        lb = New ListBoxEx With {.AllowDrop = True}
        bnAddFiles = New ButtonEx("Add Files...")
        bnAddFolder = New ButtonEx("Add Folder...")
        bnRemove = New ButtonEx("Remove")
        bnUp = New ButtonEx("Up")
        bnDown = New ButtonEx("Down")
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        lb.UpButton = bnUp
        lb.DownButton = bnDown
        lb.RemoveButton = bnRemove

        Dim buttons As New TableLayout(
            Row(bnAddFiles), Row(bnAddFolder), Row(bnRemove), Row(bnUp), Row(bnDown),
            New TableRow With {.ScaleHeight = True}) With {.Spacing = DefaultSpacing}

        Content = DialogContent(ButtonRow(bnOK, bnCancel), ScaledRow(Scaled(lb), buttons))

        DefaultButton = bnOK
        AbortButton = bnCancel
        Text = $"Source Files - {g.DefaultCommands.GetApplicationDetails()}"
    End Sub

    Property IsMerge As Boolean

    Sub New()
        MyBase.New()
        InitializeComponent()

        ScaleClientSize(42, 25)
        SetMinimumSize(21, 15)
    End Sub

    Sub ShowOpenFileDialog()
        Using dialog As New OpenFileDialog
            dialog.Multiselect = True
            dialog.SetFilter(FileTypes.Video)
            dialog.SetInitDir(s.LastSourceDir)

            If dialog.ShowDialog() = DialogResult.OK Then
                s.LastSourceDir = dialog.FileName.Dir
                lb.Items.AddRange(dialog.FileNames.Sort.ToArray)
                lb.SelectedIndex = lb.Items.Count - 1
            End If
        End Using
    End Sub

    Sub bnAdd_Click() Handles bnAddFiles.Click
        ShowOpenFileDialog()
    End Sub

    Sub bnAddFolder_Click() Handles bnAddFolder.Click
        Using dialog As New FolderBrowserDialog
            If dialog.ShowDialog = DialogResult.OK Then
                'folders that can't be read, like lost+found, are skipped
                Dim options As New EnumerationOptions With {.AttributesToSkip = 0, .IgnoreInaccessible = True}
                Dim subfolders = Directory.GetDirectories(dialog.SelectedPath, "*", options)

                If subfolders.Length > 0 Then
                    If MsgQuestion("Include sub folders?", TaskButton.YesNo) = DialogResult.Yes Then
                        options.RecurseSubdirectories = True
                    End If
                End If

                lb.Items.AddRange(Directory.GetFiles(dialog.SelectedPath, "*", options).Where(Function(val) FileTypes.Video.Contains(val.Ext)).OrderBy(Function(x) x, New StringLogicalComparer()).ToArray)
                lb.SelectedIndex = lb.Items.Count - 1
            End If
        End Using
    End Sub

    Protected Overrides Sub OnClosing(args As ComponentModel.CancelEventArgs)
        MyBase.OnClosing(args)

        If DialogResult = DialogResult.OK Then
            Dim files = GetFiles()

            If Not g.VerifySource(files) Then
                args.Cancel = True
            End If
        End If
    End Sub

    Function GetFiles() As IEnumerable(Of String)
        Return lb.Items.OfType(Of String)
    End Function

    Sub lb_DragDrop(sender As Object, e As DragEventArgs) Handles lb.DragDrop
        Dim items = FormBase.GetDroppedFiles(e)

        If Not items.NothingOrEmpty() Then
            Array.Sort(items)
            lb.Items.AddRange(items.Where(Function(val) File.Exists(val)).ToArray())
        End If
    End Sub

    Sub lb_DragOver(sender As Object, e As DragEventArgs) Handles lb.DragOver
        e.Effects = DragEffects.Copy
    End Sub
End Class
