# BRO1309: snake_case names; a property kept by a string keeps its type's other properties
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '.Any(p => PascalCaseNamingAnalyzer.GetNewName(p.Name) is not null && FindInCode(p, names).Reason is not null)'
    Replace = '.Any(p => FindInCode(p, names).Reason is not null)'
    Tests = 'PascalCaseNamingTests'
}
