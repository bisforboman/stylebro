# Non-data fields are kept only by reflection-shaped strings (owner: "Reflection-shaped strings only")
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '        if (symbol is IFieldSymbol && !isData)'
    Replace = '        if (symbol is IFieldSymbol && !isData && symbol.Name.Length < 0)'
    Tests = 'KeptFindingsTests'
}
