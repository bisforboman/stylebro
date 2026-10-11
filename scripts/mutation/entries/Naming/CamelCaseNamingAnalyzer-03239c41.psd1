# BRO1310 (Hungarian prefixes)
@{
    File = 'src/StyleBro.Analyzers/Naming/CamelCaseNamingAnalyzer.cs'
    Find = ', name => HungarianNames.GetVariableName(name, hungarian))'
    Replace = ')'
    Tests = 'HungarianNamingTests'
}
