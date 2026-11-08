# Backlog 086: --pay-off takes folders and files, not --project
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.cs'
    Find = 'if (args.Contains("--project"))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.PayOff_NothingToDo_AndUsageErrors'
}
