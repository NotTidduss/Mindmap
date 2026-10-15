using System;
using Godot;

public partial class TodoList : VBoxContainer
{
    public void Refresh()
    {
        foreach (Node child in GetChildren()) child.QueueFree();

        foreach (Entry entry in Tree.MindMap.Data.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Note)) continue;

            string[] lines = entry.Note.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                int markerIndex = trimmed.IndexOf("???");
                if (markerIndex < 0) continue;

                int textStartIndex = markerIndex + 3;
                string text = textStartIndex < trimmed.Length
                    ? trimmed[textStartIndex..].Trim()
                    : string.Empty;

                Label label = new() { Text = $"{entry.Title}: {text}" };
                AddChild(label);
            }
        }
    }
}
