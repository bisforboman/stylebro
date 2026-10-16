# BRO1306: like_private internal fields aren't PascalCase
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'Accessibility.ProtectedOrInternal && !IsPrivateLike(field, style)));'
    Replace = 'Accessibility.ProtectedOrInternal));'
    Tests = 'FieldNamingTests.InternalLikePrivate_NamesInternalFieldsLikePrivateOnes'
}
