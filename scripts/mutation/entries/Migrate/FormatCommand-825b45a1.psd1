# The line break inside the project name is cut out, so the name is the one the plan knows.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '? Regex.Replace(match.Groups[1].Value, @"\s*[\r\n]+\s*", string.Empty)'
    Replace = '? match.Groups[1].Value'
    Tests = 'Format_SeesASkippedProject_WhoseFrameworkHasALineBreak'
}
