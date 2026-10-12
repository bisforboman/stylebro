# Trial 10, tool (2026-10-12): Directory$(MSBuildProjectExtension).props is followed
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = ': m.Groups[1].Value.Equals("MSBuildProjectExtension", StringComparison.OrdinalIgnoreCase) ? ".csproj"'
    Replace = ': false ? ".csproj"'
    Tests = 'MigrationTests.Migrate_AReferenceInAFileImportedByProjectExtension_'
}
