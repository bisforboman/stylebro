# No restore advice for left-out packages.config projects
@{
    File = 'src/StyleBro.Migrate/PreviewCommand.cs'
    Find = 'if (incomplete is not null && !OnlyLeftOut(incomplete, packagesConfig))'
    Replace = 'if (incomplete is not null)'
    Tests = 'FullyQualifiedName~Preview_LeavesPackagesConfigProjectsOut'
}
