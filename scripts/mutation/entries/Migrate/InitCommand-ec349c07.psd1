# IDE0009's own severity wins over the keys' suffix
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (settings.ContainsKey(ThisQualificationSeverityKey))'
    Replace = 'if (false && settings.ContainsKey(ThisQualificationSeverityKey))'
    Tests = 'MigrationTests.Init_KeepsAnEnforcedThisQualification'
}
