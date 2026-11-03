# BRO1138 converts plain and raw strings on purpose: its fix is not held to the raw string guard (Mediator, real-world CI).
@{
    File = 'src/StyleBro.FormatHost/RawStrings.cs'
    Find = 'if (ruleId == ConvertsStrings)'
    Replace = 'if (ruleId == "none")'
    Tests = 'RawStringsTests'
}
