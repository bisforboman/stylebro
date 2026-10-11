# Trial 8, tool (2026-10-11): a nested Directory.Build.props that imports its parent's through a property
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = '        || StyleCopSetup.Imports(candidate, root).Any('
    Replace = '        && StyleCopSetup.Imports(candidate, root).Any('
    Tests = 'MigrationTests.AddPackage_FollowsANestedPropsImport'
}
