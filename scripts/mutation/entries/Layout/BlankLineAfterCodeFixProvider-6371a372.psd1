# BRO1504's fix removes the blank line below only when BRO1506 is on (trial 23, jint)
@{
    File = 'src/StyleBro.CodeFixes/Layout/BlankLineAfterCodeFixProvider.cs'
    Find = 'if (Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, DiagnosticIds.BlankLineAfterComment, cancellationToken))'
    Replace = 'if (true)'
    Tests = 'BlankLineAfterTests.CommentBelowCode_LosesTheBlankLineBelowIt_WhenBRO1506IsOn'
}
