# The skip message may carry a line break (a target framework with one): the dot must span it.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'or referenced project", RegexOptions.Singleline)'
    Replace = 'or referenced project")'
    Tests = 'Format_SeesASkippedProject_WhoseFrameworkHasALineBreak'
}
