# Shared by Invoke-Mutations.ps1 and New-Mutation.ps1.

# The folder under entries/ an entry for this file belongs in: src/StyleBro.Analyzers/<Area>/ and
# src/StyleBro.CodeFixes/<Area>/ -> <Area>, src/StyleBro.Migrate -> Migrate, anything else -> Common.
function Get-MutationArea([string]$path) {
    if ($path -match '^src/StyleBro\.(Analyzers|CodeFixes)/([^/]+)/') { return $Matches[2] }
    if ($path -like 'src/StyleBro.Migrate/*') { return 'Migrate' }
    'Common'
}
