
Imports System.Text.RegularExpressions

Namespace UI
    'Rich text control with a WinForms RichTextBox like API on top of Eto RichTextArea
    Public Class RichTextBoxEx
        Inherits RichTextArea
        Implements IContextMenuOwner

        Private ContextMenuValue As ContextMenuStripEx

        Property BlockPaint As Boolean
        Property ScrollBars As Object
        Property DetectUrls As Boolean
        Property BorderColor As Color
        Property BorderFocusedColor As Color
        Property BackReadonlyColor As Color

        Event AfterThemeApplied(text As String, theme As Theme)

        Sub New()
            MyClass.New(True, False)
        End Sub

        Sub New(createMenu As Boolean, transparent As Boolean)
            MyBase.New()
            Font = FontManager.GetCodeFont()
            Wrap = False
            AcceptsTab = False

            If createMenu Then
                InitMenu()
            End If
        End Sub

        Property ContextMenuStrip As ContextMenuStripEx Implements IContextMenuOwner.ContextMenuStrip
            Get
                Return ContextMenuValue
            End Get
            Set(value As ContextMenuStripEx)
                ContextMenuValue = value
                ContextMenu = value
            End Set
        End Property

        Property WordWrap As Boolean
            Get
                Return Wrap
            End Get
            Set(value As Boolean)
                Wrap = value
            End Set
        End Property

        Property Multiline As Boolean = True

        Property BackColor As Color
            Get
                Return BackgroundColor
            End Get
            Set(value As Color)
                BackgroundColor = value
            End Set
        End Property

        Property ForeColor As Color
            Get
                Return TextColor
            End Get
            Set(value As Color)
                TextColor = value
            End Set
        End Property

        Property SelectionStart As Integer
            Get
                Return Selection.Start
            End Get
            Set(value As Integer)
                CaretIndex = Math.Max(0, Math.Min(value, TextLength))
            End Set
        End Property

        Property SelectionLength As Integer
            Get
                Return Math.Max(0, Selection.End - Selection.Start + 1)
            End Get
            Set(value As Integer)
                [Select](SelectionStart, value)
            End Set
        End Property

        Shadows Property SelectedText As String
            Get
                Return MyBase.SelectedText
            End Get
            Set(value As String)
                MyBase.SelectedText = If(value, "")
            End Set
        End Property

        Sub [Select](start As Integer, length As Integer)
            start = Math.Max(0, Math.Min(start, TextLength))
            length = Math.Max(0, Math.Min(length, TextLength - start))

            If length = 0 Then
                CaretIndex = start
            Else
                Selection = New Range(Of Integer)(start, start + length - 1)
            End If
        End Sub

        Sub ScrollToCaret()
            ScrollTo(New Range(Of Integer)(CaretIndex, CaretIndex))
        End Sub

        Sub AppendText(value As String)
            Append(value, True)
        End Sub

        Sub Clear()
            Text = ""
        End Sub

        'formats a range of the text
        Sub SetFormat(index As Integer, length As Integer, foreColor As Color?, backColor As Color?, bold As Boolean)
            If length <= 0 OrElse index < 0 OrElse index + length > TextLength Then
                Exit Sub
            End If

            Dim range = New Range(Of Integer)(index, index + length - 1)

            If foreColor.HasValue AndAlso foreColor.Value.A > 0 Then
                Buffer.SetForeground(range, foreColor.Value)
            End If

            If backColor.HasValue AndAlso backColor.Value.A > 0 Then
                Buffer.SetBackground(range, backColor.Value)
            End If

            If bold Then
                Buffer.SetBold(range, True)
            End If
        End Sub

        Function Find(value As String, Optional start As Integer = 0) As Integer
            If value = "" Then
                Return -1
            End If

            Dim index = Text.IndexOf(value, Math.Max(0, start), StringComparison.OrdinalIgnoreCase)

            If index > -1 Then
                [Select](index, value.Length)
                ScrollTo(New Range(Of Integer)(index, index + value.Length - 1))
            End If

            Return index
        End Function

        Sub ClearAllFormatting()
            Dim value = Text
            Text = value
        End Sub

        Sub InitMenu()
            Dim cms As New ContextMenuStripEx()

            Dim cutItem = cms.Add("Cut", Sub()
                                            Clipboard.Instance.Text = SelectedText
                                            SelectedText = ""
                                        End Sub)
            cutItem.ShortcutKeyDisplayString = "Ctrl+X"

            Dim copyItem = cms.Add("Copy", Sub() Clipboard.Instance.Text = SelectedText)
            copyItem.ShortcutKeyDisplayString = "Ctrl+C"

            Dim pasteItem = cms.Add("Paste", Sub() SelectedText = Clipboard.Instance.Text)
            pasteItem.ShortcutKeyDisplayString = "Ctrl+V"

            cms.Add("Copy Everything", Sub() Clipboard.Instance.Text = Text)

            AddHandler cms.Opening, Sub()
                                        cutItem.Visible = SelectionLength > 0 AndAlso Not [ReadOnly]
                                        copyItem.Visible = SelectionLength > 0
                                        pasteItem.Visible = Clipboard.Instance.ContainsText AndAlso Not [ReadOnly]
                                    End Sub

            ContextMenuStrip = cms
        End Sub
    End Class

    Public Class CommandLineRichTextBox
        Inherits RichTextBoxEx

        Property LastCommandLine As String

        Sub New()
            MyBase.New()
            Wrap = True
        End Sub

        Sub SetText(commandLine As String)
            If commandLine = LastCommandLine Then
                Exit Sub
            End If

            If commandLine = "" Then
                Text = ""
                LastCommandLine = ""
                Exit Sub
            End If

            Format(commandLine)
        End Sub

        Sub Format(commandLine As String)
            Text = commandLine

            If LastCommandLine <> "" Then
                Dim selStart = GetCompareIndex(commandLine, LastCommandLine)
                Dim selEnd = commandLine.Length - GetCompareIndex(commandLine.Reverse(), LastCommandLine.Reverse())

                If selEnd > selStart AndAlso selEnd - selStart < commandLine.Length - 1 Then
                    While selStart > 0 AndAlso selStart + 1 < commandLine.Length AndAlso
                        Not commandLine(selStart - 1) + commandLine(selStart) = " -"

                        selStart -= 1
                    End While

                    If selEnd - selStart < 25 AndAlso selEnd - selStart <> commandLine.Length Then
                        SetFormat(selStart, selEnd - selStart, Nothing, Nothing, True)
                    End If
                End If
            End If

            If s.CommandLineHighlighting Then
                Dim th = ThemeManager.CurrentTheme.General.Controls.CommandLineRichTextBox
                Dim matches = Regex.Matches(commandLine, "(?<=\s|^)(--?\w[^\s=]*)([\s=]((?!--?\D)[^""\s]+|""[^""\n]*"")?)?", RegexOptions.IgnoreCase)

                For Each m As Match In matches
                    SetFormat(m.Groups(1).Index, m.Groups(1).Length, th.ParameterForeColor, Nothing, th.ParameterFontStyles.Contains(FontStyle.Bold))

                    If m.Groups.Count > 3 AndAlso m.Groups(3).Success Then
                        SetFormat(m.Groups(3).Index, m.Groups(3).Length, th.ParameterValueForeColor, Nothing, th.ParameterValueFontStyles.Contains(FontStyle.Bold))
                    End If
                Next
            End If

            CaretIndex = commandLine.Length
            LastCommandLine = commandLine
        End Sub

        Function GetCompareIndex(a As String, b As String) As Integer
            For x = 0 To a.Length - 1
                If x > b.Length - 1 OrElse x > a.Length - 1 OrElse a(x) <> b(x) Then
                    Return x
                End If
            Next

            Return 0
        End Function

        Private LastWidth As Integer

        Protected Overrides Sub OnSizeChanged(e As EventArgs)
            MyBase.OnSizeChanged(e)

            If Width <> LastWidth Then
                LastWidth = Width
                UpdateHeight()
            End If
        End Sub

        Sub UpdateHeight()
            'the width is unknown before the layout is done
            If Width <= FontHeight * 2 Then
                Exit Sub
            End If

            Dim lines = Math.Max(1, CInt(Math.Ceiling(MeasureTextWidth(Text, Font) / (Width - FontHeight))))
            Dim h = CInt(Font.LineHeight * Math.Min(lines, 9) + FontHeight * 0.6)
            Height = h
        End Sub
    End Class
End Namespace
