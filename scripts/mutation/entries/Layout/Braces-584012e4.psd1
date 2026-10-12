# BRO1514 if_and_loops: a deconstructing foreach isn't checked either (S121 registers ForEachStatement only).
@{
    File = 'src/StyleBro.Analyzers/Layout/Braces.cs'
    Find = 'or ForEachVariableStatementSyntax && OnlyIfAndLoops'
    Replace = '&& OnlyIfAndLoops'
    Tests = 'BracesTests'
}
