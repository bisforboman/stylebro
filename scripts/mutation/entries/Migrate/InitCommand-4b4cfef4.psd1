# Two conventions turning BRO1404 off write one line.
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (i < lines.Count && !lines[i].StartsWith(''#'') && lines.IndexOf(lines[i]) < i)'
    Replace = 'if (false)'
    Tests = 'StyleBro.Tests.MigrationTests.Conventions_CountTypesWithoutInternal_AndCaseBlocksAtTheLabel'
}
