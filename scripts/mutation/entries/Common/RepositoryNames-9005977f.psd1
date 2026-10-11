# GetProperty and friends count only on a Type-like receiver (JsonElement.GetProperty in Orleans)
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = 'return TypeMethods.Contains(name) ? receiver is null || IsTypeLike(receiver)'
    Replace = 'return TypeMethods.Contains(name) ? true'
    Tests = 'KeptFindingsTests'
}
