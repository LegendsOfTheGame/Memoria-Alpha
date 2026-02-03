using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MemoriaAlpha;

// Root model matching news.json structure
public sealed class NewsRoot
{
    [JsonPropertyName("maintenance")]
    public MaintenanceInfo? Maintenance { get; set; }

    [JsonPropertyName("latestCountdownID")]
    public int LatestCountdownID { get; set; }

    [JsonPropertyName("countdowns")]
    public List<CountdownEntry> Countdowns { get; set; } = new();

    [JsonPropertyName("latestID")]
    public int LatestID { get; set; }

    [JsonPropertyName("news")]
    public List<NewsEntry> News { get; set; } = new();
}

public sealed class MaintenanceInfo
{
    [JsonPropertyName("updated")]
    public bool Updated { get; set; }

    [JsonPropertyName("start")]
    public long Start { get; set; } // Unix timestamp

    [JsonPropertyName("end")]
    public long End { get; set; } // Unix timestamp
}

public sealed class CountdownEntry
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = ""; // "patch" or "event"

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("start")]
    public long Start { get; set; } // Unix timestamp

    [JsonPropertyName("end")]
    public long? End { get; set; } // Unix timestamp (optional for patches)
}

public sealed class NewsEntry
{
    [JsonPropertyName("ID")]
    public int ID { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("published")]
    public string Published { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

public sealed class NewsRepository
{
    private readonly string newsPath;

    public NewsRepository(string pluginDir)
    {
        newsPath = Path.Combine(pluginDir, "data", "news.json");
    }

    public NewsRoot? Load()
    {
        try
        {
            if (!File.Exists(newsPath))
            {
                Plugin.Log.Warning($"[Memoria Alpha] news.json not found at {newsPath}");
                return null;
            }

            var json = File.ReadAllText(newsPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Plugin.Log.Warning("[Memoria Alpha] news.json is empty");
                return null;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

            var root = JsonSerializer.Deserialize<NewsRoot>(json, options);
            if (root == null)
            {
                Plugin.Log.Warning("[Memoria Alpha] Failed to deserialize news.json");
                return null;
            }

            Plugin.Log.Information($"[Memoria Alpha] Loaded news.json: {root.Countdowns.Count} countdowns, {root.News.Count} news entries");
            return root;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[Memoria Alpha] Error loading news.json");
            return null;
        }
    }

    // Helper: Get next maintenance (if upcoming)
    public static MaintenanceInfo? GetUpcomingMaintenance(NewsRoot? news)
    {
        if (news?.Maintenance == null || !news.Maintenance.Updated)
            return null;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (news.Maintenance.Start > now)
            return news.Maintenance;

        return null;
    }

    // Helper: Get next patch countdown
    public static CountdownEntry? GetNextPatch(NewsRoot? news)
    {
        if (news == null)
            return null;

        // Return the most recent patch (either upcoming or just released)
        return news.Countdowns
            .Where(c => c.Type == "patch")
            .OrderByDescending(c => c.Start)
            .FirstOrDefault();
    }

    // Helper: Get next/active event countdown
    public static CountdownEntry? GetNextEvent(NewsRoot? news)
    {
        if (news == null)
            return null;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // First, check for active events (started but not ended)
        var activeEvent = news.Countdowns
            .Where(c => c.Type == "event" && c.Start <= now && (c.End == null || c.End > now))
            .OrderBy(c => c.End ?? long.MaxValue)
            .FirstOrDefault();

        if (activeEvent != null)
            return activeEvent;

        // Otherwise, return next upcoming event
        return news.Countdowns
            .Where(c => c.Type == "event" && c.Start > now)
            .OrderBy(c => c.Start)
            .FirstOrDefault();
    }

    // Helper: Check if event is currently active
    public static bool IsEventActive(CountdownEntry countdown, long now)
    {
        return countdown.Start <= now && (countdown.End == null || countdown.End > now);
    }

    // Helper: Format countdown status text with days, hours, minutes // changed
    public static string GetEventStatus(CountdownEntry countdown)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (countdown.Type == "patch")
        {
            return countdown.Start <= now ? "Currently available" : $"Releases {FormatDate(countdown.Start)}";
        }

        // Event
        if (countdown.Start <= now && countdown.End != null && countdown.End > now)
        {
            var secondsLeft = countdown.End.Value - now; // changed
            var days = secondsLeft / 86400; // changed
            var hours = (secondsLeft % 86400) / 3600; // changed
            var minutes = (secondsLeft % 3600) / 60; // changed
            return $"Ending in {days}d {hours}h {minutes}m"; // changed
        }

        if (countdown.Start > now)
        {
            return $"Starts {FormatDate(countdown.Start)}";
        }

        return "Ended";
    }

    // Helper: Get latest news entry
    public static NewsEntry? GetLatestNews(NewsRoot? news)
    {
        return news?.News.FirstOrDefault();
    }

    // Helper: Format Unix timestamp to readable date
    public static string FormatDate(long unixTimestamp)
    {
        var dt = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        return dt.ToString("MMM dd, yyyy");
    }

    // Helper: Format Unix timestamp to readable date + time
    public static string FormatDateTime(long unixTimestamp)
    {
        var dt = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        return dt.ToString("MMM dd, yyyy HH:mm 'UTC'");
    }
}
