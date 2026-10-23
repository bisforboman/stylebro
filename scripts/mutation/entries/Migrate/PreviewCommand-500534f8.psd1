# trial 48: the preview leaves an extracted packages .editorconfig files out
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = ' && !InPackage(folder, f)'
    Replace = ''
    Tests = 'MigrationTests.AddIgnoredSettings_LeavesOutTheEditorConfigsOfExtractedPackages'
}
