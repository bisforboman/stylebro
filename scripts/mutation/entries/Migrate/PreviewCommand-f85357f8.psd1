# The preview collects format's gap lines (trial 36)
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (FormatCommand.IsGapLine(line) && !gaps.Contains(line))'
    Replace = 'if (false)'
    Tests = 'Preview_WithAGapInTheLastRun'
}
