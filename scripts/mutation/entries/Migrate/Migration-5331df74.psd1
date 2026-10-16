# Folders that turn every analyzer off do not vote on conventions
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '(InitCommand.OptsOut(own) || '
    Replace = '('
    Tests = 'MigrationTests.Conventions_LeaveOptOutFoldersOut'
}
