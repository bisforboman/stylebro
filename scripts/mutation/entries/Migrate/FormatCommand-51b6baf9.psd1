# --files gives the run the whole repository's names (it renamed what the full run kept)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '        using var namesVariable = new KeptVariable(names, RepositoryNames.Variable);'
    Replace = ''
    Tests = 'KeptFindingsTests'
}
