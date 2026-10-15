# Doc texts with a period are counted for BRO1603
@{
    File = 'src/StyleBro.Migrate/Conventions.cs'
    Find = 'string.Concat(last.TextTokens.Select(t => t.Text)).TrimEnd().EndsWith(''.'')'
    Replace = 'string.Concat(last.TextTokens.Select(t => t.Text)).TrimEnd().EndsWith(''!'')'
    Tests = 'MigrationTests.Conventions_CountDocPeriodsCommentsLangwords'
}
