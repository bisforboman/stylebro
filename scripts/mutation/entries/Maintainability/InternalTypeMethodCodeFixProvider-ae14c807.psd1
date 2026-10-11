# BRO1409 (public methods of internal types): the analyzer's skips, then the fix's guards
@{
    File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'
    Find = '.Where(s => changes.Any(c => s.Span.Contains(c.Span)))'
    Replace = ''
    Tests = 'InternalTypeMethodTests'
}
