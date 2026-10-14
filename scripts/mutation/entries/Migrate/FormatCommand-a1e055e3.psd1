# Format speed (2026-10-14): a finding outside the root: the copy is formatted whole
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                return null;
            }

            files.Add'
    Replace = 'files.Add'
    Tests = 'MigrationTests.Verify_FormatsOnlyTheFilesWithFindings_InTheCopy'
}
