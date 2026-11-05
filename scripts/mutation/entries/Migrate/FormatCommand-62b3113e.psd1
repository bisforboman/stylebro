# A skip message that never completes is let through after a few lines.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '|| ++joined >= MaxContinuationLines'
    Replace = '|| ++joined >= int.MaxValue'
    Tests = 'SkipLineJoiner_KeepsALineThatNeverCompletes'
}
