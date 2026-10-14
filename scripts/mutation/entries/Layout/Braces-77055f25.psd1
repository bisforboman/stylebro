# BRO1514: an else sharing a line follows its (moved) if (trial 20)
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = '&& owner is ElseClauseSyntax or IfStatementSyntax { Parent: ElseClauseSyntax }'
    Replace = '&& false'
    Tests = 'BracesTests.NestedOnOneLine_InOnePass'
}
