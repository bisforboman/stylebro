# BRO1310 (Hungarian prefixes)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = 'if (GetNewName(f, key.Item1, hungarian) is { } name)'
    Replace = 'if (GetNewName(f, key.Item1) is { } name)'
    Tests = 'HungarianNamingTests'
}
