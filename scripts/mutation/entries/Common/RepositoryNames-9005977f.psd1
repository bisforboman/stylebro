# GetProperty and friends count only on a Type-like receiver (JsonElement.GetProperty in Orleans)
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = '                    ? receiver is null || IsTypeLike(receiver)'
    Replace = '                    ? true'
    Tests = 'KeptFindingsTests'
}
