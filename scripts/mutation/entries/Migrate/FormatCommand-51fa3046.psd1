# An empty submodule folder isn't checked out (trial 60, khellang/Middleware).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '(!Directory.Exists(folder) || !Directory.EnumerateFileSystemEntries(folder).Any())'
    Replace = '(!Directory.Exists(folder))'
    Tests = 'MigrationTests.Format_NamesSubmodulesThatArentCheckedOut'
}
