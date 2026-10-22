# A build file that only names the file (Meziantous GlobalAnalyzerConfigFiles) does not copy it
@{
    File = 'src/StyleBro.Migrate/Migration.SyncedConfig.cs'
    Find = ' && CopiesFiles.IsMatch(text)'
    Replace = ''
    Tests = 'MigrationTests.PackageCopy_FindsAnOlderCopy'
}
