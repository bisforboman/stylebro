# BRO1306: like_private internal static readonly fields take the private static style
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '!IsPrivateLike(field, style) || !IsSourceField(field)'
    Replace = 'field.DeclaredAccessibility != Accessibility.Private || !IsSourceField(field)'
    Tests = 'FieldNamingTests.InternalLikePrivate_NamesInternalFieldsLikePrivateOnes'
}
