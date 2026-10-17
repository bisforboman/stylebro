# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'exit = RunMeasured(args.Where(a => a != TimingsOption).ToArray(), output, runOnce);'
    Replace = 'exit = RunMeasured(args, output, runOnce);'
    Tests = 'MigrationTests.Format_Timings_ListsEachPhase'
}
