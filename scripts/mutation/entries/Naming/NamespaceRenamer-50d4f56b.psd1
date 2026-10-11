# format's runs load some projects: the names of the others' files (RepositoryNames, 2026-10-11)
@{
    File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'
    Find = '&& entry.Text.Split('' '').Contains(oldPart))'
    Replace = '&& false)'
    Tests = 'KeptFindingsTests'
}
