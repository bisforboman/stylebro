# A Sonar 'option' row's suppression doesn't suppress the whole rule
@{
    File = 'src/StyleBro.Migrate/Suppressions.cs'
    Find = 'Where(m => !m.OptionOnly && (sonar?.IsOn(m.Sonar) ?? true))'
    Replace = 'Where(m => sonar?.IsOn(m.Sonar) ?? true)'
    Tests = 'MigrationTests.Migrate_SonarOptionRuleInNoWarn_ResetsTheOptionThere'
}
