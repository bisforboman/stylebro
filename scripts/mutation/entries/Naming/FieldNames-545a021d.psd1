# like_private wins over the naming rules for internal fields
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '|| index < 0 || IsPrivateLike(field, style)'
    Replace = '|| index < 0'
    Tests = 'FieldNamingTests.InternalLikePrivate_KeyDecidesTheName'
}
