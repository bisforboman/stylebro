# A whitespace block that is still written is not stale
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '!nested.ContainsKey(s) && '
    Replace = ''
    Tests = 'MigrationTests.Migrate_ANestedEditorConfigTheFilesDontFollow_GetsTheDecision_AndASecondWriteChangesNothing'
}
