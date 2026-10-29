
Namespace UI
    Public Class KeysHelp
        Private Shared KeysTexts As Dictionary(Of Keys, String)

        Shared ReadOnly Property IsNumPadInstalled As Boolean = True

        Shared Sub New()
            KeysTexts = New Dictionary(Of Keys, String)
            KeysTexts(Keys.Add) = "+ (Numpad)"
            KeysTexts(Keys.Backspace) = "Back"
            KeysTexts(Keys.Decimal) = "Decimal"
            KeysTexts(Keys.Delete) = "Delete"
            KeysTexts(Keys.Divide) = "Divide"
            KeysTexts(Keys.Down) = "Down"
            KeysTexts(Keys.End) = "End"
            KeysTexts(Keys.Enter) = "Enter"
            KeysTexts(Keys.Escape) = "Escape"
            KeysTexts(Keys.Home) = "Home"
            KeysTexts(Keys.Insert) = "Insert"
            KeysTexts(Keys.Left) = "Left"
            KeysTexts(Keys.Multiply) = "Multiply"
            KeysTexts(Keys.PageDown) = "Page Down"
            KeysTexts(Keys.PageUp) = "Page Up"
            KeysTexts(Keys.Right) = "Right"
            KeysTexts(Keys.Space) = "Space"
            KeysTexts(Keys.Subtract) = "- (Numpad)"
            KeysTexts(Keys.Up) = "Up"
            KeysTexts(Keys.Control) = "Control"
            KeysTexts(Keys.Alt) = "Alt"
            KeysTexts(Keys.Shift) = "Shift"
            KeysTexts(Keys.Equal) = "="
            KeysTexts(Keys.Minus) = "-"
            KeysTexts(Keys.Comma) = ","
            KeysTexts(Keys.Period) = "."

            For i = 0 To 9
                KeysTexts(CType(Keys.D0 + i, Keys)) = i.ToString
                KeysTexts(CType(Keys.Keypad0 + i, Keys)) = i & " (Numpad)"
            Next
        End Sub

        'the keys of a string made by GetKeyString, like Ctrl+Shift+S
        Shared Function ParseKeyString(text As String) As Keys
            If String.IsNullOrWhiteSpace(text) Then
                Return Keys.None
            End If

            Dim ret = Keys.None
            Dim rest = text.Trim

            For Each modifier In {("Ctrl+", Keys.Control), ("Alt+", Keys.Alt), ("Shift+", Keys.Shift)}
                If rest.StartsWith(modifier.Item1) AndAlso rest.Length > modifier.Item1.Length Then
                    ret = ret Or modifier.Item2
                    rest = rest.Substring(modifier.Item1.Length)
                End If
            Next

            Dim key = KeysTexts.FirstOrDefault(Function(i) i.Value = rest).Key
            Dim parsed As Keys

            If key = Keys.None AndAlso Not [Enum].TryParse(rest, parsed) Then
                Return Keys.None
            End If

            Return ret Or If(key <> Keys.None, key, parsed)
        End Function

        Shared Function GetKeyString(key As Keys) As String
            If key = Keys.None Then
                Return ""
            End If

            Dim ret = ""

            If (key And Keys.Control) = Keys.Control Then
                key = key Xor Keys.Control
                ret += "Ctrl+"
            End If

            If (key And Keys.Alt) = Keys.Alt Then
                key = key Xor Keys.Alt
                ret += "Alt+"
            End If

            If (key And Keys.Shift) = Keys.Shift Then
                key = key Xor Keys.Shift
                ret += "Shift+"
            End If

            If KeysTexts.ContainsKey(key) Then
                ret += KeysTexts(key)
            Else
                ret += key.ToString
            End If

            Return ret
        End Function
    End Class
End Namespace
