# IDE0009's own severity wins over the keys' suffix
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (settings.TryGetValue(ThisQualificationSeverityKey, out var ide))'
    Replace = 'if (false && settings.TryGetValue(ThisQualificationSeverityKey, out var ide))'
    Tests = 'MigrationTests.Init_KeepsAnEnforcedThisQualification'
}
