# BRO1126 sorts the usings when dotnet format would (xbox AuthClient, second run)
@{
    File = 'src/StyleBro.CodeFixes/Readability/QualifiedUsingsCodeFixProvider.cs'
    Find = '        return changes.Count > 0 && ('
    Replace = '        return changes.Count < 0 && ('
    Tests = 'FixOrderTests.QualifiedUsings'
}
