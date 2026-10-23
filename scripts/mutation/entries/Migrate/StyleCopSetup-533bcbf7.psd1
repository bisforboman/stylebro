# trial 48: restore output in an artifacts folder counts
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'if (File.Exists(Path.Combine(above, "artifacts", "obj", Path.GetFileNameWithoutExtension(project), name)))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.Restored_FindsTheRestoreOutputNextToTheProject_OrInAnArtifactsFolder'
}
