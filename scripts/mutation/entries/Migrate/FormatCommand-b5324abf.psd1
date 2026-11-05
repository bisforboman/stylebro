# The continuation loses its indentation.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'pending += line.Trim();'
    Replace = 'pending += line;'
    Tests = 'Format_SeesASkippedProject_WhoseFrameworkHasALineBreak'
}
