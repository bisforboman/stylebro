# Trial 22: a file's tested symbols count as the project defines them
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Select(f => f + ":" + string.Join(",", TestedSymbols(f).Where(Defines.Contains))))'
    Replace = '.Select(f => f + ":" + string.Join(",", TestedSymbols(f))))'
    Tests = 'MigrationTests.Format_ComparesHowProjectsParseAFile'
}
