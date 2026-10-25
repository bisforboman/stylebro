using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fifty-fourth trial (Finbuckle.MultiTenant, Sleet, Flurl, Dapper) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Summary_CountsARuleTwoConventionsTurnOffOnce()
    {
        // Finbuckle.MultiTenant: BRO1306 is off for non-private fields and for private constants, and the summary said
        // "Turned 13 rules off" with BRO1306 listed twice.
        var lists = new Conventions.SummaryLists();
        lists.TurnedOff.AddRange(new[] { "BRO1303 (private fields)", "BRO1306 (non-private fields)", "BRO1133 (null checks)", "BRO1306 (private constants/static readonly fields)" });
        lists.Unfollowed.Add("BRO1001 (types' members: 3 of 3)");

        var summary = string.Join("\n", Conventions.Summary(lists));

        Assert.Contains("  Turned 3 rules off because your code mixes both forms: BRO1303 (private fields),\n    BRO1306 (non-private fields; private constants/static readonly fields), BRO1133 (null checks).", summary);
        Assert.Contains("  Turned 1 rule off because your code doesn't do what it enforces: BRO1001 (types' members: 3 of 3).", summary);
    }
}
