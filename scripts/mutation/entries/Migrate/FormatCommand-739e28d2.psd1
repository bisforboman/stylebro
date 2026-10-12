# Trial 10, tool (2026-10-12): the packages.config restore advice only with packages.config
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '(packagesConfig ? OldStyleRestore : OldStylePackageReferenceRestore)'
    Replace = 'OldStyleRestore'
    Tests = 'MigrationTests.Format_OldStyleProjects_'
}
