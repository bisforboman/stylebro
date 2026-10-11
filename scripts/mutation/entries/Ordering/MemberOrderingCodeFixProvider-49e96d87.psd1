# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.CodeFixes/Ordering/MemberOrderingCodeFixProvider.cs'
    Find = 'Severities.IsOn(document.Project.CompilationOptions, tree, DiagnosticIds.AutoAccessorsOnOneLine, cancellationToken)'
    Replace = 'false'
    Tests = 'FixOrderTests'
}
