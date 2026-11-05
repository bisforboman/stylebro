# Only the skip message is held back to be joined.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (!line.Contains("Required references did not load for ", StringComparison.Ordinal) || line.Contains'
    Replace = 'if (line.Contains'
    Tests = 'Format_SeesASkippedProject_WhoseFrameworkHasALineBreak'
}
