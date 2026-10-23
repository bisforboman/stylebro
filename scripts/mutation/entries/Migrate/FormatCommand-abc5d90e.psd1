# Trial 47: doc generation is part of the compile key.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '+ (Documentation ? "|documentation" : string.Empty)'
    Replace = ''
    Tests = 'MigrationTests.Format_RunsTheConfigurationsThatGenerateDocsOfTheirOwn'
}
