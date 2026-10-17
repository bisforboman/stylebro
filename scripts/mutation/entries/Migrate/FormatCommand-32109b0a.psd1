# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'using var timings = args.Contains(TimingsOption) ? PhaseTimes.Start() : null;'
    Replace = 'using var timings = PhaseTimes.Start();'
    Tests = 'MigrationTests.Format_Timings_ListsEachPhase'
}
