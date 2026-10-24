# Format host: the copy's folder itself moves to the repository's
@{
    File = 'src/StyleBro.Migrate/FormatCommand.Host.cs'
    Find = 'text == from.TrimEnd(Path.DirectorySeparatorChar) ? to.TrimEnd(Path.DirectorySeparatorChar) : '
    Replace = ''
    Tests = 'MigrationTests.ACopysPaths_MoveToTheRepositorys_AndBack'
}
