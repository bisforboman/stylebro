# Names nameof(...) produces keep them (Ocelot)
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNames.cs'
    Find = 'access.Name == name ? (ExpressionSyntax)access : name'
    Replace = 'name'
    Tests = 'FieldNamingTests.NamesInNameof_KeepTheirName'
}
