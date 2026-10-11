# stylebro-migrate format: kept findings don't fail it, files left to change do (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'return NotCleanExitCode;'
    Replace = 'return 0;'
    Tests = 'KeptFindingsTests'
}
