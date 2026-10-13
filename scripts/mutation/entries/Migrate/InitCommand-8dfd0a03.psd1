# Trial 14: init --project names an existing solution or project
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'if (workspace is { Length: > 0 } && !File.Exists(Path.Combine(root, workspace)))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Init_TakesProject_AndKeepsTheBlockOrderOnASecondRun'
}
