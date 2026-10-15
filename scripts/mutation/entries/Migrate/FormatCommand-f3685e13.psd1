# Trial 23: build output isn't listed
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '&& !f[own.Length..].Split(''\\'', ''/'').Any(s => s is "obj" or "bin")'
    Replace = ''
    Tests = 'MigrationTests.LaterFrameworks_GetOnlyTheFilesThatCompileDifferently'
}
