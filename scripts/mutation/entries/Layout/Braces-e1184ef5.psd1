# BRO1514 stylebro_braces_statements = if_and_loops: using, lock and fixed bodies need no braces (Sonar's S121).
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'ForEachVariableStatementSyntax && OnlyIfAndLoops(options)))'
    Replace = 'ForEachVariableStatementSyntax && false))'
    Tests = 'BracesTests'
}
