# A global IDE1006 suppression targeting a containing type, member or namespace
@{
    File = 'src/StyleBro.Analyzers/Naming/NamingRules.cs'
    Find = '&& global.Any(g => g.Target == id &&'
    Replace = '&& global.Any(g => false &&'
    Tests = 'PascalCaseNamingTests.GlobalIde1006Suppressions'
}
