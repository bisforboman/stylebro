# A second run counts the suppressions already carried over
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'already += replacing.IsMatch(text) ? 1 : 0;'
    Replace = ''
    Tests = 'MigrationTests.Suppressions_ASecondRun_SaysTheyWereCarriedOver'
}
