# Trial 12 (2026-10-12): init follows insert_final_newline, column alignment, BRO1527, BRO1113, one-line jumps, mixed code under an unenforced key
@{
    File = 'src/StyleBro.Migrate/Migration.cs'
    Find = 'return (withCode.Count > 0 ? withCode : projects)'
    Replace = 'return projects'
    Tests = 'MigrationTests.PropsFiles_LeaveOutProjectsWithoutCode'
}
