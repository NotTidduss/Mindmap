using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public partial class MindMapData
{
    public List<Entry> Entries { get; set; } = [];
    public int NextId { get; set; } = 1;

    public Entry GetEntry(int id) => Entries.FirstOrDefault(entry => entry.Id == id);
    public Entry CreateEntry(string title, string note, Godot.Vector2 position)
    {
        Entry entry = new()
        {
            Id = NextId++,
            Title = title,
            Note = note,
            Position = position
        };

        Entries.Add(entry);
        return entry;
    }
    public bool RemoveEntry(int id)
    {
        Entry entry = GetEntry(id);
        if (entry is null) return false;

        Entries.Remove(entry);

        foreach (Entry item in Entries)
            item.ConnectionIds.RemoveAll(connectionId => connectionId == id);

        return true;
    }

    public void Clear()
    {
        Entries.Clear();
        NextId = 1;
    }
    public void EnsureNextId()
    {
        int maxId = Entries.Count == 0 ? 0 : Entries.Max(entry => entry.Id);
        NextId = Math.Max(NextId, maxId + 1);
    }
}
