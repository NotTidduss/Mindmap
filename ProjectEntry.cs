using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;

[Serializable]
public partial class ProjectEntry
{
    public int Id { get; set; }

    public string Title { get; set; } = "New Entry";

    public string Note { get; set; } = string.Empty;

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public List<int> ConnectionIds { get; set; } = new();

    [JsonIgnore]
    public Vector2 Position
    {
        get => new(PositionX, PositionY);
        set
        {
            PositionX = value.X;
            PositionY = value.Y;
        }
    }
}
