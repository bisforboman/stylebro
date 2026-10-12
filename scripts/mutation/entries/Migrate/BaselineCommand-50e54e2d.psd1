# Trial 9, tool (2026-10-12): a Traversal project is a workspace
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.cs'
    Find = 'FormatCommand.IsTraversal(Path.Combine(root, f))'
    Replace = 'false'
    Tests = 'MigrationTests.Workspace_ATraversalProject'
}
