# A project's root holds the repository files it links (trial 36)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '            return Holding(folder, LinkedFolders(workspace, RepositoryRoot(folder)));'
    Replace = '            return folder;'
    Tests = 'WorkingRoot_HoldsTheRepositoryFiles'
}
