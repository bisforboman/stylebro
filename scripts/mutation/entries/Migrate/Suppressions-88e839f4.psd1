# StyleSharp read like StyleCop (2026-10-14)
@{
    File = 'src/StyleBro.Migrate/Suppressions.cs'
    Find = 'Where(m => m.Groups[2].Value == "none")'
    Replace = 'Where(m => m.Groups[2].Value == "nothing")'
    Tests = 'MigrationTests.Suppressions_AreNotAddedForRulesTheSettingsTurnOff'
}
