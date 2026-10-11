# packages.config projects (2026-10-11): installed once
@{
    File = 'src/StyleBro.Migrate/PackagesConfig.cs'
    Find = 'if (project.Contains(@"\StyleBro.Analyzers.", StringComparison.OrdinalIgnoreCase))'
    Replace = 'if (false)'
    Tests = 'MigrationTests.AddPackage_InstallsItThePackagesConfigWay'
}
