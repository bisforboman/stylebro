# bro format (trial 32, CAP): transitive references of a project without the run's framework ask for all frameworks too
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'AfterTargets="IncludeTransitiveProjectReferences"'
    Replace = 'AfterTargets="StyleBroNoSuchTarget"'
    Tests = 'MigrationTests.Format_ATransitiveReferenceOfAProjectWithoutTheRunsFramework'
}
