# init --write names the package (trial 44)
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = '        if (Migration.PackageCopy(root) is { } packageCopy)'
    Replace = '        if (Migration.PackageCopy(root) is { } packageCopy && false)'
    Tests = 'MigrationTests.Init_SaysWhenTheBuildPutsAPackagesEditorConfigBack'
}
