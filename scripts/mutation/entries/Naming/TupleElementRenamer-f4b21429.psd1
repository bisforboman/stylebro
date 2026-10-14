# Format speed (2026-10-14): a project's attempt renames only its own elements
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'foreach (var (document, root, model) in documents.Where(d => onlyIn is null || onlyIn.Contains(d.Document.FilePath ?? d.Document.Name)))'
    Replace = 'foreach (var (document, root, model) in documents)'
    Tests = 'TupleElementNamingTests.ElementsInAnotherProject'
}
