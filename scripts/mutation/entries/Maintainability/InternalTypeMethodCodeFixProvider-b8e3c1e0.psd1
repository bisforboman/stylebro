# BRO1409 (public methods of internal types): the analyzer's skips, then the fix's guards
@{
    File = 'src/StyleBro.CodeFixes/Maintainability/InternalTypeMethodCodeFixProvider.cs'
    Find = 'return derived.Any(d => d.Locations.Any(l => l.IsInSource && project.GetDocument(l.SourceTree) is null));'
    Replace = 'return false;'
    Tests = 'InternalTypeMethodTests'
}
