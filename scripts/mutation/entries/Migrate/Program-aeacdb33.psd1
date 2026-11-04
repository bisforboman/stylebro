# Trial 67: a file that mentions only an IDE id is scanned for suppressions.
@{
    File = 'src/StyleBro.Migrate/Program.cs'
    Find = 'var mentions = new Regex(@"\b(?:S(?:[AX]|ST)?|IDE)\d{3,4}\b", RegexOptions.IgnoreCase);'
    Replace = 'var mentions = new Regex(@"\bS(?:[AX]|ST)?\d{3,4}\b", RegexOptions.IgnoreCase);'
    Tests = 'Init_CarriesIde0040SuppressionsOver'
}
