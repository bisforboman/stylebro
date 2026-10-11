# Names nameof(...) produces keep them (Ocelot)
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'
    Find = '&& !names.Nameof.Contains(oldName)'
    Replace = ''
    Tests = 'CamelCaseNamingTests.NamesInNameof_KeepTheirName'
}
