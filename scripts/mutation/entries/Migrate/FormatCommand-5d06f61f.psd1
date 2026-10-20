# A linked folder that doesn't exist doesn't move the root (trial 36)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = ' && Directory.Exists(f))
            .Distinct'
    Replace = ')
            .Distinct'
    Tests = 'WorkingRoot_HoldsTheRepositoryFiles'
}
