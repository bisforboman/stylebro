# BRO1301: the 'this.' stays unless the repository asks for no qualifier
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '&& Analyzers.Readability.BaseCalls.PrefersNoQualifier(member, options)'
    Replace = '&& true'
    Tests = 'CamelCaseNamingTests.AThisALocalMadeNecessary_GoesWithTheRename'
}
