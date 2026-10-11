# Names nameof(...) produces keep them (Ocelot)
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNames.cs'
    Find = '|| CamelCaseNames.IsNameofName(token)'
    Replace = ''
    Tests = 'FieldNamingTests.NamesInNameof_KeepTheirName'
}
