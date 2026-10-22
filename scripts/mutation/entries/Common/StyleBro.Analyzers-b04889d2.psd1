# Trial 42: PolyShim, a file-based program gets no StyleBro analyzers
@{
    File = 'src/StyleBro.Package/build/StyleBro.Analyzers.targets'
    Find = 'Condition="''%(Analyzer.NuGetPackageId)'' == ''StyleBro.Analyzers''"'
    Replace = 'Condition="''%(Analyzer.NuGetPackageId)'' == ''none''"'
    Tests = 'MigrationTests.PackageTargets_LeaveAFileBasedProgramWithoutStyleBrosAnalyzers'
}
