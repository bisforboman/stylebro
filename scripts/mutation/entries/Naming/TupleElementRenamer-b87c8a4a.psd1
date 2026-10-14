# Format speed (2026-10-14): each project's tuple elements on their own when the whole name can't be renamed
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = 'return sets.Count > 1 ? sets.Where(files => wanted.Any(files.Contains)).ToList() : new List<HashSet<string>>();'
    Replace = 'return new List<HashSet<string>>();'
    Tests = 'TupleElementNamingTests.ElementsInAnotherProject'
}
