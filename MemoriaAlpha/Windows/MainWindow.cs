using System.Numerics;
using System.Linq;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace MemoriaAlpha.Windows;

public class MainWindow : Window
{
    private readonly Plugin plugin;

    private enum TopCategory
    {
        MainScenario,
        Chronicles,
        Side,
        AlliedSocieties,
        ClassJob,
        Other,
    }

    private TopCategory selectedTopCategory = TopCategory.Chronicles;
    private int selectedSubIndex = 0;

    public MainWindow(Plugin plugin)
        : base("Memoria Alpha")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 350),
            MaximumSize = new Vector2(9999, 9999),
        };
    }

    public override void Draw()
    {
        ImGui.Text("Quest Overview (dummy data)");
        ImGui.Separator();

        DrawNewsSection();

        DrawTopCategoryCombo();
        DrawSubCategoryCombo();
        ImGui.Separator();
        DrawQuestTable();
    }

    private void DrawNewsSection()
    {
        if (ImGui.CollapsingHeader("News / Events"))
        {
            var news = plugin.News;
            if (news == null)
            {
                ImGui.TextDisabled("No news data available.");
            }
            else
            {
                // Next maintenance
                var maint = NewsRepository.GetUpcomingMaintenance(news);
                if (maint != null)
                {
                    var startStr = NewsRepository.FormatDateTime(maint.Start);
                    var endStr = NewsRepository.FormatDateTime(maint.End);
                    ImGui.TextColored(new Vector4(1.0f, 0.5f, 0.0f, 1.0f), $"⚠ Maintenance: {startStr} - {endStr}");
                }
                else
                {
                    ImGui.Text("✔ No upcoming maintenance.");
                }

                // Next patch
                var patch = NewsRepository.GetNextPatch(news);
                if (patch != null)
                {
                    var status = NewsRepository.GetEventStatus(patch);
                    ImGui.Text($"📦 {patch.Title}: {status}");
                }

                // Next/active event
                var evt = NewsRepository.GetNextEvent(news);
                if (evt != null)
                {
                    var status = NewsRepository.GetEventStatus(evt);
                    ImGui.Text($"🎉 {evt.Title}: {status}");
                }

                // Attribution
                ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
                ImGui.TextWrapped("News data provided by XIV ToDo (xivtodo.com)");
                ImGui.PopStyleColor();
            }

            ImGui.Separator();
        }
    }

    private void DrawTopCategoryCombo()
    {
        string label = selectedTopCategory switch
        {
            TopCategory.MainScenario => "Main Scenario",
            TopCategory.Chronicles => "Chronicles of a New Era",
            TopCategory.Side => "Sidequests",
            TopCategory.AlliedSocieties => "Allied Society Quests",
            TopCategory.ClassJob => "Class & Job Quests",
            _ => "Other",
        };

        ImGui.SetNextItemWidth(-1);
        if (ImGui.BeginCombo("##TopCategory", label))
        {
            void Option(TopCategory cat, string text)
            {
                bool selected = selectedTopCategory == cat;
                if (ImGui.Selectable(text, selected))
                {
                    selectedTopCategory = cat;
                    selectedSubIndex = 0;
                }
                if (selected)
                    ImGui.SetItemDefaultFocus();
            }

            Option(TopCategory.MainScenario, "Main Scenario");
            Option(TopCategory.Chronicles, "Chronicles of a New Era");
            Option(TopCategory.Side, "Sidequests");
            Option(TopCategory.AlliedSocieties, "Allied Society Quests");
            Option(TopCategory.ClassJob, "Class & Job Quests");
            Option(TopCategory.Other, "Other");

            ImGui.EndCombo();
        }
    }

    private void DrawSubCategoryCombo()
    {
        string[] subs = selectedTopCategory switch
        {
            TopCategory.MainScenario => new[]
            {
                "Seventh Umbral Era (ARR)",
                "Heavensward (HW)",
                "Stormblood (StB)",
                "Shadowbringers (ShB)",
                "Endwalker (EW)",
                "Dawntrail (DT)",
            },
            TopCategory.Chronicles => new[]
            {
                "Primals",
                "The Crystal Tower",
            },
            TopCategory.Side => new[]
            {
                "Tales of the Dragonsong War",
                "Hildibrand",
            },
            TopCategory.AlliedSocieties => new[]
            {
                "ARR – Amalj'aa",
                "ARR – Sylph",
                "ARR – Kobold",
                "ARR – Sahagin",
                "ARR – Ixali",
                "ARR – Intersocietal",
                "HW – Vanu Vanu",
                "HW – Vath",
                "HW – Moogle",
                "HW – Intersocietal",
                "StB – Kojin",
                "StB – Ananta",
                "StB – Namazu",
                "StB – Intersocietal",
                "ShB – Pixie",
                "ShB – Qitari",
                "ShB – Dwarf",
                "ShB – Intersocietal",
                "EW – Arkasodara",
                "EW – Omicron",
                "EW – Loporrit",
                "EW – Intersocietal",
                "DT – Pelupelu",
                "DT – Mamool Ja",
                "DT – Yok Huy",
            },
            TopCategory.ClassJob => new[]
            {
                "Class & Job quests",
            },
            _ => new[] { "All" },
        };

        if (selectedSubIndex >= subs.Length)
            selectedSubIndex = 0;

        ImGui.SetNextItemWidth(-1);
        if (ImGui.BeginCombo("##SubCategory", subs[selectedSubIndex]))
        {
            for (int i = 0; i < subs.Length; i++)
            {
                bool selected = i == selectedSubIndex;
                if (ImGui.Selectable(subs[i], selected))
                {
                    selectedSubIndex = i;
                }
                if (selected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
    }

    private void DrawQuestTable()
    {
        // TEMP: only ARR MSQ is implemented right now
        if (selectedTopCategory == TopCategory.MainScenario &&
            selectedSubIndex > 0) // anything other than "Seventh Umbral Era (ARR)"
        {
            ImGui.Text("This expansion's MSQ is not implemented yet.");
            return;
        }

        var quests = plugin.Quests.AsEnumerable();
        quests = selectedTopCategory switch
        {
            TopCategory.MainScenario => quests.Where(q => q.Category == QuestCategory.MainScenario),
            TopCategory.Chronicles => quests.Where(q => q.Category == QuestCategory.Chronicles),
            TopCategory.Side => quests.Where(q => q.Category == QuestCategory.Side),
            TopCategory.AlliedSocieties => quests.Where(q => q.Category == QuestCategory.AlliedSocieties),
            TopCategory.ClassJob => quests.Where(q => q.Category == QuestCategory.ClassJob),
            _ => quests.Where(q => q.Category == QuestCategory.Other),
        };

        // Filter by starting city and GC for ARR MSQ
        if (selectedTopCategory == TopCategory.MainScenario &&
            selectedSubIndex == 0) // Seventh Umbral Era (ARR)
        {
            var playerStart = plugin.GetStartCity();
            var normalizedStart = playerStart.ToLowerInvariant();
            quests = quests.Where(q =>
                string.IsNullOrEmpty(q.Start) ||
                q.Start.ToLowerInvariant() == normalizedStart);

            var playerGc = plugin.PlayerGc;
            if (!string.IsNullOrEmpty(playerGc))
            {
                quests = quests.Where(q =>
                    string.IsNullOrEmpty(q.Gc) ||
                    q.Gc == playerGc);
            }
        }

        if (selectedTopCategory == TopCategory.Chronicles)
        {
            quests = selectedSubIndex switch
            {
                0 => quests.Where(q => q.Title == "A Recurring Problem"),
                1 => quests.Where(q => q.Title == "Legacy of Allag"),
                _ => quests,
            };
        }

        var list = quests.ToList();
        if (!list.Any())
        {
            ImGui.Text("No quests in this group yet.");
            return;
        }

        if (ImGui.BeginTable("QuestTable", 3,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn(" ");
            ImGui.TableSetupColumn("Title");
            ImGui.TableSetupColumn("Area");
            ImGui.TableHeadersRow();

            foreach (var quest in list)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(quest.Completed ? "✔" : " ");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(quest.Title);
                ImGui.TableSetColumnIndex(2);
                ImGui.Text(quest.Area);
            }

            ImGui.EndTable();
        }
    }
}
