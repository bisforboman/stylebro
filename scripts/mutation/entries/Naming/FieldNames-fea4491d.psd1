# like_private private protected fields aren't named like protected ones
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '        && !IsPrivateLike(field, style);'
    Replace = '        && true;'
    Tests = 'FieldNamingTests.InternalLikePrivate_NamesInternalFieldsLikePrivateOnes'
}
