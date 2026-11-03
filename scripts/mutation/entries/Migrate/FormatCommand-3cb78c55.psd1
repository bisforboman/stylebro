# The full check says the files format kept (a raw string literal a fix would change).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var gaps = Gaps(lines.Concat(RecordedRawStringGaps(root, args)));'
    Replace = 'var gaps = Gaps(lines);'
    Tests = 'Verify_SaysWhatFormatSaidAboutAFileTheRawStringGuardKept'
}
