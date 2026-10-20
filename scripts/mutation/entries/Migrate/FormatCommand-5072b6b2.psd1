# A solution's root holds the repository files its projects link (trial 36, NodaTime)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '            folder = Holding(folder, LinkedFolders(Path.GetFullPath(Path.Combine(listedFrom, project.Replace(''\\'', Path.DirectorySeparatorChar))), repository));'
    Replace = ''
    Tests = 'WorkingRoot_HoldsTheRepositoryFiles'
}
