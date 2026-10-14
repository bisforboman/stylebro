# BRO1309: [SuppressMessage] for IDE1006 keeps names
@{
    File = 'src/StyleBro.Analyzers/Naming/NamingRules.cs'
    Find = '|| IsSuppressedByAttribute(compilation, symbol))'
    Replace = ')'
    Tests = 'PascalCaseNamingTests.WhereIde1006IsSuppressed'
}
