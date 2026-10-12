# format's runs load some projects: the names of the others' files (RepositoryNames, 2026-10-11)
@{
    File = 'src/StyleBro.CodeFixes/Naming/TupleElementRenamer.cs'
    Find = '.Outside.TryGetValue(oldName, out var outside))'
    Replace = '.Outside.TryGetValue(oldName, out var outside) && false)'
    Tests = 'KeptFindingsTests'
}
