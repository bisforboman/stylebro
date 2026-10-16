# Trial 25: a source generator's output emitted into the repository is generated
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (Regex.IsMatch(build, @"<EmitCompilerGeneratedFiles>\s*true\s*<", RegexOptions.IgnoreCase))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.GeneratedFileNote_LeavesOutDesignersAndToolOutput_NoBuildWrites'
}
