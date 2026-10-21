# Trial 39: the build's solution decides whether a submodule's own project is hidden.
@{
    File = 'src/StyleBro.Package/build/StyleBro.Analyzers.targets'
    Find = '''$(SolutionDir)'' != '''' and ''$(SolutionDir)'' != ''*Undefined*'''
    Replace = '''$(SolutionDir)'' == ''never'''
    Tests = 'MigrationTests.PackageTargets_HideASubmodulesProject_WhenTheSuperprojectsSolutionBuildsIt'
}
