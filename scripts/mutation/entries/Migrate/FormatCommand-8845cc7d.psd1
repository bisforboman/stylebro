# bro format: kept findings don't fail it, files left to change do (2026-10-10)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'please report it.");
                return NotCleanExitCode;'
    Replace = 'please report it.");
                return 0;'
    Tests = 'KeptFindingsTests'
}
