# Commented-out Compile items don't count (trial 36)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Regex.Matches(StyleCopSetup.WithoutComments(File.ReadAllText(project)),'
    Replace = 'Regex.Matches(File.ReadAllText(project),'
    Tests = 'WorkingRoot_HoldsTheRepositoryFiles'
}
