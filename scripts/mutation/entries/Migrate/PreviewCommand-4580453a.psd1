# A preview with gaps exits with 3 like format (trial 36)
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = ': notVerified ? FormatCommand.NotVerifiedExitCode : 0;'
    Replace = ': 0;'
    Tests = 'Preview_WithAGapInTheLastRun'
}
