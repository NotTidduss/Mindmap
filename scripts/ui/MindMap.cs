using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class MindMap : GraphEdit
{
    private const string EntryPrefix = "Entry_";
    private const float NoteBaseHeight = 120f;
    private const float NoteMaxHeight = NoteBaseHeight * 2f;
    private const float NoteHeightPadding = 24f;

    private static readonly Color DefaultModulate = Colors.White;
    private static readonly Color HighlightModulate = Colors.LightGoldenrod;

    public MindMapData Data => _mindMapData;
    private MindMapData _mindMapData = new();


    public override void _Ready()
    {
        ConnectionRequest += OnConnectionRequest;
        DisconnectionRequest += OnDisconnectionRequest;
    }

    private void OnConnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
    {
        if (fromNode == toNode) return;

        string fromName = fromNode.ToString();
        string toName = toNode.ToString();

        if (!IsNodeConnected(fromName, (int)fromPort, toName, (int)toPort))
            ConnectNode(fromName, (int)fromPort, toName, (int)toPort);

        AddConnectionToData(fromName, toName);
    }
    private void OnDisconnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
    {
        string fromName = fromNode.ToString();
        string toName = toNode.ToString();

        if (IsNodeConnected(fromName, (int)fromPort, toName, (int)toPort))
            DisconnectNode(fromName, (int)fromPort, toName, (int)toPort);

        RemoveConnectionFromData(fromName, toName);
    }

    public void AddEntry(Vector2 position)
        => CreateEntryNode(_mindMapData.CreateEntry("New Entry", string.Empty, position));

    public void Highlight(string query)
    {
        Lowlight();
        if (string.IsNullOrEmpty(query)) return;

        List<Entry> matches = [.. _mindMapData.Entries
            .Where(entry => Utils.IsFuzzyMatch(query, entry.Title))
            .Distinct()];

        foreach (Entry entry in matches)
        {
            GraphNode node = GetNodeOrNull<GraphNode>(GetEntryNodeName(entry.Id));
            if (node is null) continue;

            node.Modulate = HighlightModulate;
        }

        Tree.SearchCount.Set(matches.Count);
    }
    public void Lowlight()
    {
        foreach (Entry entry in _mindMapData.Entries)
        {
            GraphNode node = GetNodeOrNull<GraphNode>(GetEntryNodeName(entry.Id));
            if (node is null) continue;

            node.Modulate = DefaultModulate;
        }
    }

    public void Build(MindMapData data)
    {
        ClearGraph();

        _mindMapData = data;
        _mindMapData.EnsureNextId();

        foreach (Entry entry in _mindMapData.Entries)
            CreateEntryNode(entry);

        CallDeferred(nameof(RefreshNoteHeights));

        ReconnectEntries();
    }

    public void UpdateData()
    {
        foreach (Entry entry in _mindMapData.Entries)
        {
            GraphNode node = GetNodeOrNull<GraphNode>(GetEntryNodeName(entry.Id));
            if (node is null) continue;

            entry.Position = node.PositionOffset;

            LineEdit titleEdit = node.GetNodeOrNull<LineEdit>("Content/Title");
            if (titleEdit != null) entry.Title = titleEdit.Text;

            TextEdit noteEdit = node.GetNodeOrNull<TextEdit>("Content/Note");
            if (noteEdit != null) entry.Note = noteEdit.Text;
        }

        RebuildConnections();
    }

    private void RebuildConnections()
    {
        foreach (Entry entry in _mindMapData.Entries) entry.ConnectionIds.Clear();

        var connectionList = GetConnectionList();
        foreach (Godot.Collections.Dictionary connection in connectionList)
        {
            string fromNodeName = connection["from_node"].AsString();
            string toNodeName = connection["to_node"].AsString();

            int? fromId = TryParseEntryId(fromNodeName);
            int? toId = TryParseEntryId(toNodeName);

            if (!fromId.HasValue || !toId.HasValue) continue;

            Entry fromEntry = _mindMapData.GetEntry(fromId.Value);
            if (fromEntry is null) continue;

            if (!fromEntry.ConnectionIds.Contains(toId.Value))
                fromEntry.ConnectionIds.Add(toId.Value);
        }
    }

    private void ReconnectEntries()
    {
        ClearConnections();

        foreach (Entry entry in _mindMapData.Entries)
        {
            string fromNodeName = GetEntryNodeName(entry.Id);

            foreach (int connectionId in entry.ConnectionIds)
            {
                string toNodeName = GetEntryNodeName(connectionId);

                if (GetNodeOrNull<GraphNode>(fromNodeName) == null
                || GetNodeOrNull<GraphNode>(toNodeName) == null)
                    continue;

                if (!IsNodeConnected(fromNodeName, 0, toNodeName, 0))
                    ConnectNode(fromNodeName, 0, toNodeName, 0);
            }
        }
    }

    private void AddConnectionToData(string fromNodeName, string toNodeName)
    {
        int? fromId = TryParseEntryId(fromNodeName);
        int? toId = TryParseEntryId(toNodeName);
        if (!fromId.HasValue || !toId.HasValue) return;

        Entry fromEntry = _mindMapData.GetEntry(fromId.Value);
        if (fromEntry is null) return;

        if (!fromEntry.ConnectionIds.Contains(toId.Value))
            fromEntry.ConnectionIds.Add(toId.Value);
    }
    private void RemoveConnectionFromData(string fromNodeName, string toNodeName)
    {
        int? fromId = TryParseEntryId(fromNodeName);
        int? toId = TryParseEntryId(toNodeName);
        if (!fromId.HasValue || !toId.HasValue) return;

        Entry fromEntry = _mindMapData.GetEntry(fromId.Value);
        if (fromEntry is null) return;

        fromEntry.ConnectionIds.RemoveAll(id => id == toId.Value);
    }

    private void ClearGraph()
    {
        List<Node> nodesToRemove = [];

        foreach (Node child in GetChildren())
            if (child is GraphNode graphNode && graphNode.Name.ToString().StartsWith(EntryPrefix))
                nodesToRemove.Add(graphNode);

        foreach (Node node in nodesToRemove) node.QueueFree();
    }

    private void CreateEntryNode(Entry entry)
    {
        GraphNode node = new()
        {
            Name = GetEntryNodeName(entry.Id),
            Title = entry.Title,
            PositionOffset = entry.Position
        };
        VBoxContainer content = new()
        {
            Name = "Content"
        };
        LineEdit titleEdit = new()
        {
            Name = "Title",
            Text = entry.Title,
            PlaceholderText = "Entry title"
        };
        TextEdit noteEdit = new()
        {
            Name = "Note",
            Text = entry.Note,
            CustomMinimumSize = new Vector2(280f, NoteBaseHeight),
            WrapMode = TextEdit.LineWrappingMode.Boundary
        };

        titleEdit.TextChanged += newText =>
        {
            entry.Title = newText;
            node.Title = newText;
            Tree.TodoList.Refresh();
        };
        noteEdit.TextChanged += () =>
        {
            entry.Note = noteEdit.Text;
            UpdateNoteHeight(noteEdit, node);
            Tree.TodoList.Refresh();
        };
        noteEdit.Resized += () =>
        {
            UpdateNoteHeight(noteEdit, node);
        };

        content.AddChild(titleEdit);
        content.AddChild(noteEdit);
        node.AddChild(content);
        node.SetSlot(0, true, 0, Colors.White, true, 0, Colors.White);
        AddChild(node);
        UpdateNoteHeight(noteEdit, node);
    }

    private void RefreshNoteHeights()
    {
        foreach (Entry entry in _mindMapData.Entries)
        {
            GraphNode node = GetNodeOrNull<GraphNode>(GetEntryNodeName(entry.Id));
            if (node is null) continue;

            TextEdit noteEdit = node.GetNodeOrNull<TextEdit>("Content/Note");
            if (noteEdit is null) continue;

            UpdateNoteHeight(noteEdit, node);
        }
    }


    private static string GetEntryNodeName(int id) => $"{EntryPrefix}{id}";
    private static int? TryParseEntryId(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !name.StartsWith(EntryPrefix))
            return null;

        string idText = name[EntryPrefix.Length..];
        return int.TryParse(idText, out int id) ? id : null;
    }
    private static void UpdateNoteHeight(TextEdit note, GraphNode entry)
    {
        int logicalLineCount = Math.Max(1, note.GetLineCount());
        int visualLineCount = 0;

        for (int line = 0; line < logicalLineCount; line++)
            visualLineCount += 1 + note.GetLineWrapCount(line);

        float contentHeight = visualLineCount * note.GetLineHeight() + NoteHeightPadding;
        float targetHeight = Mathf.Clamp(contentHeight, NoteBaseHeight, NoteMaxHeight);
        note.CustomMinimumSize = new Vector2(note.CustomMinimumSize.X, targetHeight);

        Vector2 minimumSize = entry.GetCombinedMinimumSize();
        float targetNodeWidth = Mathf.Max(entry.Size.X, minimumSize.X);
        entry.Size = new Vector2(targetNodeWidth, minimumSize.Y);
    }
}
