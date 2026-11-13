Imports System.Management.Automation
Imports System.Management.Automation.Runspaces

Imports StaxRip.UI

'runs PowerShell commands in StaxRip with $p, $s and $g of the running instance, the variables of a session are kept
Public Class PowerShellConsoleForm
    Inherits FormBase

    Public rtbOutput As RichTextBoxEx
    Public WithEvents tbInput As TextBox

    Private ReadOnly Runspace As Runspace
    Private ReadOnly History As New List(Of String)
    Private HistoryIndex As Integer

    Sub New()
        rtbOutput = New RichTextBoxEx With {.ReadOnly = True, .Font = FontManager.GetCodeFont()}
        tbInput = New TextBox With {
            .Font = FontManager.GetCodeFont(),
            .PlaceholderText = "PowerShell command, Enter runs it, Up and Down browse the history"}

        Content = New TableLayout(ScaledRow(rtbOutput), Row(tbInput)) With {
            .Spacing = DefaultSpacing,
            .Padding = New Padding(FH(0.3))}

        RestoreClientSize(50, 30)
        Text = $"PowerShell Console - {g.DefaultCommands.GetApplicationDetails()}"

        Runspace = RunspaceFactory.CreateRunspace()
        Runspace.ThreadOptions = PSThreadOptions.UseCurrentThread
        Runspace.Open()
        RunCode(PowerShell.InitCode, False)

        rtbOutput.Text = "$p is the current project, $s the settings and $g the global object of StaxRip," + BR +
            "for example: $p.SourceFile, $g.MainForm.Text, [StaxRip.MainModule]::MsgInfo(""Hello"")" + BR2
    End Sub

    Sub tbInput_KeyDown(sender As Object, e As KeyEventArgs) Handles tbInput.KeyDown
        If e.KeyData = Keys.Enter Then
            e.Handled = True
            RunInput()
        ElseIf e.KeyData = Keys.Up AndAlso History.Count > 0 Then
            e.Handled = True
            HistoryIndex = Math.Max(0, HistoryIndex - 1)
            tbInput.Text = History(HistoryIndex)
        ElseIf e.KeyData = Keys.Down AndAlso History.Count > 0 Then
            e.Handled = True
            HistoryIndex = Math.Min(History.Count, HistoryIndex + 1)
            tbInput.Text = If(HistoryIndex < History.Count, History(HistoryIndex), "")
        End If
    End Sub

    Sub RunInput()
        Dim command = tbInput.Text.Trim()
        If command = "" Then Exit Sub

        History.Remove(command)
        History.Add(command)
        HistoryIndex = History.Count
        tbInput.Text = ""
        rtbOutput.Append("PS> " + command + BR, True)
        rtbOutput.Append(RunCode(command, True), True)
    End Sub

    'the variables are set before each command because another project can be opened meanwhile
    Function RunCode(code As String, formatOutput As Boolean) As String
        If code = "" Then Return ""

        Runspace.SessionStateProxy.SetVariable("p", p)
        Runspace.SessionStateProxy.SetVariable("s", s)
        Runspace.SessionStateProxy.SetVariable("g", g)

        Using pipeline = Runspace.CreatePipeline()
            pipeline.Commands.AddScript(code)

            If formatOutput Then
                pipeline.Commands.Add("Out-String")
            End If

            Try
                Dim result = String.Join("", pipeline.Invoke().Where(Function(i) i IsNot Nothing).Select(Function(i) i.ToString()))

                For Each errorRecord In pipeline.Error.ReadToEnd()
                    result += errorRecord.ToString() + BR
                Next

                Return If(result.Trim() = "", BR, result.TrimEnd() + BR + BR)
            Catch ex As Exception
                Return If(TypeOf ex Is RuntimeException, ex.Message, ex.ToString()) + BR + BR
            End Try
        End Using
    End Function

    Protected Overrides Sub OnClosed(e As EventArgs)
        Runspace.Dispose()
        MyBase.OnClosed(e)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        tbInput.Focus()
    End Sub
End Class
