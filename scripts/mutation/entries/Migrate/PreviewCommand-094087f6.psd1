# A --keep copy loses its marker
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '                if (keep)
                {
                    File.Delete(MarkerOf(temp));'
    Replace = '                if (false)
                {
                    File.Delete(MarkerOf(temp));'
    Tests = 'MigrationTests.Preview_AppliesInitAndFormatToACopy_WritesThePatch_AndLeavesTheRepositoryAlone'
}
