# Only the last format run's gaps count (trial 36)
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '                gaps.Clear();'
    Replace = ''
    Tests = 'Preview_WithAGapInTheLastRun'
}
