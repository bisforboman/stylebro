# Renamer: a name only a field has ('_root') counts in any string (trial 29, Harmony)
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '|| (symbol is IFieldSymbol && LooksLikeFieldName(symbol.Name) && strings.Contains(symbol.Name))'
    Replace = ''
    Tests = 'KeptFindingsTests.AFieldNameOnlyAFieldHas'
}
