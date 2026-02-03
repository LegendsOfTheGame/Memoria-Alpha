using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MemoriaAlpha;

public sealed class QuestRepository
{
    private readonly string dataRoot;

    public QuestRepository(string dataRoot)
    {
        this.dataRoot = dataRoot;
    }

    public List<QuestEntry> LoadAllMsqArr()
    {
        var result = new List<QuestEntry>();

        // ARR MSQ: 2.0–2.5
        var arrMsqDirs = new[]
        {
            Path.Combine(dataRoot, "2.x", "2.0", "1-msq.json"),
            // When ready, uncomment as you add real files:
            // Path.Combine(dataRoot, "2.x", "2.1", "1-msq.json"),
            // Path.Combine(dataRoot, "2.x", "2.2", "1-msq.json"),
            // Path.Combine(dataRoot, "2.x", "2.3", "1-msq.json"),
            // Path.Combine(dataRoot, "2.x", "2.4", "1-msq.json"),
            // Path.Combine(dataRoot, "2.x", "2.5", "1-msq.json"),
        };

        foreach (var path in arrMsqDirs.Where(File.Exists))
        {
            var json   = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                continue;

            var drawer = JsonSerializer.Deserialize<DrawerFile>(json)!;

            foreach (var q in drawer.quests)
            {
                result.Add(new QuestEntry
                {
                    Id        = q.Id[0],  // for now: first Id only
                    Title     = q.Title,
                    Area      = q.Area,
                    Level     = q.Level,
                    Expansion = drawer.expansion,
                    Category  = QuestCategory.MainScenario,
                    Start     = q.Start ?? "",  // starting city (Gridania / Limsa Lominsa / Ul'dah)
                    Gc        = q.Gc ?? "", 
                    Completed = q.Id.Any(Plugin.IsQuestCompleted),
                });
            }
        }

        Plugin.Log.Information($"[Memoria Alpha] LoadAllMsqArr loaded {result.Count} quests.");
        return result;
    }
}
