using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MemoriaAlpha;

public sealed class TocService
{
    public IReadOnlyList<TocEntry> Entries { get; }

    public TocService(string tocPath)
    {
        var json = File.ReadAllText(tocPath);
        Entries = JsonSerializer.Deserialize<List<TocEntry>>(json)
                  ?? new List<TocEntry>();
    }

    public Dictionary<string, int[]> GetFinalMsqByPatch()
        => Entries
            .Where(e => string.Equals(e.Role, "Final", StringComparison.OrdinalIgnoreCase))
            .GroupBy(e => e.Patch)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(e => e.Ids).Distinct().ToArray()
            );
}
