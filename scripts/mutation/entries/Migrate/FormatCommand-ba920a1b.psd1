# --files of a solution: the folder that holds all its projects.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'Directory.Exists(target) ? target : WorkingRoot(target);'
    Replace = 'Directory.Exists(target) ? target : Path.GetDirectoryName(target)!;'
    Tests = 'MigrationTests.Files_OfASolutionInASubfolder_AreCheckedAgainstTheFolderOfItsProjects'
}
