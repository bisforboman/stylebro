# s_camelCase: inner underscores are BRO1308's
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '|| !char.IsLetter(rest[0]) || rest.Contains(''_''))'
    Replace = '|| !char.IsLetter(rest[0]))'
    Tests = 'FieldNamingTests.NewName_StaticPrefix'
}
