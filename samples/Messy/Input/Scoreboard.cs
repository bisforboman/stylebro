namespace Messy;

/// <summary>Keeps the best score.</summary>
public class Scoreboard
{
    private (string player, int points) _best = (player: "none", points: 0);

    /// <summary>Records a score.</summary>
    /// <param name="player">The player.</param>
    /// <param name="points">The points.</param>
    public void Record(string player, int points)
    {
        if (points > _best.points)
        {
            _best = (player, points);
        }
    }

    /// <summary>Describes the best score.</summary>
    /// <returns>The description.</returns>
    public string Describe() => _best.player + ": " + _best.points;
}
