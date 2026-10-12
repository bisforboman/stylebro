# BRO1147 allow_null_forgiving_on_unannotated: explicitly typed locals are still checked, like S8969.
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = 'Parent: VariableDeclarationSyntax { Type.IsVar: true }'
    Replace = 'Parent: VariableDeclarationSyntax'
    Tests = 'NullForgivingTests'
}
