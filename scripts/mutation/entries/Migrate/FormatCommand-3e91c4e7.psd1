# Trial 37: a query asking for a restore itself keeps it
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'arguments.Count > 0 && arguments[0] == "msbuild" && !arguments.Any('
    Replace = 'arguments.Count > 0 && arguments[0] == "msbuild" && arguments.Any('
    Tests = 'WithoutRestore_TurnsTheRestoreOff'
}
