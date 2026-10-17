# BRO1514-BRO1516 leave an embedded empty statement (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'if (child is BlockSyntax or EmptyStatementSyntax)'
    Replace = 'if (child is BlockSyntax)'
    Tests = 'BracesTests.AnEmbeddedEmptyStatement_IsNotReported'
}
