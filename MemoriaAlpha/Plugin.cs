using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using MemoriaAlpha.Windows;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace MemoriaAlpha;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/pmycommand";

    public Configuration Configuration { get; init; }
    public readonly WindowSystem WindowSystem = new("MemoriaAlpha");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public readonly List<QuestEntry> Quests = new();

    // ToC + data
    public TocService Toc { get; }
    public QuestRepository QuestRepository { get; }
    
    // New: NewsRepository and cached news data
    public NewsRepository NewsRepository { get; }
    public NewsRoot? News { get; private set; }

    // Cached GC string
    public string PlayerGc { get; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        var pluginDir = PluginInterface.AssemblyLocation.DirectoryName!;
        var tocPath = Path.Combine(pluginDir, "toc.json");
        Toc = new TocService(tocPath);

        var dataRoot = Path.Combine(pluginDir, "data");
        QuestRepository = new QuestRepository(dataRoot);

        NewsRepository = new NewsRepository(pluginDir);
        News = NewsRepository.Load();

        var highestPatch = GetHighestCompletedPatch();
        Log.Information($"[Memoria Alpha] Highest completed patch: {highestPatch}");

        // Load ARR MSQ quests (2.0–2.5) from JSON
        Quests.Clear();
        Quests.AddRange(QuestRepository.LoadAllMsqArr());

        // Cache player GC once
        PlayerGc = DetectPlayerGc();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle Memoria Alpha's main window."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        Log.Information($"=== Memoria Alpha loaded ({PluginInterface.Manifest.Name}) ===");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.Toggle();
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();

    // Uses toc.json + quest completion to find highest completed patch
    private string GetHighestCompletedPatch()
    {
        var finalByPatch = Toc.GetFinalMsqByPatch();
        var orderedPatches = finalByPatch.Keys
            .Select(p => new { Patch = p, Version = Version.Parse(p) })
            .OrderBy(x => x.Version)
            .ToList();

        if (orderedPatches.Count == 0)
            return "2.0";

        string highest = orderedPatches.First().Patch;
        foreach (var entry in orderedPatches)
        {
            var allDone = finalByPatch[entry.Patch]
                .All(id => IsQuestCompleted(id));
            if (allDone)
                highest = entry.Patch;
            else
                break;
        }

        return highest;
    }

    // Public so QuestRepository can use it
    public static bool IsQuestCompleted(int questId)
    {
        return QuestManager.IsQuestComplete((ushort)questId);
    }

    // Detect starting city from "Close to Home" quest variants
    public string GetStartCity()
    {
        // Gridania
        if (IsQuestCompleted(65621) || IsQuestCompleted(65659) || IsQuestCompleted(65660))
            return "Gridania";

        // Limsa Lominsa
        if (IsQuestCompleted(65644) || IsQuestCompleted(65645))
            return "Limsa Lominsa";

        // Ul'dah
        if (IsQuestCompleted(66104) || IsQuestCompleted(66105) || IsQuestCompleted(66106))
            return "Ul'dah";

        // If none done yet, just default
        return "Gridania";
    }

    // Detect player GC from Dalamud's PlayerState
    private string DetectPlayerGc()
    {
        // PlayerState.GrandCompany is a RowRef, use its Key value
        var gcKey = (int)PlayerState.GrandCompany.RowId;
        switch (gcKey)
        {
            case 1: return "Maelstrom";
            case 2: return "Twin Adder";
            case 3: return "Immortal Flames";
            default: return ""; // no GC yet
        }
    }
}
