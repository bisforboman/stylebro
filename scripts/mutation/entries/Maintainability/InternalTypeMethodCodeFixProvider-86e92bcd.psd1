# BRO1409 (public methods of internal types): the analyzer's skips, then the fix's guards
@{
    File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'
    Find = 'if (!Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, DiagnosticIds.MemberOrdering, cancellationToken))'
    Replace = 'if (true)'
    Tests = 'InternalTypeMethodTests'
}
