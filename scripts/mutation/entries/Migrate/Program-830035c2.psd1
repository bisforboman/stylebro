# check + preview rejected
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'if (args is ["format", ..] && PreviewCommand.Wants(args) && args.Contains("--verify-no-changes"))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.CheckWithAPreview_FailsBeforeRunningAnything'
}
