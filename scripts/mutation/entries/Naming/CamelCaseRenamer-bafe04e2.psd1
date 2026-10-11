# BRO1309: snake_case names; a property kept by a string keeps its type's other properties
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (reason is null && symbol is IPropertySymbol'
    Replace = 'if (false && symbol is IPropertySymbol'
    Tests = 'PascalCaseNamingTests'
}
