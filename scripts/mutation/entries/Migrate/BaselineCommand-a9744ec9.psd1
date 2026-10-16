# baseline: keep entries outside the folder it ran on
@{
    File = 'src/StyleBro.Migrate/BaselineCommand.cs'
    Find = 'if (!Path.GetFullPath(Path.Combine(baselineFolder, key.Path))'
    Replace = 'if (Path.GetFullPath(Path.Combine(baselineFolder, key.Path))'
    Tests = 'BaselineTests.Generator_KeepsThePreviousEntriesOutsideTheFolderItRanOn'
}
