# Trial 39: a submodule's project is hidden only when the build's solution is outside the submodule.
@{
    File = 'src/StyleBro.Package/build/StyleBro.Analyzers.targets'
    Find = 'or $(_StyleBroSolutionFolder.StartsWith('
    Replace = 'or !$(_StyleBroSolutionFolder.StartsWith('
    Tests = 'MigrationTests.PackageTargets_HideASubmodulesProject_WhenTheSuperprojectsSolutionBuildsIt'
}
