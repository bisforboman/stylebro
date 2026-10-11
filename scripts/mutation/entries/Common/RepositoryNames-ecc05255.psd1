# A helper with "Field" in its name (GetPrivateField) is reflection-shaped
@{
    File = 'src/StyleBro.Analyzers/RepositoryNames.cs'
    Find = ' || HasFieldWord(name);'
    Replace = ';'
    Tests = 'KeptFindingsTests'
}
