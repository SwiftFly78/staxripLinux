
Imports StaxRip.UI
Imports Microsoft.VisualBasic

'Thread safe task dialog, the Eto dialog is created and shown on the UI thread when Show is called.
Public Class TaskDialog(Of T)
    Implements IDisposable

    Property CommandDefinitions As New List(Of CommandDefinition)
    Property ButtonDefinitions As New List(Of ButtonDefinition)
    Property SelectedValue As T
    Property SelectedText As String
    Property Title As String
    Property Text As String
    Property Timeout As Integer
    Property Symbol As Symbol
    Property Content As String
    Property ExpandedContent As String
    Property Icon As TaskIcon
    Property Owner As Window
    Property ShowCopyButton As Boolean
    Property InputText As String
    Property CheckBoxText As String
    Property CheckBoxChecked As Boolean
    Property MenuItems As New List(Of Object)
    Property MenuValue As Object

    Private Dialog As TaskDialogForm
    Private Disposed As Boolean

    Sub New()
    End Sub

    Sub New(defaultValue As T)
        SelectedValue = defaultValue
    End Sub

    Sub New(defaultText As String, defaultValue As T)
        Me.New(defaultValue)
        SelectedText = defaultText
    End Sub

    ReadOnly Property IsDisposingOrDisposed As Boolean
        Get
            Return Disposed
        End Get
    End Property

    Sub AddCommand(value As T)
        AddCommand(value.ToString, Nothing, value)
    End Sub

    Sub AddCommand(text As String, Optional value As T = Nothing, Optional backColor As ColorHSL? = Nothing, Optional foreColor As ColorHSL? = Nothing)
        AddCommand(text, Nothing, value, backColor, foreColor)
    End Sub

    Sub AddCommand(text As String, description As String, value As T, Optional backColor As ColorHSL? = Nothing, Optional foreColor As ColorHSL? = Nothing)
        If value Is Nothing Then
            value = CType(CObj(text), T)
        End If

        CommandDefinitions.Add(New CommandDefinition With {.Text = text, .Description = description, .Value = value})
    End Sub

    Sub AddCommands(values As IEnumerable(Of T))
        For Each i In values
            AddCommand(i)
        Next
    End Sub

    Sub AddButton(text As String, Optional timeoutButton As Boolean = False)
        AddButton(text, CType(CObj(text), T), timeoutButton)
    End Sub

    Sub AddButton(value As T, Optional timeoutButton As Boolean = False)
        AddButton(value.ToString, value, timeoutButton)
    End Sub

    Sub AddButton(text As String, value As T, Optional timeoutButton As Boolean = False)
        ButtonDefinitions.Add(New ButtonDefinition With {.Text = text, .Value = value, .TimeoutButton = timeoutButton})
    End Sub

    'a button that deletes or overwrites something, it's not the default button
    Sub AddDestructiveButton(text As String, value As T)
        ButtonDefinitions.Add(New ButtonDefinition With {.Text = text, .Value = value, .Destructive = True})
    End Sub

    Sub AddButtons(values As IEnumerable(Of T))
        For Each i In values
            AddButton(i)
        Next
    End Sub

    WriteOnly Property Buttons As TaskButton
        Set(value As TaskButton)
            For Each i In {TaskButton.OK, TaskButton.Yes, TaskButton.No,
                TaskButton.Cancel, TaskButton.Retry, TaskButton.Close}

                If value.HasFlag(i) Then
                    AddButton(i.ToString, CType(CObj(GetDialogResultFromButton(i)), T))
                End If
            Next
        End Set
    End Property

    Shared Function GetDialogResultFromButton(button As TaskButton) As DialogResult
        Select Case button
            Case TaskButton.OK
                Return DialogResult.Ok
            Case TaskButton.Cancel, TaskButton.Close
                Return DialogResult.Cancel
            Case TaskButton.Yes
                Return DialogResult.Yes
            Case TaskButton.No
                Return DialogResult.No
            Case TaskButton.Retry
                Return DialogResult.Retry
            Case Else
                Return DialogResult.None
        End Select
    End Function

    Shared Function GetIconName(icon As TaskIcon) As String
        Select Case icon
            Case TaskIcon.Warning
                Return Styling.GetIconName("dialog-warning", "dialog-warning-symbolic")
            Case TaskIcon.Error
                Return Styling.GetIconName("dialog-error", "dialog-error-symbolic")
            Case TaskIcon.Question
                Return Styling.GetIconName("dialog-question", "dialog-question-symbolic")
            Case Else
                Return Styling.GetIconName("dialog-information", "dialog-information-symbolic")
        End Select
    End Function

    Function GetText() As String
        Dim ret = Title

        If Content <> "" Then
            ret += BR2 + Content
        End If

        If ExpandedContent <> "" Then
            ret += BR2 + ExpandedContent
        End If

        Return ret
    End Function

    Sub Close()
        UIBeginInvoke(Sub() Dialog?.Close())
    End Sub

    Function Show() As T
        UIInvoke(AddressOf ShowInternal)
        Return SelectedValue
    End Function

    Private Sub ShowInternal()
        If TypeOf SelectedValue Is DialogResult AndAlso SelectedValue.Equals(DialogResult.None) Then
            If ButtonDefinitions.Any(Function(i) i.Value.Equals(DialogResult.No)) Then
                SelectedValue = CType(CObj(DialogResult.No), T)
            End If

            If ButtonDefinitions.Any(Function(i) i.Value.Equals(DialogResult.Cancel)) Then
                SelectedValue = CType(CObj(DialogResult.Cancel), T)
            End If
        End If

        Dialog = New TaskDialogForm
        Dialog.Title = If(Text <> "", Text, g.DefaultCommands.GetApplicationDetails())

        Dim fh = FontHeight
        Dim maxTextWidth = fh * 45
        Dim lines = (Title & BR & Content).Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)

        If lines.Length > 0 Then
            Dim longest = lines.Max(Function(x) MeasureTextWidth(x))
            maxTextWidth = Math.Max(maxTextWidth, Math.Min(longest + fh, fh * 75))
        End If

        If CommandDefinitions.Count > 0 Then
            maxTextWidth = Math.Max(maxTextWidth, fh * 35)
        End If

        Dim layout As New DynamicLayout With {.Padding = New Padding(fh), .DefaultSpacing = New Size(fh \ 2, fh \ 2)}
        Dim headerTitle = If(String.IsNullOrWhiteSpace(Title), Content, Title)
        Dim showContent = Not String.IsNullOrWhiteSpace(Title) AndAlso Content <> ""

        Dim titleLabel As New Label With {
            .Text = headerTitle,
            .Wrap = WrapMode.Word,
            .Width = maxTextWidth,
            .Font = If(showContent OrElse ExpandedContent <> "" OrElse CommandDefinitions.Count > 0,
                FontManager.GetDefaultFont(2, FontStyle.Bold), FontManager.GetDefaultFont(1))
        }

        If Icon <> TaskIcon.None Then
            Dim iconControl = Styling.CreateIconControl(GetIconName(Icon), CInt(fh * 2.4))
            layout.AddRow(New TableLayout(New TableRow(New TableCell(iconControl), New TableCell(titleLabel, True))) With {.Spacing = New Size(fh, 0)})
        Else
            layout.AddRow(titleLabel)
        End If

        If showContent Then
            If Content.Length > 2500 OrElse Content.Split(CChar(vbLf)).Length > 30 Then
                Dim ta As New TextArea With {
                    .Text = Content,
                    .ReadOnly = True,
                    .Wrap = True,
                    .Font = FontManager.GetCodeFont(-1),
                    .Size = New Size(maxTextWidth, fh * 20)
                }
                layout.AddRow(ta)
            Else
                layout.AddRow(New Label With {.Text = Content, .Wrap = WrapMode.Word, .Width = maxTextWidth})
            End If
        End If

        If ExpandedContent <> "" Then
            Dim details As New TextArea With {
                .Text = ExpandedContent,
                .ReadOnly = True,
                .Wrap = False,
                .Font = FontManager.GetCodeFont(-1),
                .Size = New Size(maxTextWidth, fh * 14)
            }

            layout.AddRow(New Expander With {.Header = New Label With {.Text = "Details"}, .Content = details, .Expanded = False})
        End If

        Dim inputBox As TextBox = Nothing

        If InputText IsNot Nothing Then
            inputBox = New TextBox With {.Text = InputText}
            layout.AddRow(inputBox)
        End If

        Dim menuDropDown As DropDown = Nothing

        If MenuItems.Count > 0 Then
            menuDropDown = New DropDown

            For Each item In MenuItems
                menuDropDown.Items.Add(New ListItem With {.Text = item.ToString, .Tag = item})
            Next

            Dim index = MenuItems.IndexOf(MenuValue)
            menuDropDown.SelectedIndex = If(index < 0, 0, index)
            layout.AddRow(menuDropDown)
        End If

        Dim timeoutButton As Button = Nothing
        Dim firstCommand As Button = Nothing

        For Each command In CommandDefinitions
            Dim cmd = command
            Dim button As New Button With {.Text = cmd.Text, .Font = FontManager.GetDefaultFont(1)}
            Styling.SetButtonContent(button, Nothing, Styling.GetIconName("go-next-symbolic"), True)

            Dim family = TryCast(CObj(cmd.Value), FontFamily)

            If family IsNot Nothing Then
                button.Font = New Font(family, FontManager.SystemFontSize + 1)
            End If

            AddHandler button.Click, Sub()
                                         SelectedText = If(cmd.Text = "", cmd.Description, cmd.Text)
                                         SelectedValue = cmd.Value
                                         Dialog.Close(DialogResult.Ok)
                                     End Sub

            If firstCommand Is Nothing Then
                firstCommand = button
            End If

            If cmd.Description <> "" Then
                Dim stack As New StackLayout With {.Orientation = Orientation.Vertical, .HorizontalContentAlignment = HorizontalAlignment.Stretch, .Spacing = 2}
                stack.Items.Add(button)
                Dim description As New Label With {.Text = cmd.Description, .Wrap = WrapMode.Word, .Width = maxTextWidth}
                description.AddStyleClass("dim-label")
                stack.Items.Add(description)
                layout.AddRow(stack)
            Else
                layout.AddRow(button)
            End If
        Next

        Dim checkBox As CheckBox = Nothing
        Dim bottomRow As New StackLayout With {.Orientation = Orientation.Horizontal, .Spacing = fh \ 2, .VerticalContentAlignment = VerticalAlignment.Center}

        If CheckBoxText <> "" Then
            checkBox = New CheckBox With {.Text = CheckBoxText, .Checked = CheckBoxChecked}
            bottomRow.Items.Add(checkBox)
        End If

        If ShowCopyButton Then
            Dim copyButton As New ButtonEx With {.Text = "Copy Message", .IconName = Styling.GetIconName("edit-copy-symbolic"), .MinimumSize = Size.Empty}
            copyButton.AddStyleClass("flat")
            AddHandler copyButton.Click, Sub() Clipboard.Instance.Text = GetText()
            bottomRow.Items.Add(copyButton)
        End If

        bottomRow.Items.Add(New StackLayoutItem(Nothing, True))

        For Each bd In ButtonDefinitions
            Dim def = bd
            Dim button As New Button With {.Text = def.Text}

            AddHandler button.Click, Sub()
                                         SelectedText = def.Text
                                         SelectedValue = def.Value

                                         If inputBox IsNot Nothing Then
                                             InputText = inputBox.Text
                                         End If

                                         Dialog.Close(If(TypeOf CObj(def.Value) Is DialogResult, DirectCast(CObj(def.Value), DialogResult), DialogResult.Ok))
                                     End Sub

            If def.TimeoutButton AndAlso timeoutButton Is Nothing Then
                timeoutButton = button
            End If

            If def.Destructive Then
                button.AddStyleClass("destructive-action")
            ElseIf Dialog.DefaultButton Is Nothing AndAlso (def.Text = "OK" OrElse def.Text = "Yes") Then
                Dialog.DefaultButton = button
                button.AddStyleClass("suggested-action")
            End If

            If def.Value IsNot Nothing AndAlso def.Value.Equals(DialogResult.Cancel) Then
                Dialog.AbortButton = button

                'Enter cancels destructive actions instead of confirming them
                If Dialog.DefaultButton Is Nothing AndAlso ButtonDefinitions.Any(Function(x) x.Destructive) Then
                    Dialog.DefaultButton = button
                End If
            End If

            bd.Button = button
            bottomRow.Items.Add(button)
        Next

        If bottomRow.Items.Count > 1 Then
            layout.AddRow(bottomRow)
        End If

        Dialog.Content = layout

        If Timeout > 0 Then
            Dim button = If(timeoutButton, Dialog.DefaultButton)
            Dim originalText = If(button?.Text, "")
            Dim originalTitle = Dialog.Title
            Dim counter = Timeout
            Dim timer As New UITimer With {.Interval = 1}

            Dim update = Sub()
                             If button IsNot Nothing Then
                                 button.Text = $"{originalText} ({counter})"
                             Else
                                 Dialog.Title = $"{originalTitle} ({counter})"
                             End If
                         End Sub

            update()

            AddHandler timer.Elapsed, Sub()
                                          counter -= 1

                                          If counter <= 0 Then
                                              timer.Stop()

                                              If button IsNot Nothing Then
                                                  button.Text = originalText
                                                  button.PerformClick()
                                              Else
                                                  Dialog.Close(DialogResult.Cancel)
                                              End If
                                          Else
                                              update()
                                          End If
                                      End Sub

            AddHandler Dialog.Closed, Sub() timer.Stop()
            timer.Start()
        End If

        AddHandler Dialog.Shown, Sub()
                                     If inputBox IsNot Nothing Then
                                         inputBox.Focus()
                                         inputBox.SelectAll()
                                     ElseIf Dialog.DefaultButton IsNot Nothing Then
                                         Dialog.DefaultButton.Focus()
                                     ElseIf firstCommand IsNot Nothing Then
                                         firstCommand.Focus()
                                     End If
                                 End Sub

        Dim ownerWindow = If(Owner, GetOwnerWindow())

        If ownerWindow Is Nothing Then
            Dialog.ShowModal()
        Else
            Dialog.ShowModal(ownerWindow)
        End If

        If inputBox IsNot Nothing AndAlso SelectedValue IsNot Nothing AndAlso Not SelectedValue.Equals(DialogResult.Cancel) Then
            InputText = inputBox.Text
        End If

        If checkBox IsNot Nothing Then
            CheckBoxChecked = checkBox.Checked.GetValueOrDefault()
        End If

        If menuDropDown IsNot Nothing AndAlso menuDropDown.SelectedIndex > -1 Then
            MenuValue = MenuItems(menuDropDown.SelectedIndex)
        End If

        Dialog.Dispose()
        Dialog = Nothing
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
        Disposed = True
    End Sub

    Public Class CommandDefinition
        Property Text As String
        Property Description As String
        Property Value As T
    End Class

    Public Class ButtonDefinition
        Property Text As String
        Property Value As T
        Property TimeoutButton As Boolean
        Property Destructive As Boolean
        Property Button As Button
    End Class
End Class

Public Class TaskDialogForm
    Inherits Dialog(Of DialogResult)

    Sub New()
        Resizable = True
        ShowInTaskbar = False
        Minimizable = False
        Maximizable = False
        Icon = g.Icon
    End Sub
End Class

Public Enum TaskIcon
    None
    Info
    Warning
    Question
    [Error]
    Shield
End Enum

Public Enum TaskButton
    None = 0
    OK = 1
    Yes = 2
    No = 4
    Cancel = 8
    Retry = 16
    Close = 32
    OkCancel = OK Or Cancel
    YesNo = Yes Or No
    YesNoCancel = YesNo Or Cancel
    RetryCancel = Retry Or Cancel
End Enum
