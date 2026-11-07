# An import through GetDirectoryNameOfFileAbove is followed (OpenTelemetry's rulesets).
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'var nameAbove = Regex.Match(value, @"^\$\(\[MSBuild\]::GetDirectoryNameOfFileAbove\('
    Replace = 'var nameAbove = Regex.Match(value, @"^\$\(\[MSBuild\]::GetDirectoryNameOfFileAboveX\('
    Tests = 'StyleBro.Tests.MigrationTests.GetDirectoryNameOfFileAbove_IsFollowed_SoTheRulesetIsRead'
}
