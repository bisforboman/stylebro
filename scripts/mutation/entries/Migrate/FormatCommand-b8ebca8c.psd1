# A kept raw string file still lets format record the clean check.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'gaps.All(g => IsHeldBackGap(g) || IsRawStringGap(g))'
    Replace = 'gaps.All(IsHeldBackGap)'
    Tests = 'Verify_SaysWhatFormatSaidAboutAFileTheRawStringGuardKept'
}
