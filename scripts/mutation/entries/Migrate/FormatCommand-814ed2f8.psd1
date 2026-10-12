# Trial 10, tool (2026-10-12): MSBuild's error instead of a JsonReaderException
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (process.ExitCode != 0)
        {
            throw new ToolException'
    Replace = 'if (process.ExitCode < 0)
        {
            throw new ToolException'
    Tests = 'MigrationTests.Format_AProjectMSBuildCantEvaluate_'
}
