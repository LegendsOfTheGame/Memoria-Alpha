using System;
using System.Collections.Generic;

namespace MemoriaAlpha;

public sealed class DrawerFile
{
    public string expansion { get; set; } = "";   // "2.0"
    public string drawer { get; set; } = "";      // "1-msq"
    public string title { get; set; } = "";
    public List<DrawerQuest> quests { get; set; } = new();
}

public sealed class DrawerQuest
{
    public string Title { get; set; } = "";
    public int[] Id { get; set; } = Array.Empty<int>();
    public string Area { get; set; } = "";
    public string? Start { get; set; }
    public int Level { get; set; }
    public string? Gc { get; set; }
}
