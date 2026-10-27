# Fixers for another language are left out (fixture matrix, visual-basic: the VB IDE0036 fixer deleted a C# method).
@{
    File = 'src/StyleBro.FormatHost/FixOrder.cs'
    Find = '.Where(f => languages is null || LanguagesOf(f) is not { Count: > 0 } own || own.Overlaps(languages))'
    Replace = ''
    Tests = 'StyleBro.Tests.FormatHostFixTests.AFixer_ForAnotherLanguage_IsLeftOut'
}
