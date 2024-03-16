using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using NLog;

namespace SaturnClient;

/// <summary>
/// Main Window class that handles the UI and data fetching operations.
/// </summary>
public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient = new();
    private readonly Logger logger = LogManager.GetCurrentClassLogger();
    
    /// <summary>
    /// Initializes the MainWindow component.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        logger.Info("Application started.");
        FetchAndDisplayTeamData();
    }
    
    /// <summary>
    /// Fetches and displays team data asynchronously.
    /// </summary>
    private async void FetchAndDisplayTeamData()
    {
        progressBar.Visibility = Visibility.Visible;

        try
        {
            var correlationIds = await EnqueueAllTeamsAndGetCorrelationIds();
            var allTeamStats = await FetchAllProcessedData(correlationIds);

            var uniqueTeamStats = allTeamStats
                .OrderBy(teamStat => teamStat.gameDate)
                .DistinctBy(teamStat => new { teamStat.teamName, teamStat.teamNumber, teamStat.gameDate })
                .ToList();

            lvTeamData.ItemsSource = uniqueTeamStats;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred: {ex.Message}");
            logger.Error(ex, "Failed to fetch and display team data.");
        }
        finally
        {
            progressBar.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Fetches all processed data for the given correlation IDs.
    /// </summary>
    /// <param name="correlationIds">The dictionary of team numbers and their corresponding correlation IDs.</param>
    /// <returns>A list of TeamStat objects.</returns>
    private async Task<List<TeamStat>> FetchAllProcessedData(Dictionary<int, string> correlationIds)
    {
        var allTeamStats = new List<TeamStat>();

        foreach (var kvp in correlationIds)
        {
            var correlationId = kvp.Value;
            var fetchedData = await FetchProcessedData(correlationId);
            if (fetchedData != null && fetchedData.Count > 0)
            {
                allTeamStats.AddRange(fetchedData);
            }
        }

        return allTeamStats;
    }

    /// <summary>
    /// Enqueues all teams and gets the correlation IDs.
    /// </summary>
    /// <returns>A dictionary mapping team numbers to correlation IDs.</returns>
    private async Task<Dictionary<int, string>> EnqueueAllTeamsAndGetCorrelationIds()
    {
        var correlationIds = new Dictionary<int, string>();

        for (int teamNumber = 1; teamNumber <= 32; teamNumber++)
        {
            var correlationId = await EnqueueTeam(teamNumber);
            if (!string.IsNullOrEmpty(correlationId))
            {
                correlationIds[teamNumber] = correlationId;
            }
        }

        return correlationIds;
    }
    
    /// <summary>
    /// Enqueues a team for processing and retrieves the correlation ID.
    /// </summary>
    /// <param name="teamNumber">The team number to enqueue.</param>
    /// <returns>The correlation ID.</returns>
    private async Task<string> EnqueueTeam(int teamNumber)
    {
        var teamRequest = new TeamRequest { TeamNumber = teamNumber };
        var jsonRequest = JsonSerializer.Serialize(teamRequest);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("http://localhost:5124/Teams/EnqueueTeam", content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(jsonResponse);

        return apiResponse?.correlationId ?? string.Empty;
    }

    /// <summary>
    /// Fetches processed data for a given correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to fetch data for.</param>
    /// <returns>A list of TeamStat objects.</returns>
    private async Task<List<TeamStat>> FetchProcessedData(string correlationId)
    {
        return await RetryPolicy.RetryOnConditionAsync<List<TeamStat>>(
            async () =>
            {
                var response = await _httpClient.GetAsync($"http://localhost:5124/Teams/GetProcessedData/{correlationId}");
                if (!response.IsSuccessStatusCode) throw new HttpRequestException("Data not found or processing not complete.");

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var teamStats = JsonSerializer.Deserialize<List<TeamStat>>(jsonResponse);
                return teamStats ?? new List<TeamStat>();
            },
            result => result == null || result.Count == 0, // Condition for retry, e.g., data not ready
            3, // Max retry count
            TimeSpan.FromSeconds(5) // Delay between retries
        );
    }

    /// <summary>
    /// Represents a request to enqueue a team.
    /// </summary>
    public class TeamRequest
    {
        public int TeamNumber { get; set; }
    }

    /// <summary>
    /// Represents the API response.
    /// </summary>
    public class ApiResponse
    {
        public string correlationId { get; set; }
    }
}
