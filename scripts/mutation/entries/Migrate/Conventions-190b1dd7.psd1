# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = '{(fromTemplate ? " (and the empty lines from the template)" : string.Empty)}'
    Replace = '{(true ? " (and the empty lines from the template)" : string.Empty)}'
    Tests = 'MigrationTests.EmptyHeaderLines_MentionTheTemplate_OnlyWithOne'
}
