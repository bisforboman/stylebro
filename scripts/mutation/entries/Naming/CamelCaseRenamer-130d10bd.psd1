# format's runs load some projects: the names of the others' files (RepositoryNames, 2026-10-11)
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = 'if (current.DeclaredAccessibility is Accessibility.Private or Accessibility.NotApplicable)'
    Replace = 'if (false)'
    Tests = 'KeptFindingsTests'
}
