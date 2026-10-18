
Imports StaxRip.UI

Public Class MacroEditorControl
    Inherits Panel

    Public WithEvents TabControl As TabControl
    Public WithEvents tpEdit As TabPage
    Public WithEvents tpPreview As TabPage
    Public WithEvents tpDefaults As TabPage
    Public WithEvents gb As GroupBox
    Public WithEvents rtbEdit As RichTextBoxEx
    Public WithEvents rtbPreview As RichTextBoxEx
    Public WithEvents rtbDefaults As RichTextBoxEx
    Public WithEvents llMacros As ButtonLabel
    Public WithEvents llHelp As ButtonLabel
    Public TipProvider As New TipProvider

    Private ReadOnly HelpPaths As New List(Of String)

    Property SpecialMacrosFunction As Func(Of String, String)
    Property AutoWrap As Boolean = True

    Private IsCommandLineMode As Boolean

    Sub New()
        rtbEdit = New RichTextBoxEx
        rtbPreview = New RichTextBoxEx With {.ReadOnly = True}
        rtbDefaults = New RichTextBoxEx With {.ReadOnly = True}
        tpEdit = New TabPage(rtbEdit) With {.Text = "Edit"}
        tpPreview = New TabPage(rtbPreview) With {.Text = "Preview"}
        tpDefaults = New TabPage(rtbDefaults) With {.Text = "Defaults"}
        TabControl = New TabControl
        TabControl.Pages.Add(tpEdit)
        TabControl.Pages.Add(tpPreview)
        TabControl.Pages.Add(tpDefaults)
        llMacros = New ButtonLabel With {.Text = "Macros"}
        llHelp = New ButtonLabel With {.Text = "Help", .Visible = False}

        gb = New GroupBox With {
            .Content = New TableLayout(
                New TableRow(TabControl) With {.ScaleHeight = True},
                New TableRow(HRow(llMacros, llHelp))) With {.Spacing = DefaultSpacing}}

        Content = gb
    End Sub

    Property Text As String
        Get
            Return gb.Text
        End Get
        Set(value As String)
            gb.Text = value
        End Set
    End Property

    Property Value As String
        Get
            Return rtbEdit.Text.FixBreak
        End Get
        Set(value As String)
            rtbEdit.Text = value

            If IsCommandLineMode Then
                EditTextChanged(Nothing, Nothing)
            End If
        End Set
    End Property

    Sub SetCommandLineDefaults()
        AutoWrap = True
        IsCommandLineMode = True
        AddHandler rtbEdit.TextChanged, AddressOf EditTextChanged
    End Sub

    Sub SetScriptDefaults()
        AutoWrap = False
        rtbEdit.WordWrap = False
        rtbPreview.WordWrap = False
        rtbDefaults.WordWrap = False
    End Sub

    Sub SetMacroDefaults()
        AutoWrap = False
        rtbEdit.WordWrap = False
        rtbPreview.WordWrap = False
        rtbDefaults.WordWrap = False
    End Sub

    Sub UpdateWrapMode(rtb As RichTextBoxEx)
        If AutoWrap Then
            rtb.WordWrap = Not rtb.Text.FixBreak.Contains(BR)
        End If
    End Sub

    Sub TabControl_SelectedIndexChanged(sender As Object, e As EventArgs) Handles TabControl.SelectedIndexChanged
        If TabControl.SelectedPage Is tpPreview Then
            UpdatePreview()
        End If
    End Sub

    Sub UpdatePreview()
        If SpecialMacrosFunction IsNot Nothing Then
            rtbPreview.Text = SpecialMacrosFunction.Invoke(rtbEdit.Text)
        Else
            rtbPreview.Text = Macro.Expand(rtbEdit.Text)
        End If
    End Sub

    Sub rtbPreview_TextChanged(sender As Object, e As EventArgs) Handles rtbPreview.TextChanged
        UpdateWrapMode(rtbPreview)
    End Sub

    Sub rtbEdit_TextChanged(sender As Object, e As EventArgs) Handles rtbEdit.TextChanged
        UpdateWrapMode(rtbEdit)
    End Sub

    Sub rtbDefaults_TextChanged(sender As Object, e As EventArgs) Handles rtbDefaults.TextChanged
        UpdateWrapMode(rtbDefaults)
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        UpdateWrapMode(rtbEdit)
        UpdateWrapMode(rtbPreview)

        If rtbDefaults.Text = "" AndAlso TabControl.Pages.Contains(tpDefaults) Then
            TabControl.Pages.Remove(tpDefaults)
        End If
    End Sub

    Sub llMacros_Click(sender As Object, e As EventArgs) Handles llMacros.Click
        MacrosForm.ShowDialogForm()
    End Sub

    Sub llHelp_Click(sender As Object, e As EventArgs) Handles llHelp.Click
        For Each path In HelpPaths
            g.ShellExecute(path)
        Next
    End Sub

    Sub EditTextChanged(sender As Object, e As EventArgs)
        Dim editText = Value
        HelpPaths.Clear()
        Dim caption = ""

        For Each pack In Package.Items.Values
            If editText.Contains(pack.Name) Then
                caption += ", " + pack.Name
                HelpPaths.Add(pack.HelpFileOrURL)
            End If
        Next

        If caption <> "" Then
            llHelp.Text = "Help (" + caption.Trim(", ".ToCharArray) + ")"
        End If

        llHelp.Visible = HelpPaths.Count > 0
    End Sub
End Class
