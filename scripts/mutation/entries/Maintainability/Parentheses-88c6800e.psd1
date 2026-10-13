# Trial 18: parentheses with a comment and a line break inside stay
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'
    Find = 'gap.All(IsSpace) || !gap.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia) || t.HasStructure);'
    Replace = 'true;'
    Tests = 'ParenthesesTests'
}
