# BRO1524 (split conditional expressions)
@{
    File = 'src/StyleBro.Analyzers/Layout/ConditionalLayout.cs'
    Find = 'if (conditional.ContainsDiagnostics'
    Replace = 'if (false'
    Tests = 'ConditionalLayoutTests'
}
