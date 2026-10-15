# only the changed projects' generated files
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = '.Where(f => !changedSet.Contains(f) && File.Exists(f) && ProjectOf(f) is { } project && changedProjects.Contains(project) != otherProjects && !NotCompiled(f, project) && IsGenerated(f, build))'
    Replace = '.Where(f => !changedSet.Contains(f) && File.Exists(f) && IsGenerated(f, build))'
    Tests = 'MigrationTests.Format_SaysToRebuildCheckedInGeneratedFiles'
}
