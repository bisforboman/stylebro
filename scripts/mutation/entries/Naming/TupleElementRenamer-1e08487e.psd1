# BRO1311: a tuple element in a body isn't seen by other projects.
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'a is BlockSyntax or ArrowExpressionClauseSyntax or EqualsValueClauseSyntax'
    Replace = 'a is ArrowExpressionClauseSyntax or EqualsValueClauseSyntax'
    Tests = 'KeptFindingsTests'
}
