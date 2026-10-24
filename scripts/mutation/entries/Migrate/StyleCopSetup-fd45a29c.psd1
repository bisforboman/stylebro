# Trial 52/53: a folder with a project, solution or Directory.Build.* file is not an extracted package
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'if (names.Any(n => Regex.IsMatch(n!, @"(\.(cs|vb|fs)proj'
    Replace = 'if (false && names.Any(n => Regex.IsMatch(n!, @"(\.(cs|vb|fs)proj'
    Tests = 'MigrationTests.ACommittedPackageNextToCode_IsNotAnExtractedPackage'
}
