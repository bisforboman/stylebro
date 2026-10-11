# BRO1309: snake_case names; a property kept by a string keeps its type's other properties
@{
    File = 'src/StyleBro.Analyzers/Naming/PascalCaseNamingAnalyzer.cs'
    Find = 'symbol.Name.TrimStart(''_'').IndexOf(''_'') < 0 ? GetNewName(symbol.Name) : null;'
    Replace = 'GetNewName(symbol.Name);'
    Tests = 'PascalCaseNamingTests'
}
