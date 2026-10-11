# BRO1514-BRO1516 (braces)
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'child is ReturnStatementSyntax or ThrowStatementSyntax'
    Replace = 'child is StatementSyntax or ThrowStatementSyntax'
    Tests = 'BracesTests'
}
