# BRO1147 (redundant null-forgiving '!')
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = 'case EqualsValueClauseSyntax clause when clause.Parent is not VariableDeclaratorSyntax { Parent.Parent: LocalDeclarationStatementSyntax }:'
    Replace = 'case EqualsValueClauseSyntax clause when false:'
    Tests = 'NullForgivingTests'
}
