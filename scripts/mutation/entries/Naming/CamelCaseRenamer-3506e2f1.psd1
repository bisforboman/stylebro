# BRO1305: only crefs of the type parameter's own type or method
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'g.Identifier.ValueText == owner.Name'
    Replace = 'true'
    Tests = 'PrefixNamingTests.TypeParametersInCrefs_AreRenamedToo'
}
