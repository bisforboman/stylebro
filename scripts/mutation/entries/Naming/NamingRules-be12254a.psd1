# A global IDE1006 suppression without a target covers the compilation
@{
    File = 'src/StyleBro.Analyzers/Naming/NamingRules.cs'
    Find = 'if (global.Any(g => g.Target is null && g.Scope is null or "module"))'
    Replace = 'if (false)'
    Tests = 'PascalCaseNamingTests.GlobalIde1006Suppressions'
}
