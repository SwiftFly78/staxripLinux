
'On Linux the fonts come from fontconfig, a font name configured in the settings
'is used if it's installed, otherwise the system default/monospace font is used.
Public NotInheritable Class FontManager
    Private Shared ReadOnly FamilyCache As New Dictionary(Of FontCategory, FontFamily)
    Private Shared AvailableFamilies As List(Of FontFamily)

    Public Const DefaultCodeFontSize As Single = 10.0
    Public Const DefaultFontSize As Single = 9.0
    Public Const DefaultThumbnailFontSize As Single = 10.0

    Shared Sub Reset()
        FamilyCache.Clear()
    End Sub

    Shared ReadOnly Property SystemFontSize As Single
        Get
            Return SystemFonts.Default().Size
        End Get
    End Property

    Shared Function GetAvailableFamilies() As List(Of FontFamily)
        If AvailableFamilies Is Nothing Then
            Try
                AvailableFamilies = Fonts.AvailableFontFamilies.OrderBy(Function(x) x.Name).ToList()
            Catch
                AvailableFamilies = New List(Of FontFamily)
            End Try
        End If

        Return AvailableFamilies
    End Function

    Shared Function GetFontFamilies(category As FontCategory, filtered As Boolean) As List(Of FontFamily)
        Dim families As IEnumerable(Of FontFamily) = GetAvailableFamilies()

        If filtered Then
            families = families.Where(Function(x) Not x.Name.ContainsAny({"UltraCondensed", "UltraExpanded"}) AndAlso Not x.Name.EndsWith(" Thin"))
        End If

        If category = FontCategory.Code Then
            families = families.Where(Function(x) IsMonospace(x))
        End If

        Return families.ToList()
    End Function

    Shared Function IsMonospace(family As FontFamily) As Boolean
        Try
            Using font = New Font(family, 10)
                Return Math.Abs(font.MeasureString("iiiii").Width - font.MeasureString("WWWWW").Width) < 1
            End Using
        Catch
            Return False
        End Try
    End Function

    Shared Function GetFontFamily(category As FontCategory, fontName As String) As FontFamily
        If fontName <> "" Then
            Dim family = GetAvailableFamilies().FirstOrDefault(Function(x) x.Name.Equals(fontName, StringComparison.OrdinalIgnoreCase))

            If family IsNot Nothing Then
                Return family
            End If
        End If

        If category = FontCategory.Code Then
            Return New FontFamily(FontFamilies.MonospaceFamilyName)
        End If

        Return SystemFonts.Default().Family
    End Function

    Shared Function GetFont(category As FontCategory, fontName As String, Optional size As Single = DefaultFontSize, Optional style As FontStyle = FontStyle.None) As Font
        Return New Font(GetFontFamily(category, fontName), Math.Max(1, size * s.UIScaleFactor), style)
    End Function

    Shared Function GetFont(family As FontFamily, Optional size As Single = DefaultFontSize, Optional style As FontStyle = FontStyle.None) As Font
        Return New Font(family, Math.Max(1, size * s.UIScaleFactor), style)
    End Function

    Private Shared Function GetCategoryFont(category As FontCategory, size As Single, style As FontStyle) As Font
        Dim family As FontFamily = Nothing

        If Not FamilyCache.TryGetValue(category, family) Then
            Dim name = ""

            If s?.Fonts IsNot Nothing AndAlso s.Fonts.ContainsKey(category) Then
                name = s.Fonts(category)
            End If

            family = GetFontFamily(category, name)
            FamilyCache(category) = family
        End If

        Return New Font(family, Math.Max(1, size), style)
    End Function

    'sizeOffset is relative to the system font size
    Shared Function GetCodeFont(Optional sizeOffset As Single = 0.0, Optional style As FontStyle = FontStyle.None) As Font
        Return GetCategoryFont(FontCategory.Code, SystemFontSize + sizeOffset, style)
    End Function

    Shared Function GetDefaultFont(Optional sizeOffset As Single = 0.0, Optional style As FontStyle = FontStyle.None) As Font
        If sizeOffset = 0 AndAlso style = FontStyle.None AndAlso
            (s?.Fonts Is Nothing OrElse Not s.Fonts.ContainsKey(FontCategory.Default) OrElse s.Fonts(FontCategory.Default) = "") Then

            Return SystemFonts.Default()
        End If

        Return GetCategoryFont(FontCategory.Default, SystemFontSize + sizeOffset, style)
    End Function

    Shared Function GetThumbnailFont(Optional sizeOffset As Single = 0.0, Optional style As FontStyle = FontStyle.None) As Font
        Return GetCategoryFont(FontCategory.Thumbnail, DefaultThumbnailFontSize + sizeOffset, style)
    End Function
End Class

Public Enum FontCategory
    All
    Code
    [Default]
    Thumbnail
End Enum
