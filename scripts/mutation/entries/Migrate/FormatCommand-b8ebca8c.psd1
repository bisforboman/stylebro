# A kept raw string file still lets format record the clean check.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'gaps.All(g => IsNoteGap(g) || IsRawStringGap(g))'
    Replace = 'gaps.All(IsNoteGap)'
    Tests = 'Verify_SaysWhatFormatSaidAboutAFileTheRawStringGuardKept'
}
