# Without a .gitmodules, no submodules (trial 60).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (!File.Exists(file))
        {
            return [];
        }

        try
        {
            return File.ReadAllLines(file)'
    Replace = 'if (false)
        {
            return [];
        }

        try
        {
            return File.ReadAllLines(file)'
    Tests = 'MigrationTests.Format_NamesSubmodulesThatArentCheckedOut'
}
