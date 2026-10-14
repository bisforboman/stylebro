# The preview counts suppression ids like the migration (SSH.NET: 25 vs 27).
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '(line[0] == ''+'' ? 1 : -1) * Regex.Matches(line, @"\b(?:BRO|IDE|CA)\d{4}\b").Count : 1;'
    Replace = '1;'
    Tests = 'StyleBro.Tests.MigrationTests.Preview_CountsSuppressionIdsAdded_LikeTheMigration'
}
