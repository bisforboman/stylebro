# Trial 18: a file that swaps StyleCop for StyleBro itself gets no "remove StyleBro too" advice
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = '|| !Regex.IsMatch(StyleCopSetup.WithoutComments(File.ReadAllText(Path.Combine(root, f))), @"Include\s*=\s*""StyleBro\.Analyzers""", RegexOptions.IgnoreCase)).ToList();'
    Replace = '|| true).ToList();'
    Tests = 'MigrationTests.Migrate_NamesAStyleCopReferenceInASubmodule_AndRecognisesTheSwap'
}
