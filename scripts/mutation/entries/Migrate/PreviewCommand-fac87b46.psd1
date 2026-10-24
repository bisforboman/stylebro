# Trial 50: the preview passes its copy as the top
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'var package = AddPackage(File.Exists(target) ? Path.GetDirectoryName(target)! : target, copy);'
    Replace = 'var package = AddPackage(File.Exists(target) ? Path.GetDirectoryName(target)! : target);'
    Tests = 'MigrationTests.Preview_OfASubfolder_SeesTheReferenceAbove'
}
