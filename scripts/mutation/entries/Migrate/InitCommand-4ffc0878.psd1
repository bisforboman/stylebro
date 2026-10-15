# Only the shallowest solutions count without one in the root (a fixture folder's solution lists fixtures)
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'solutions = solutions.Where(s => Depth(s.Folder) == depth).ToList();'
    Replace = ''
    Tests = 'MigrationTests.Init_LeavesFixtureFoldersOutsideTheSolutions'
}
