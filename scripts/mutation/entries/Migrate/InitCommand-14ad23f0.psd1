# init after the eighth trial (2026-10-11): CSharpier, member order, silent keys, unsigned suffixes, evaluated projects
@{
    File = 'src/StyleBro.Migrate/InitCommand.cs'
    Find = 'return projects.Where(p => p.Frameworks.Length > 1)'
    Replace = 'return projects.Where(p => p.Frameworks.Length > 2)'
    Tests = 'MigrationTests.MultiTargetedProjects_AreEvaluated'
}
