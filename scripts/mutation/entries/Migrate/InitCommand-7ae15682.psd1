# The built-in rules only leave the block where the Style category or the bulk severity is off
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (!OffSeverity(style))'
    Replace = 'if (true)'
    Tests = 'MigrationTests.StyleOffIds_KeepsRulesTheFileSetsAndACategoryThatOverridesTheBulk'
}
