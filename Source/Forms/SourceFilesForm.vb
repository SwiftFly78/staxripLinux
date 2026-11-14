
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
    Public WithEvents cbMergeMethod As ComboBoxEx
    Private laMergeMethod As LabelEx

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

        laMergeMethod = New LabelEx With {.Text = "Merge with:", .Visible = False}
        cbMergeMethod = New ComboBoxEx With {
            .Visible = False,
            .ToolTip = "mkvmerge appends the files into an MKV file." + BR +
                "The concat demuxer of ffmpeg joins files with the same streams and keeps the container of the first file." + BR +
                "The concat protocol of ffmpeg joins the bytes of the files, for parts of an MPEG-TS file and for fragmented" + BR +
                "MP4 segments (M4S) with their init segment." + BR +
                "Automatic uses the concat protocol for TS and M4S files and mkvmerge for other files."}
        cbMergeMethod.Items.AddRange({"Automatic", "mkvmerge", "ffmpeg concat demuxer", "ffmpeg concat protocol"})
        cbMergeMethod.SelectedIndex = Math.Min(Math.Max(s.Storage.GetInt("merge method", 0), 0), 3)

        Dim buttons As New TableLayout(
            Row(bnAddFiles), Row(bnAddFolder), Row(bnRemove), Row(bnUp), Row(bnDown),
            New TableRow With {.ScaleHeight = True}) With {.Spacing = DefaultSpacing}

        Content = DialogContent(SplitRow({laMergeMethod, cbMergeMethod}, {bnOK, bnCancel}), ScaledRow(Scaled(lb), buttons))

        DefaultButton = bnOK
        AbortButton = bnCancel
        Text = $"Source Files - {g.DefaultCommands.GetApplicationDetails()}"
    End Sub

    Private IsMergeValue As Boolean

    Property IsMerge As Boolean
        Get
            Return IsMergeValue
        End Get
        Set(value As Boolean)
            IsMergeValue = value
            laMergeMethod.Visible = value
            cbMergeMethod.Visible = value
        End Set
    End Property

    ReadOnly Property MergeMethod As MergeMethod
        Get
            Return CType(Math.Max(cbMergeMethod.SelectedIndex, 0), MergeMethod)
        End Get
    End Property

    'M4S segments can only be merged
    ReadOnly Property FileTypes As String()
        Get
            Return If(IsMerge, StaxRip.FileTypes.Video.Concat({"m4s"}).ToArray, StaxRip.FileTypes.Video)
        End Get
    End Property

    Sub New()
        MyBase.New()
        InitializeComponent()

        ScaleClientSize(42, 25)
        SetMinimumSize(21, 15)
    End Sub

    Sub ShowOpenFileDialog()
        Using dialog As New OpenFileDialog
            dialog.Multiselect = True
            dialog.SetFilter(FileTypes)
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

                lb.Items.AddRange(Directory.GetFiles(dialog.SelectedPath, "*", options).Where(Function(val) FileTypes.Contains(val.Ext)).OrderBy(Function(x) x, New StringLogicalComparer()).ToArray)
                lb.SelectedIndex = lb.Items.Count - 1
            End If
        End Using
    End Sub

    Protected Overrides Sub OnClosing(args As ComponentModel.CancelEventArgs)
        MyBase.OnClosing(args)

        If DialogResult = DialogResult.OK Then
            Dim files = GetFiles()

            If IsMerge Then
                s.Storage.SetInt("merge method", cbMergeMethod.SelectedIndex)
            End If

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

Public Enum MergeMethod
    Automatic
    Mkvmerge
    FfmpegConcatDemuxer
    FfmpegConcatProtocol
End Enum
