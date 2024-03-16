namespace SaturnClient;

/// <summary>
/// Represents the statistics for an NFL team.
/// </summary>
public class TeamStat
{
    /// <summary>
    /// Gets or sets the name of the team.
    /// </summary>
    public string teamName { get; set; }

    /// <summary>
    /// Gets or sets the number associated with the team.
    /// </summary>
    public string teamNumber { get; set; }

    /// <summary>
    /// Gets or sets the score of the team.
    /// </summary>
    public string teamScore { get; set; }

    /// <summary>
    /// Gets or sets the date of the game.
    /// </summary>
    public DateTime gameDate { get; set; } 
}