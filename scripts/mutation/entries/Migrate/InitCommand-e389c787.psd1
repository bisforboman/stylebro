# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '.. convention.Key == Conventions.StaticPrefixKey ? StaticPrefixKeys : []]'
    Replace = '.. Array.Empty<string>()]'
    Tests = 'MigrationTests.Init_KeepsTheSCamelCaseLines_OnASecondRun'
}
