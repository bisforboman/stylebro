# Trial 50: the props above a previewed subfolder count
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (ReferencesStyleBro(files.Concat(ImportedFromAbove(root, top))))'
    Replace = 'if (ReferencesStyleBro(files))'
    Tests = 'MigrationTests.Preview_OfASubfolder_SeesTheReferenceAbove'
}
