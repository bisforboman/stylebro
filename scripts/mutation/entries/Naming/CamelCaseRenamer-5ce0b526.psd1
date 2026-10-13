# Renamer: only drops a 'this.' the original code needed
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '|| !model.LookupSymbols(access.SpanStart, name: oldName).Any(s => s is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol))'
    Replace = ')'
    Tests = 'FieldNamingTests'
}
