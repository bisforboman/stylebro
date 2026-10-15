# Trial 22: latest is the SDK's newest C# version (b5a)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return $"{sdk + 4}.0";'
    Replace = 'return value;'
    Tests = 'MigrationTests.Format_ComparesHowProjectsParseAFile'
}
