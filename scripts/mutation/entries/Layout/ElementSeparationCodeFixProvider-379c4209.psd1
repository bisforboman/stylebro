# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.CodeFixes/Layout/ElementSeparationCodeFixProvider.cs'
    Find = 'DiagnosticIds.AutoAccessorsOnOneLine, cancellationToken) ? options : null;'
    Replace = 'DiagnosticIds.AutoAccessorsOnOneLine, cancellationToken) ? options : options;'
    Tests = 'ElementSeparationTests'
}
