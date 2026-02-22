using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public partial class MindMapData
{
    public List<ProjectEntry> Entries { get; set; } = new();

    public int NextId { get; set; } = 1;

    public ProjectEntry CreateEntry(string title, string note, Godot.Vector2 position)
    {
        var entry = new ProjectEntry
        {
            Id = NextId++,
            Title = title,
            Note = note,
            Position = position
        };

        Entries.Add(entry);
        return entry;
    }

    public ProjectEntry GetEntry(int id)
    {
        return Entries.FirstOrDefault(entry => entry.Id == id);
    }

    public bool RemoveEntry(int id)
    {
        var entry = GetEntry(id);
        if (entry is null)
        {
            return false;
        }

        Entries.Remove(entry);

        foreach (var item in Entries)
        {
            item.ConnectionIds.RemoveAll(connectionId => connectionId == id);
        }

        return true;
    }

    public void Clear()
    {
        Entries.Clear();
        NextId = 1;
    }

    public void EnsureNextId()
    {
        var maxId = Entries.Count == 0 ? 0 : Entries.Max(entry => entry.Id);
        NextId = Math.Max(NextId, maxId + 1);
    }
}
