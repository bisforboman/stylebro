Set-Location /home/user/stylebro
$n = './scripts/mutation/New-Mutation.ps1'
& $n -File 'src/StyleBro.Migrate/Suppressions.cs' -Find '["IDE0040"] = ["BRO1007", "BRO1404"],' -Replace '["IDE0040"] = ["BRO1404"],' -Tests 'Init_CarriesIde0040SuppressionsOver' -Comment 'Trial 67: IDE0040 is replaced by BRO1007 and BRO1404.'
& $n -File 'src/StyleBro.Migrate/Program.cs' -Find 'var mentions = new Regex(@"\b(?:S(?:[AX]|ST)?|IDE)\d{3,4}\b", RegexOptions.IgnoreCase);' -Replace 'var mentions = new Regex(@"\bS(?:[AX]|ST)?\d{3,4}\b", RegexOptions.IgnoreCase);' -Tests 'Init_CarriesIde0040SuppressionsOver' -Comment 'Trial 67: a file that mentions only an IDE id is scanned for suppressions.'
