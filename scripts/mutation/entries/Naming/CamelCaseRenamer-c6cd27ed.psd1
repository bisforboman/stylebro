# format's runs load some projects: the names of the others' files (RepositoryNames, 2026-10-11)
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'else if (entry.Kind == RepositoryNames.IdentifierKind && !paths.Contains(entry.Where))'
    Replace = 'else if (entry.Kind == RepositoryNames.IdentifierKind)'
    Tests = 'KeptFindingsTests'
}
