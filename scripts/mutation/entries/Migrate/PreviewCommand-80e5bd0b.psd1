# Trial 8, tool (2026-10-11): projects that turn central package management off get a versioned reference
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'var optOuts = packages is null ? [] :'
    Replace = 'var optOuts = true ? [] :'
    Tests = 'MigrationTests.AddPackage_GivesProjectsWithoutCentralPackageManagement'
}
