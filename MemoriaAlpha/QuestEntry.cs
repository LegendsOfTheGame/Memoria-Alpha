namespace MemoriaAlpha;

public enum QuestCategory
{
    MainScenario,
    Chronicles,
    Side,
    AlliedSocieties,
    ClassJob,
    Other,
}

public sealed class QuestEntry
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Area { get; set; } = "";
    public int Level { get; set; }
    public string Expansion { get; set; } = "";
    public QuestCategory Category { get; set; }
    public string Start { get; set; } = ""; // NEW: starting city
    public string Gc { get; set; } = "";
    public bool Completed { get; set; }
}
