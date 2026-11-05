# Verify takes the raw string gaps from the copy run (079).
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'copyRawStringGaps.Add(line);'
    Replace = '_ = line;'
    Tests = 'FormatLineEndingsTests.Verify_AFileTheRawStringGuardKeptInTheCopy'
}
