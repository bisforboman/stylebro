# The kept files of every run are said, not only the last runs.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Concat(rawStringGaps));'
    Replace = ');'
    Tests = 'Verify_SaysWhatFormatSaidAboutAFileTheRawStringGuardKept'
}
