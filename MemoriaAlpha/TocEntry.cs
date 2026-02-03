using System;

namespace MemoriaAlpha;

public sealed class TocEntry
{
    public string Patch { get; set; } = "";      // "2.0".."2.5"
    public string Expansion { get; set; } = "";  // "ARR"
    public string Role { get; set; } = "";       // "Start" or "Final"
    public string Name { get; set; } = "";
    public int[] Ids { get; set; } = Array.Empty<int>();
}
