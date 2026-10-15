# BRO1505: the blank line goes below an #endif (trial 23)
@{
    File = 'src/StyleBro.Analyzers/Layout/ElementSeparation.cs'
    Find = 'if (leading[i].IsKind(SyntaxKind.EndIfDirectiveTrivia))'
    Replace = 'if (false)'
    Tests = 'ElementSeparationTests.BlankLine_GoesBelowTheDirectivesThatEndConditionalCode'
}
