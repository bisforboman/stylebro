# BRO1303: like_private internal fields are checked like private ones
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '            && IsPrivateLike(field, style)'
    Replace = '            && field.DeclaredAccessibility == Accessibility.Private'
    Tests = 'FieldNamingTests.InternalLikePrivate_NamesInternalFieldsLikePrivateOnes'
}
