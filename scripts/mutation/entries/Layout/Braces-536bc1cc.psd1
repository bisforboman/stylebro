# BRO1514-BRO1516: 'fixed (a) fixed (b) { }' shares one block like consecutive usings (#129)
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = ' or FixedStatementSyntax { Statement: FixedStatementSyntax }'
    Replace = ''
    Tests = 'BracesTests.ConsecutiveFixed_ShareOneBlock'
}
