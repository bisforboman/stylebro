# Renamer: keeps 'this.' when something else is named like the new name
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '|| !model.LookupSymbols(access.SpanStart, name: newName).IsEmpty'
    Replace = ''
    Tests = 'FieldNamingTests'
}
