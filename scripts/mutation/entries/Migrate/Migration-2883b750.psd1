# Trial 26 (2026-10-16): init/migrate
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'catch (Exception e) when (e is IOException or UnauthorizedAccessException)'
    Replace = 'catch (Exception e) when (e is ArgumentException)'
    Tests = 'MigrationTests.ReadOrNull_SkipsALockedFile'
}
