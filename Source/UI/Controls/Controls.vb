
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks

Namespace UI
    Public Class ButtonEx
        Inherits Button
        Implements IContextMenuOwner

        Private TextValue As String = ""
        Private ShowMenuSymbolValue As Boolean
        Private SymbolValue As Symbol
        Private SymbolButtonValue As ButtonSymbol
        Private IconNameValue As String

        Property ClickAction As Action
        Property ContextMenuStrip As ContextMenuStripEx Implements IContextMenuOwner.ContextMenuStrip

        'closes the parent dialog with this result after the click handlers ran
        Property DialogResult As DialogResult = DialogResult.None

        Sub New()
            MyBase.New()
        End Sub

        Sub New(text As String)
            MyBase.New()
            Me.Text = text
        End Sub

        Shadows Property Text As String
            Get
                Return TextValue
            End Get
            Set(value As String)
                TextValue = If(value, "")
                UpdateText()
            End Set
        End Property

        Property ShowMenuSymbol As Boolean
            Get
                Return ShowMenuSymbolValue
            End Get
            Set(value As Boolean)
                ShowMenuSymbolValue = value
                UpdateText()
            End Set
        End Property

        Property Symbol As Symbol
            Get
                Return SymbolValue
            End Get
            Set(value As Symbol)
                SymbolValue = value
                UpdateText()
            End Set
        End Property

        Property SymbolButton As ButtonSymbol
            Get
                Return SymbolButtonValue
            End Get
            Set(value As ButtonSymbol)
                SymbolButtonValue = value
                UpdateText()
            End Set
        End Property

        'icon of the icon theme shown left of the text
        Property IconName As String
            Get
                Return IconNameValue
            End Get
            Set(value As String)
                IconNameValue = value
                UpdateText()
            End Set
        End Property

        Private FillContentValue As Boolean

        'the text starts at the left edge and the menu arrow sits at the right edge like in a combo box
        Property FillContent As Boolean
            Get
                Return FillContentValue
            End Get
            Set(value As Boolean)
                FillContentValue = value
                UpdateText()
            End Set
        End Property

        Sub UpdateText()
            Dim value = TextValue.Trim
            Dim iconName = If(IconNameValue <> "", IconNameValue, If(value = "", GetSymbolIconName(), Nothing))

            If value = "" AndAlso iconName = "" Then
                value = GetSymbolText()
            End If

            MyBase.Text = value

            'Eto ellipsizes button texts, so buttons in tight layouts get too narrow
            Dim label = TryCast(TryCast(ControlObject, Global.Gtk.Bin)?.Child, Global.Gtk.Label)
            If label IsNot Nothing Then label.Ellipsize = Pango.EllipsizeMode.None

            Styling.SetButtonContent(Me, iconName, ShowMenuSymbol OrElse SymbolButton = ButtonSymbol.Menu, FillContent OrElse TypeOf Me Is MenuButton)
        End Sub

        Function GetSymbolIconName() As String
            Select Case SymbolButton
                Case ButtonSymbol.None
                    Return Styling.GetIconName(Symbol)
                Case ButtonSymbol.Delete
                    Return Styling.GetIconName("edit-delete-symbolic")
            End Select

            Return Nothing
        End Function

        Function GetSymbolText() As String
            Select Case SymbolButton
                Case ButtonSymbol.Left1 : Return "<"
                Case ButtonSymbol.Left2 : Return "<<"
                Case ButtonSymbol.Left3 : Return "<<<"
                Case ButtonSymbol.Right1 : Return ">"
                Case ButtonSymbol.Right2 : Return ">>"
                Case ButtonSymbol.Right3 : Return ">>>"
                Case ButtonSymbol.Open : Return "["
                Case ButtonSymbol.Close : Return "]"
                Case ButtonSymbol.Delete : Return "×"
                Case ButtonSymbol.Menu : Return ""
            End Select

            Return SymbolHelp.GetText(Symbol)
        End Function

        Protected Overrides Sub OnClick(e As EventArgs)
            Dim dialog = TryCast(ParentWindow, DialogBase)

            If DialogResult <> DialogResult.None AndAlso dialog IsNot Nothing Then
                dialog.PerformButtonClose(DialogResult, Sub() RaiseClick(e))
            Else
                RaiseClick(e)
            End If
        End Sub

        Private Sub RaiseClick(e As EventArgs)
            ClickAction?.Invoke()

            If ContextMenuStrip IsNot Nothing Then
                ContextMenuStrip.Show(Me, 0, Height)
            End If

            MyBase.OnClick(e)
        End Sub

        Private RegularFont As Font
        Private BoldCount As Integer

        'the bold text shows for a moment that a click had an effect
        Async Sub ShowBold()
            If BoldCount = 0 Then
                'GTK frees the font of a widget when it changes, so a copy is kept
                RegularFont = New Font(Font.Family, Font.Size, Font.FontStyle)
            End If

            BoldCount += 1
            Font = New Font(RegularFont.Family, RegularFont.Size, FontStyle.Bold)
            Await Task.Delay(300)
            BoldCount -= 1

            If BoldCount = 0 AndAlso Not IsDisposed Then
                Font = RegularFont
            End If
        End Sub

        Sub SetFontStyle(style As FontStyle)
            Font = FontManager.GetDefaultFont(0, style)
        End Sub

        Sub SetFontSize(size As Single)
        End Sub

        Enum ButtonSymbol
            None
            Left1
            Left2
            Left3
            Right1
            Right2
            Right3
            Open
            Close
            Delete
            Menu
        End Enum
    End Class

    Public Class SymbolHelp
        Shared Function GetText(symbol As Symbol) As String
            Select Case symbol
                Case Symbol.Up, Symbol.ChevronUp
                    Return "↑"
                Case Symbol.Down, Symbol.ChevronDown
                    Return "↓"
                Case Symbol.Back, Symbol.ChevronLeft
                    Return "←"
                Case Symbol.Forward, Symbol.ChevronRight
                    Return "→"
                Case Symbol.Add
                    Return "+"
                Case Symbol.Remove, Symbol.Delete
                    Return "−"
                Case Symbol.Edit
                    Return "✎"
                Case Symbol.Copy
                    Return "⧉"
                Case Symbol.Search, Symbol.Zoom
                    Return "⌕"
                Case Symbol.Help
                    Return "?"
                Case Symbol.Play
                    Return "▶"
                Case Symbol.Settings
                    Return "⚙"
            End Select

            Return ""
        End Function
    End Class

    'single or multi line text control with a WinForms TextBox like API,
    'the TextBox property returns the control itself for compatibility
    Public Class TextEdit
        Inherits Panel
        Implements IContextMenuOwner

        Private SingleLineBox As TextBox
        Private MultiLineBox As TextArea
        Private BlockTextChanged As Boolean
        Private ContextMenuValue As ContextMenuStripEx
        Private FontValue As Font
        Private ReadOnlyValue As Boolean
        Private TextAlignValue As HorizontalAlignment = HorizontalAlignment.Left
        Private PlaceholderValue As String

        Shadows Event TextChanged(sender As Object, e As EventArgs)
        Shadows Event KeyDown(sender As Object, e As KeyEventArgs)
        Shadows Event KeyUp(sender As Object, e As KeyEventArgs)
        Shadows Event MouseDown(sender As Object, e As MouseEventArgs)
        Shadows Event MouseUp(sender As Object, e As MouseEventArgs)
        Shadows Event MouseWheel(sender As Object, e As MouseEventArgs)
        Shadows Event GotFocus(sender As Object, e As EventArgs)
        Shadows Event LostFocus(sender As Object, e As EventArgs)
        Event DoubleClick(sender As Object, e As EventArgs)
        Event Click(sender As Object, e As EventArgs)

        Sub New()
            CreateSingleLine()
        End Sub

        ReadOnly Property TextBox As TextEdit
            Get
                Return Me
            End Get
        End Property

        ReadOnly Property InnerControl As TextControl
            Get
                Return If(DirectCast(MultiLineBox, TextControl), SingleLineBox)
            End Get
        End Property

        Private Sub CreateSingleLine()
            SingleLineBox = New TextBox
            WireEvents(SingleLineBox)
            Content = SingleLineBox
        End Sub

        Private Sub WireEvents(c As TextControl)
            AddHandler c.TextChanged, Sub(sender, e)
                                          If Not BlockTextChanged Then
                                              OnTextChanged(e)
                                          End If
                                      End Sub
            AddHandler c.KeyDown, Sub(sender, e) RaiseEvent KeyDown(Me, e)
            AddHandler c.KeyUp, Sub(sender, e) RaiseEvent KeyUp(Me, e)
            AddHandler c.MouseDown, Sub(sender, e) RaiseEvent MouseDown(Me, e)
            AddHandler c.MouseUp, Sub(sender, e)
                                      RaiseEvent MouseUp(Me, e)

                                      If e.Buttons = MouseButtons.Primary Then
                                          RaiseEvent Click(Me, EventArgs.Empty)
                                      End If
                                  End Sub
            AddHandler c.MouseDoubleClick, Sub(sender, e) RaiseEvent DoubleClick(Me, e)
            AddHandler c.MouseWheel, Sub(sender, e) RaiseEvent MouseWheel(Me, e)
            AddHandler c.GotFocus, Sub(sender, e) RaiseEvent GotFocus(Me, e)
            AddHandler c.LostFocus, Sub(sender, e) RaiseEvent LostFocus(Me, e)
        End Sub

        Protected Overridable Sub OnTextChanged(e As EventArgs)
            RaiseEvent TextChanged(Me, e)
        End Sub

        Property Multiline As Boolean
            Get
                Return MultiLineBox IsNot Nothing
            End Get
            Set(value As Boolean)
                If value = Multiline Then
                    Exit Property
                End If

                Dim text = Me.Text

                If value Then
                    MultiLineBox = New TextArea With {.Wrap = True, .AcceptsTab = False}
                    WireEvents(MultiLineBox)
                    SingleLineBox = Nothing
                    Content = MultiLineBox
                Else
                    MultiLineBox = Nothing
                    CreateSingleLine()
                End If

                BlockTextChanged = True
                InnerControl.Text = text
                BlockTextChanged = False
                ApplyProperties()
            End Set
        End Property

        Private Sub ApplyProperties()
            If FontValue IsNot Nothing Then
                InnerControl.Font = FontValue
            End If

            If SingleLineBox IsNot Nothing Then
                SingleLineBox.ReadOnly = ReadOnlyValue
                SingleLineBox.TextAlignment = If(TextAlignValue = HorizontalAlignment.Center, TextAlignment.Center,
                    If(TextAlignValue = HorizontalAlignment.Right, TextAlignment.Right, TextAlignment.Left))

                If PlaceholderValue <> "" Then
                    SingleLineBox.PlaceholderText = PlaceholderValue
                End If
            Else
                MultiLineBox.ReadOnly = ReadOnlyValue
            End If

            InnerControl.ContextMenu = ContextMenuValue
        End Sub

        Property Text As String
            Get
                Return If(InnerControl.Text, "")
            End Get
            Set(value As String)
                If value <> InnerControl.Text Then
                    InnerControl.Text = If(value, "")

                    If ShowEndOfText AndAlso SingleLineBox IsNot Nothing AndAlso Not SingleLineBox.HasFocus Then
                        SingleLineBox.CaretIndex = SingleLineBox.Text.Length
                    End If
                End If
            End Set
        End Property

        'paths show their file name when they don't fit
        Property ShowEndOfText As Boolean

        Sub SetTextWithoutTextChangedEvent(text As String)
            BlockTextChanged = True
            Me.Text = text
            BlockTextChanged = False
        End Sub

        Property [ReadOnly] As Boolean
            Get
                Return ReadOnlyValue
            End Get
            Set(value As Boolean)
                ReadOnlyValue = value
                ApplyProperties()
            End Set
        End Property

        Property Font As Font
            Get
                Return If(FontValue, InnerControl.Font)
            End Get
            Set(value As Font)
                FontValue = value
                ApplyProperties()
            End Set
        End Property

        Property TextAlign As HorizontalAlignment
            Get
                Return TextAlignValue
            End Get
            Set(value As HorizontalAlignment)
                TextAlignValue = value
                ApplyProperties()
            End Set
        End Property

        Property BackColor As Color
            Get
                Return InnerControl.BackgroundColor
            End Get
            Set(value As Color)
                InnerControl.BackgroundColor = value
            End Set
        End Property

        Sub ResetBackColor()
            ClearBackgroundColor(InnerControl)
        End Sub

        Property ForeColor As Color
            Get
                Return InnerControl.TextColor
            End Get
            Set(value As Color)
                InnerControl.TextColor = value
            End Set
        End Property

        Property AcceptsReturn As Boolean
            Get
                Return MultiLineBox Is Nothing OrElse MultiLineBox.AcceptsReturn
            End Get
            Set(value As Boolean)
                If MultiLineBox IsNot Nothing Then MultiLineBox.AcceptsReturn = value
            End Set
        End Property

        Property AcceptsTab As Boolean
            Get
                Return MultiLineBox IsNot Nothing AndAlso MultiLineBox.AcceptsTab
            End Get
            Set(value As Boolean)
                If MultiLineBox IsNot Nothing Then MultiLineBox.AcceptsTab = value
            End Set
        End Property

        Property WordWrap As Boolean
            Get
                Return MultiLineBox IsNot Nothing AndAlso MultiLineBox.Wrap
            End Get
            Set(value As Boolean)
                If MultiLineBox IsNot Nothing Then MultiLineBox.Wrap = value
            End Set
        End Property

        Property ContextMenuStrip As ContextMenuStripEx Implements IContextMenuOwner.ContextMenuStrip
            Get
                Return ContextMenuValue
            End Get
            Set(value As ContextMenuStripEx)
                ContextMenuValue = value
                ApplyProperties()
            End Set
        End Property

        Sub SendMessageCue(value As String, Optional hideWhenFocused As Boolean = False)
            PlaceholderValue = value
            ApplyProperties()
        End Sub

        Property SelectionStart As Integer
            Get
                If SingleLineBox IsNot Nothing Then
                    Return SingleLineBox.Selection.Start
                End If

                Return MultiLineBox.Selection.Start
            End Get
            Set(value As Integer)
                [Select](value, 0)
            End Set
        End Property

        Property SelectionLength As Integer
            Get
                If SingleLineBox IsNot Nothing Then
                    Return SingleLineBox.Selection.Length()
                End If

                Return MultiLineBox.Selection.Length()
            End Get
            Set(value As Integer)
                [Select](SelectionStart, value)
            End Set
        End Property

        Property SelectedText As String
            Get
                If SingleLineBox IsNot Nothing Then
                    Return SingleLineBox.SelectedText
                End If

                Return MultiLineBox.SelectedText
            End Get
            Set(value As String)
                Dim start = SelectionStart
                Dim length = SelectionLength
                Dim txt = Text
                start = Math.Min(start, txt.Length)
                length = Math.Min(length, txt.Length - start)
                Text = txt.Substring(0, start) + value + txt.Substring(start + length)
                [Select](start + If(value, "").Length, 0)
            End Set
        End Property

        Sub [Select](start As Integer, length As Integer)
            start = Math.Max(0, Math.Min(start, Text.Length))
            length = Math.Max(0, Math.Min(length, Text.Length - start))
            Dim range = New Range(Of Integer)(start, start + length - 1)

            If SingleLineBox IsNot Nothing Then
                If length = 0 Then
                    SingleLineBox.CaretIndex = start
                Else
                    SingleLineBox.Selection = range
                End If
            Else
                If length = 0 Then
                    MultiLineBox.CaretIndex = start
                Else
                    MultiLineBox.Selection = range
                End If
            End If
        End Sub

        Sub SelectAll()
            If SingleLineBox IsNot Nothing Then
                SingleLineBox.SelectAll()
            Else
                MultiLineBox.SelectAll()
            End If
        End Sub

        Sub Copy()
            Clipboard.Instance.Text = SelectedText
        End Sub

        Sub Paste()
            If Not [ReadOnly] Then
                SelectedText = Clipboard.Instance.Text
            End If
        End Sub

        Shadows Sub Focus()
            InnerControl.Focus()
        End Sub

        ReadOnly Property Focused As Boolean
            Get
                Return InnerControl.HasFocus
            End Get
        End Property

        Sub ScrollToCaret()
        End Sub

        'no-ops for WinForms theme properties
        Property BorderColor As Color
        Property BorderFocusedColor As Color
        Property BorderHoverColor As Color
        Property BackReadonlyColor As Color
    End Class

    Public Class NumEdit
        Inherits NumericStepper

        Private BlockEvent As Boolean
        Private UpIncrementValue As Double = 1
        Private DownIncrementValue As Double = 1
        Private LastValue As Double

        Shadows Event ValueChanged(numEdit As NumEdit)

        Sub New()
            MinValue = -9000000000
            MaxValue = 9000000000
            Increment = 1
            DecimalPlaces = 0
            MaximumDecimalPlaces = 0
            AddHandler MyBase.ValueChanged, Sub()
                                                If BlockEvent Then
                                                    Exit Sub
                                                End If

                                                CorrectStep()
                                                LastValue = MyBase.Value
                                                OnValueChanged(Me)
                                            End Sub
        End Sub

        'the spin buttons step by Increment, a step down is corrected to use DownIncrement
        Private Sub CorrectStep()
            Dim delta = MyBase.Value - LastValue

            If DownIncrementValue <> UpIncrementValue AndAlso delta < 0 AndAlso Math.Abs(-delta - Increment) < 0.000001 Then
                Dim target = Math.Max(MinValue, LastValue - DownIncrementValue)
                BlockEvent = True
                MyBase.Value = target
                BlockEvent = False
            End If
        End Sub

        Sub UpdateValue()
            Value = Value
        End Sub

        Protected Overridable Overloads Sub OnValueChanged(numEdit As NumEdit)
            RaiseEvent ValueChanged(Me)
        End Sub

        Property Minimum As Double
            Get
                Return MinValue
            End Get
            Set(value As Double)
                MinValue = value
            End Set
        End Property

        Property Maximum As Double
            Get
                Return MaxValue
            End Get
            Set(value As Double)
                MaxValue = value
            End Set
        End Property

        Shadows Property Increment As Double
            Get
                Return MyBase.Increment
            End Get
            Set(value As Double)
                MyBase.Increment = value
                UpIncrementValue = value
                DownIncrementValue = value
            End Set
        End Property

        Property UpIncrement As Double
            Get
                Return UpIncrementValue
            End Get
            Set(value As Double)
                UpIncrementValue = value
                MyBase.Increment = value
            End Set
        End Property

        Property DownIncrement As Double
            Get
                Return DownIncrementValue
            End Get
            Set(value As Double)
                DownIncrementValue = value
            End Set
        End Property

        Shadows Property DecimalPlaces As Integer
            Get
                Return MyBase.DecimalPlaces
            End Get
            Set(value As Integer)
                MyBase.DecimalPlaces = value
                MaximumDecimalPlaces = value
            End Set
        End Property

        Shadows Property Value As Double
            Get
                Return MyBase.Value
            End Get
            Set(value As Double)
                If value > MaxValue Then value = MaxValue
                If value < MinValue Then value = MinValue
                LastValue = value
                MyBase.Value = value
            End Set
        End Property

        Sub SetValue(value As Double, updateText As Boolean)
            Me.Value = value
        End Sub

        WriteOnly Property Help As String
            Set(value As String)
                ToolTip = TipProvider.FormatTip(value)
            End Set
        End Property

        Property BackColor As Color
        Property BorderColor As Color
        Property BorderSelectedColor As Color
    End Class

    Public Class CheckBoxEx
        Inherits CheckBox

        Sub New()
            MyBase.New()
        End Sub

        Shadows Property Checked As Boolean
            Get
                Return MyBase.Checked.GetValueOrDefault()
            End Get
            Set(value As Boolean)
                MyBase.Checked = value
            End Set
        End Property

        Property BackColor As Color
        Property ForeColor As Color
    End Class

    'Combo box with a WinForms like API, items are arbitrary objects shown with ToString
    Public Class ComboBoxEx
        Inherits ComboBox

        Private ReadOnly ItemsValue As New ObservableCollection(Of Object)
        Private DropDownStyleValue As ComboBoxStyle = ComboBoxStyle.DropDownList

        Event SelectedIndexChangedEx(sender As Object, e As EventArgs)

        Sub New()
            MyBase.New()
            ItemTextBinding = New DelegateBinding(Of Object, String)(Function(x) If(x?.ToString, ""))
            DataStore = ItemsValue
            [ReadOnly] = True
        End Sub

        Shadows ReadOnly Property Items As ObservableCollection(Of Object)
            Get
                Return ItemsValue
            End Get
        End Property

        Property DropDownStyle As ComboBoxStyle
            Get
                Return DropDownStyleValue
            End Get
            Set(value As ComboBoxStyle)
                DropDownStyleValue = value
                [ReadOnly] = value = ComboBoxStyle.DropDownList
            End Set
        End Property

        Property SelectedItem As Object
            Get
                Return SelectedValue
            End Get
            Set(value As Object)
                SelectedValue = value
            End Set
        End Property

        'the hint in the entry of an editable combo box
        WriteOnly Property PlaceholderText As String
            Set(value As String)
                Dim entry = TryCast(TryCast(ControlObject, Global.Gtk.ComboBox)?.Child, Global.Gtk.Entry)

                If entry IsNot Nothing Then
                    entry.PlaceholderText = value
                End If
            End Set
        End Property

        'in drop down list mode setting the text selects the item with this text
        Shadows Property Text As String
            Get
                If DropDownStyleValue = ComboBoxStyle.DropDownList Then
                    Return If(SelectedValue?.ToString, "")
                End If

                Return MyBase.Text
            End Get
            Set(value As String)
                If DropDownStyleValue = ComboBoxStyle.DropDownList Then
                    SelectedValue = ItemsValue.FirstOrDefault(Function(i) i?.ToString = value)
                Else
                    MyBase.Text = value
                End If
            End Set
        End Property

        Property BackColor As Color
        Property ForeColor As Color
    End Class

    Public Enum ComboBoxStyle
        Simple
        DropDown
        DropDownList
    End Enum

    Public Class LabelEx
        Inherits Label

        Event Click(sender As Object, e As EventArgs)

        'like auto sized WinForms labels, wrapping must be enabled explicitly
        Sub New()
            MyBase.New()
            VerticalAlignment = VerticalAlignment.Center
            Wrap = WrapMode.None
        End Sub

        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            MyBase.OnMouseUp(e)

            If e.Buttons = MouseButtons.Primary Then
                RaiseEvent Click(Me, EventArgs.Empty)
            End If
        End Sub

        Shadows Property Text As String
            Get
                Return MyBase.Text
            End Get
            Set(value As String)
                MyBase.Text = value
                UpdateMinimumWidth()
            End Set
        End Property

        Shadows Property Wrap As WrapMode
            Get
                Return MyBase.Wrap
            End Get
            Set(value As WrapMode)
                MyBase.Wrap = value
                UpdateMinimumWidth()
            End Set
        End Property

        Protected Overrides Sub OnLoadComplete(e As EventArgs)
            MyBase.OnLoadComplete(e)
            UpdateMinimumWidth()
        End Sub

        'Eto labels report no minimum width, so GTK cuts them in tight layouts
        Sub UpdateMinimumWidth()
            Dim w = If(MyBase.Wrap = WrapMode.None AndAlso MyBase.Text <> "", MeasureTextWidth(MyBase.Text, Font) + 1, -1)
            TryCast(ControlObject, Global.Gtk.Widget)?.SetSizeRequest(w, -1)
        End Sub

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
    End Class

    Public Class ButtonLabel
        Inherits LinkButton
        Implements IContextMenuOwner

        Property ClickAction As Action
        Property ContextMenuStrip As ContextMenuStripEx Implements IContextMenuOwner.ContextMenuStrip

        Sub New()
            MyBase.New()
        End Sub

        Protected Overrides Sub OnClick(e As EventArgs)
            MyBase.OnClick(e)
            ClickAction?.Invoke()
        End Sub

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)

            If e.Buttons = MouseButtons.Alternate AndAlso ContextMenuStrip IsNot Nothing Then
                e.Handled = True
                ContextMenuStrip.Show(Me)
            End If
        End Sub

        Sub ShowContext()
            ContextMenuStrip?.Show(Me, New PointF(0, Height))
        End Sub

        Property ForeColor As Color
            Get
                Return TextColor
            End Get
            Set(value As Color)
                TextColor = value
            End Set
        End Property

        Property LinkColor As Color
        Property LinkHoverColor As Color
    End Class

    'check box whose actions only run for changes made by the user, the right click action
    'opens the setting in its dialog
    Public Class ToggleCheckBox
        Inherits CheckBox

        Private BlockAction As Boolean

        Property LeftClickAction As Action(Of Boolean)
        Property RightClickAction As Action(Of Boolean)

        Property State As Boolean
            Get
                Return Checked.GetValueOrDefault
            End Get
            Set(value As Boolean)
                BlockAction = True
                Checked = value
                BlockAction = False
            End Set
        End Property

        Protected Overrides Sub OnCheckedChanged(e As EventArgs)
            MyBase.OnCheckedChanged(e)

            If Not BlockAction Then
                LeftClickAction?.Invoke(State)
            End If
        End Sub

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)

            If e.Buttons = MouseButtons.Alternate AndAlso RightClickAction IsNot Nothing Then
                e.Handled = True
                RightClickAction.Invoke(State)
            End If
        End Sub
    End Class

    'Horizontal line with optional caption
    Public Class LineControl
        Inherits Drawable

        Private TextValue As String = ""

        Sub New()
            Height = FontHeight
        End Sub

        Property Text As String
            Get
                Return TextValue
            End Get
            Set(value As String)
                TextValue = If(value, "")
                Invalidate()
            End Set
        End Property

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)

            Dim gx = e.Graphics
            Dim textOffset As Single = 0
            Dim lineY = CSng(Math.Floor(Height / 2) + 0.5)
            Dim font = FontManager.GetDefaultFont(0, FontStyle.Bold)
            Dim fore = If(Enabled, SystemColors.ControlText, SystemColors.DisabledText)

            If TextValue <> "" Then
                Dim size = gx.MeasureString(font, TextValue)
                gx.DrawText(font, fore, 0, (Height - size.Height) / 2, TextValue)
                textOffset = size.Width + font.LineHeight / 2
            End If

            Dim lineColor = If(IsDarkMode, Color.FromArgb(255, 255, 255, 50), Color.FromArgb(0, 0, 0, 50))
            gx.DrawLine(lineColor, textOffset, lineY, Width, lineY)
        End Sub
    End Class

    Public Class GroupBoxEx
        Inherits GroupBox
    End Class

    'section of a window drawn as card, with a heading, controls right of the heading and the content below
    Public Class Card
        Inherits Panel

        ReadOnly Property Heading As New LabelEx

        Private ReadOnly HeaderRightPanel As New Panel
        Private ReadOnly ContentPanel As New Panel

        Sub New(text As String)
            Heading.Text = text
            Heading.AddStyleClass("staxrip-heading")
            Padding = New Padding(FH(0.7))

            Dim header As New TableLayout(New TableRow(New TableCell(Heading, True), New TableCell(HeaderRightPanel))) With {
                .Spacing = New Size(FH(0.35), 0)}

            Styling.AddToHeaderSizeGroup(header)

            MyBase.Content = New TableLayout(
                New TableRow(header),
                New TableRow(ContentPanel) With {.ScaleHeight = True}) With {.Spacing = New Size(0, FH(0.5))}

            Me.AddStyleClass("staxrip-card")
        End Sub

        Property HeaderRight As Control
            Get
                Return HeaderRightPanel.Content
            End Get
            Set(value As Control)
                HeaderRightPanel.Content = value
            End Set
        End Property

        Shadows Property Content As Control
            Get
                Return ContentPanel.Content
            End Get
            Set(value As Control)
                ContentPanel.Content = value
            End Set
        End Property
    End Class

    'facts separated by dots, secondary facts are dimmed like the dim-label style of GTK, so the key
    'values stand out, the lines break between facts and the dots only separate facts of a line
    Public Class FactsLabel
        Inherits LabelEx

        Private Const Separator = "  ·  "
        Private Facts As New List(Of (Text As String, IsKey As Boolean))
        Private LayoutWidth As Integer = -1
        Private LastMarkup As String

        Sub New()
            Wrap = WrapMode.Word
            Dim label = NativeLabel

            If label IsNot Nothing Then
                AddHandler label.SizeAllocated, Sub(sender, e)
                                                    If e.Allocation.Width <> LayoutWidth Then
                                                        LayoutWidth = e.Allocation.Width
                                                        Application.Instance.AsyncInvoke(AddressOf UpdateMarkup)
                                                    End If
                                                End Sub
            End If
        End Sub

        ReadOnly Property NativeLabel As Global.Gtk.Label
            Get
                Return TryCast(ControlObject, Global.Gtk.Label)
            End Get
        End Property

        'facts are only wrapped as a whole
        Sub SetFacts(ParamArray facts As (Text As String, IsKey As Boolean)())
            Me.Facts = facts.Where(Function(i) i.Text <> "").Select(Function(i) (i.Text.Replace(" "c, Convert.ToChar(&HA0)), i.IsKey)).ToList
            Visible = Me.Facts.Count > 0
            UpdateMarkup()
        End Sub

        Private Sub UpdateMarkup()
            Dim label = NativeLabel

            If label Is Nothing Then
                Me.Text = Facts.Select(Function(i) i.Text).Join(Separator)
                Exit Sub
            End If

            Dim measure = Function(text As String)
                              Dim width, height As Integer

                              Using layout = label.CreatePangoLayout(text)
                                  layout.GetPixelSize(width, height)
                              End Using

                              Return width
                          End Function

            Dim dimmed = Function(text As String) $"<span alpha=""55%"">{GLib.Markup.EscapeText(text)}</span>"
            Dim available = label.Allocation.Width - 2
            Dim separatorWidth = measure(Separator)
            Dim lines As New List(Of List(Of String))
            Dim lineWidth = 0

            For Each fact In Facts
                Dim width = measure(fact.Text)
                Dim markup = If(fact.IsKey, GLib.Markup.EscapeText(fact.Text), dimmed(fact.Text))

                'without allocation all facts are in one line, it's wrapped when necessary
                If lines.Count = 0 OrElse (available > 2 AndAlso lineWidth + separatorWidth + width > available) Then
                    lines.Add(New List(Of String) From {markup})
                    lineWidth = width
                Else
                    lines.Last.Add(markup)
                    lineWidth += separatorWidth + width
                End If
            Next

            Dim value = lines.Select(Function(line) line.Join(dimmed(Separator))).Join(Environment.NewLine)

            If value <> LastMarkup Then
                LastMarkup = value
                label.Markup = value
            End If
        End Sub
    End Class

    'requests a fixed natural width while the content can still shrink to its minimum width,
    'so columns don't move when their content changes and the window can get narrower
    Public Class NaturalWidthBin
        Inherits Global.Gtk.Bin

        Private ReadOnly NaturalWidth As Integer

        Sub New(content As Control, naturalWidth As Integer)
            Me.NaturalWidth = naturalWidth
            HasWindow = False
            Add(content.ToNative(True))
            Show()
        End Sub

        Protected Overrides Sub OnGetPreferredWidth(ByRef minimum_width As Integer, ByRef natural_width As Integer)
            MyBase.OnGetPreferredWidth(minimum_width, natural_width)
            natural_width = Math.Max(minimum_width, NaturalWidth)
        End Sub

        Shared Function Create(content As Control, naturalWidth As Integer) As Control
            Return New NativeControlHost(New NaturalWidthBin(content, naturalWidth))
        End Function
    End Class

    'button with an action and an arrow button that shows a menu, linked like the split buttons of GTK apps
    Public Class SplitButton
        Inherits Panel

        Private ReadOnly MainButton As New Global.Gtk.Button
        Private ReadOnly ArrowButton As New Global.Gtk.Button
        Private ReadOnly Label As New Global.Gtk.Label

        Property ClickAction As Action
        Property Menu As ContextMenuStripEx

        Sub New(text As String, iconName As String, Optional suggested As Boolean = False)
            Dim box As New Global.Gtk.Box(Global.Gtk.Orientation.Horizontal, 0)
            box.StyleContext.AddClass("linked")

            Dim buttonContent As New Global.Gtk.Box(Global.Gtk.Orientation.Horizontal, 6) With {.Halign = Global.Gtk.Align.Center}

            If iconName <> "" Then
                buttonContent.PackStart(Styling.CreateIcon(iconName), False, False, 0)
            End If

            Label.Text = text
            buttonContent.PackStart(Label, False, False, 0)
            MainButton.Child = buttonContent
            ArrowButton.Child = Styling.CreateIcon(Styling.GetIconName("pan-down-symbolic"))
            ArrowButton.TooltipText = "More options"

            If suggested Then
                MainButton.StyleContext.AddClass("suggested-action")
                ArrowButton.StyleContext.AddClass("suggested-action")
            End If

            box.PackStart(MainButton, False, False, 0)
            box.PackStart(ArrowButton, False, False, 0)
            box.ShowAll()

            AddHandler MainButton.Clicked, Sub() ClickAction?.Invoke()
            AddHandler ArrowButton.Clicked, Sub() ShowMenu()

            Content = New NativeControlHost(box)
        End Sub

        Property Text As String
            Get
                Return Label.Text
            End Get
            Set(value As String)
                Label.Text = value
            End Set
        End Property

        Property ToolTipText As String
            Get
                Return MainButton.TooltipText
            End Get
            Set(value As String)
                MainButton.TooltipText = value
            End Set
        End Property

        'shows a confirmation instead of the text for a moment
        Async Sub ShowConfirmation(text As String)
            Dim previous = Label.Text
            Label.Text = text
            Await Task.Delay(1500)

            If Label.Text = text Then
                Label.Text = previous
            End If
        End Sub

        Sub ShowMenu()
            Dim x, y As Integer

            If Menu IsNot Nothing AndAlso ArrowButton.TranslateCoordinates(TryCast(ControlObject, Global.Gtk.Widget), 0, ArrowButton.AllocatedHeight, x, y) Then
                Menu.Show(Me, x, y)
            End If
        End Sub
    End Class

    Public Enum NextStepKind
        Ready
        Hint
        Warning
        [Error]
    End Enum

    'the bar of the main window that says what needs attention next and offers the action that resolves it
    Public Class NextStepBar
        Inherits Panel

        Private ReadOnly Icon As New Global.Gtk.Image
        Private KindValue As NextStepKind = NextStepKind.Hint

        ReadOnly Property TitleLabel As New LabelEx
        ReadOnly Property MessageLabel As New LabelEx With {.Wrap = WrapMode.Word}
        ReadOnly Property ActionButton As New ButtonEx With {.Visible = False}
        ReadOnly Property ContinueButton As New ButtonEx("Continue") With {.Visible = False}

        Sub New()
            TitleLabel.AddStyleClass("staxrip-heading")
            Icon.Show()
            Icon.StyleContext.AddClass("staxrip-next-step-icon")
            Padding = New Padding(FH(0.7), FH(0.5))

            Dim text As New TableLayout(New TableRow(TitleLabel), New TableRow(MessageLabel)) With {.Spacing = New Size(0, FH(0.1))}

            Content = New TableLayout(New TableRow(
                New TableCell(New NativeControlHost(Icon)),
                New TableCell(text, True),
                New TableCell(HRow(ActionButton, ContinueButton)))) With {.Spacing = New Size(FH(0.7), 0)}

            Me.AddStyleClass("staxrip-next-step")
            Kind = NextStepKind.Hint
        End Sub

        Property Kind As NextStepKind
            Get
                Return KindValue
            End Get
            Set(value As NextStepKind)
                Me.RemoveStyleClass("staxrip-next-step-" + KindValue.ToString.ToLowerInvariant)
                KindValue = value
                Me.AddStyleClass("staxrip-next-step-" + value.ToString.ToLowerInvariant)

                Dim iconName = Styling.GetIconName(
                    If(value = NextStepKind.Ready, "object-select-symbolic",
                    If(value = NextStepKind.Warning, "dialog-warning-symbolic",
                    If(value = NextStepKind.Error, "dialog-error-symbolic", "dialog-information-symbolic"))))

                Icon.SetFromIconName(iconName, Global.Gtk.IconSize.LargeToolbar)
            End Set
        End Property

        Sub SetMessage(title As String, message As String)
            TitleLabel.Text = title
            MessageLabel.Text = If(message, "").Trim
            MessageLabel.Visible = MessageLabel.Text <> ""
        End Sub

        Sub SetAction(text As String)
            ActionButton.Text = text
            ActionButton.Visible = text <> ""
        End Sub
    End Class

    Public Class PanelEx
        Inherits Panel

        Property ShowNiceBorder As Boolean
    End Class

    Public Class TrackBarEx
        Inherits Slider

        Property NoMouseWheelEvent As Boolean

        'raised for changes made by the user only
        Event Scroll(sender As Object, e As EventArgs)

        Private BlockScroll As Boolean

        Sub New()
            AddHandler ValueChanged, Sub()
                                         If Not BlockScroll Then
                                             RaiseEvent Scroll(Me, EventArgs.Empty)
                                         End If
                                     End Sub

            'the position is no value of its own, the controls next to the slider show the size
            Dim scale = If(TryCast(ControlObject, Global.Gtk.Scale), TryCast(TryCast(ControlObject, Global.Gtk.Bin)?.Child, Global.Gtk.Scale))

            If scale IsNot Nothing Then
                scale.DrawValue = False
            End If
        End Sub

        Property Minimum As Integer
            Get
                Return MinValue
            End Get
            Set(value As Integer)
                MinValue = value
            End Set
        End Property

        Property Maximum As Integer
            Get
                Return MaxValue
            End Get
            Set(value As Integer)
                MaxValue = value
            End Set
        End Property

        Shadows Property Value As Integer
            Get
                Return MyBase.Value
            End Get
            Set(value As Integer)
                If value > MaxValue Then value = MaxValue
                If value < MinValue Then value = MinValue

                If value <> MyBase.Value Then
                    BlockScroll = True
                    MyBase.Value = value
                    BlockScroll = False
                End If
            End Set
        End Property
    End Class

    'Progress bar showing a progress fill and a single line of optionally colored text
    Public Class LabelProgressBar
        Inherits Drawable

        Private MinimumValue As Double
        Private MaximumValue As Double = 100
        Private ProgressValue As Double
        Private TextValue As String = ""
        Private Segments As List(Of (Text As String, Color As Color?, Bold As Boolean))

        Property ProgressColor As Color = Color.FromArgb(60, 140, 200, 90)
        Property Font As Font = FontManager.GetCodeFont(-1)

        Sub New()
            Height = CInt(FontHeight * 1.6)
        End Sub

        Property Minimum As Double
            Get
                Return MinimumValue
            End Get
            Set(value As Double)
                MinimumValue = value
                Invalidate()
            End Set
        End Property

        Property Maximum As Double
            Get
                Return MaximumValue
            End Get
            Set(value As Double)
                MaximumValue = value
                Invalidate()
            End Set
        End Property

        Property Value As Double
            Get
                Return ProgressValue
            End Get
            Set(value As Double)
                If ProgressValue <> value Then
                    ProgressValue = value
                    Invalidate()
                End If
            End Set
        End Property

        Property Text As String
            Get
                Return TextValue
            End Get
            Set(value As String)
                TextValue = If(value, "")
                Segments = Nothing
                Invalidate()
            End Set
        End Property

        Sub SetSegments(value As List(Of (Text As String, Color As Color?, Bold As Boolean)))
            Segments = value
            TextValue = String.Join("", value.Select(Function(x) x.Text))
            Invalidate()
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)

            Dim gx = e.Graphics
            Dim range = MaximumValue - MinimumValue

            If range > 0 Then
                Dim w = CSng(Width * Math.Max(0, Math.Min(1, (ProgressValue - MinimumValue) / range)))
                gx.FillRectangle(ProgressColor, 0, 0, w, Height)
            End If

            Dim fore = SystemColors.ControlText
            Dim y = (Height - Font.LineHeight) / 2
            Dim x As Single = Font.LineHeight / 3

            If Segments Is Nothing Then
                gx.DrawText(Font, fore, x, y, TextValue)
            Else
                For Each seg In Segments
                    Dim f = If(seg.Bold, New Font(Font.Family, Font.Size, FontStyle.Bold), Font)
                    gx.DrawText(f, If(seg.Color, fore), x, y, seg.Text)
                    x += gx.MeasureString(f, seg.Text).Width
                Next
            End If
        End Sub
    End Class

    Public Class TabControlEx
        Inherits TabControl

        ReadOnly Property TabPages As Collection(Of TabPage)
            Get
                Return Pages
            End Get
        End Property

        Property SelectedTab As TabPage
            Get
                Return SelectedPage
            End Get
            Set(value As TabPage)
                SelectedPage = value
            End Set
        End Property
    End Class

    Public Class SearchTextBox
        Inherits SearchBox

        Shadows Event TextChanged()

        Sub New()
            MyBase.New()
            AddHandler MyBase.TextChanged, Sub() RaiseEvent TextChanged()
        End Sub

        Sub SendMessageCue(value As String)
            PlaceholderText = value
        End Sub
    End Class

    Public Interface IPage
        Property Node As TreeNode
        Property Path As String
        Property TipProvider As TipProvider
        Property FormSizeScaleFactor As SizeF
    End Interface

    'pages that store their values when the settings dialog is confirmed
    Public Interface ISaveValues
        Sub SaveValues()
    End Interface

    'Labels and edits of the audio rows of the main window
    Public Class AudioTextEdit
        Inherits TextEdit

        Sub New()
            MyBase.New()
        End Sub

        Sub New(row As Integer)
            Me.New()
        End Sub
    End Class

    Public Class AudioRowLabel
        Inherits LabelEx

        Sub New(row As Integer)
            MyBase.New()
            Text = (row + 1).ToString
            Me.AddStyleClass("dim-label")
        End Sub
    End Class

    'language of the track, the menu lists the common languages first and the others by initial,
    'it's created on the first click because there are hundreds of languages
    Public Class AudioLanguageButton
        Inherits ButtonEx

        Private ReadOnly LanguageMenu As New ContextMenuStripEx
        Private TrackLanguageValue As Language

        Property LanguageChanged As Action(Of Language)

        Sub New()
            MyBase.New()
            ShowMenuSymbol = True
            MinimumSize = Size.Empty
        End Sub

        Sub New(row As Integer)
            Me.New()
        End Sub

        Property TrackLanguage As Language
            Get
                Return TrackLanguageValue
            End Get
            Set(value As Language)
                TrackLanguageValue = value
                Text = If(value?.EnglishName, "")
            End Set
        End Property

        Protected Overrides Sub OnClick(e As EventArgs)
            If LanguageMenu.Items.Count = 0 Then
                For Each lng In Language.Languages.OrderBy(Function(x) x.EnglishName)
                    Dim path = If(lng.IsCommon, g.GetExtendedLanguageName(lng),
                        $"More | {lng.ToString().Substring(0, 1).ToUpperInvariant()}... | {g.GetExtendedLanguageName(lng)}")

                    MenuItemEx.Add(Of Language)(LanguageMenu.Items, path, Sub(l As Language)
                                                                              TrackLanguage = l
                                                                              LanguageChanged?.Invoke(l)
                                                                          End Sub, lng)
                Next
            End If

            Dim selectedPath = LanguageMenu.GetItems().OfType(Of MenuItemEx)().
                Where(Function(x) TypeOf x.Tag Is Language AndAlso DirectCast(x.Tag, Language).Name = TrackLanguage?.Name).
                OrderBy(Function(x) If(x.Path, "").Length).FirstOrDefault()?.Path

            For Each item In LanguageMenu.GetItems().OfType(Of MenuItemEx)()
                item.Marked = selectedPath <> "" AndAlso (item.Path = selectedPath OrElse selectedPath.StartsWith(item.Path + " | "))
            Next

            LanguageMenu.Show(Me, New PointF(0, Height))
            MyBase.OnClick(e)
        End Sub
    End Class

    Public Class AudioNameButtonLabel
        Inherits ButtonEx

        Property ContextMenuStripEx As ContextMenuStripEx
            Get
                Return ContextMenuStrip
            End Get
            Set(value As ContextMenuStripEx)
                ContextMenuStrip = value
            End Set
        End Property

        Sub New()
            MyBase.New()
            Text = "Audio Profile"
            ShowMenuSymbol = True
            MinimumSize = Size.Empty
        End Sub

        Sub New(row As Integer)
            Me.New()
        End Sub
    End Class

    Public Class AudioEditButtonLabel
        Inherits ButtonEx

        Sub New()
            MyBase.New()
            Text = "Edit..."
            MinimumSize = Size.Empty
        End Sub

        Sub New(row As Integer)
            Me.New()
        End Sub
    End Class
End Namespace
