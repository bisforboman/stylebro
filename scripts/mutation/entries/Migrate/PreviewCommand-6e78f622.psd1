# Trial 18: the packages.config projects the preview leaves out have no changes in the patch
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = '.Where(d => d.Length > 0).Distinct().ToList();'
    Replace = '.Where(d => false).ToList();'
    Tests = 'MigrationTests.Preview_LeavesPackagesConfigProjectsOut_WhenOthersRemain'
}
