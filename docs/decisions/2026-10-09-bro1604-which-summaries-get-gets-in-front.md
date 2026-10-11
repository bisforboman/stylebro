# BRO1604: which summaries get 'Gets' in front (2026-10-09)

## Question

A trial on new repositories (Kavita) found BRO1604 putting the accessor words in front of summaries that aren't noun
phrases:

```csharp
// Before
/// <summary>This is the Koreader hash</summary>
public string Hash { get; set; }

/// <summary>Not used - For parity</summary>
public string Document { get; set; }

// After (until now)
/// <summary>Gets or sets this is the Koreader hash</summary>
/// <summary>Gets or sets not used - For parity</summary>
```

Until now only summaries starting with a condition or a verb from a list (`If`, `Returns`, `Is`, ...) were left alone.

## Choices

1. Only fix noun phrases: add the words only when the summary starts with an article (`The`, `A`, `An`), a lower-case
   word, or one of the known prefixes the rule already replaces; anything else isn't reported (there is no good fix).
2. Keep reporting everything StyleCop reports, and grow the skip lists as cases turn up.
3. Report everything, but fix only noun phrases (a warning without a fix).

## Decision

The owner: "Only fix noun phrases" (choice 1). A summary that starts with any other capitalized word gets no
diagnostic, since StyleBro reports only what it can fix:

```csharp
/// <summary>The order number.</summary>        -> Gets or sets the order number.
/// <summary>number of pages.</summary>          -> Gets number of pages.
/// <summary>Gets the total.</summary>           -> Gets or sets the total. (a known prefix, replaced)
/// <summary>This is the Koreader hash</summary> -> not reported
/// <summary>Device id of the user</summary>     -> not reported
```

Lower-case conditions and verbs (`if true, ...`, `returns ...`) stay skipped. StyleCop reports all of these.
