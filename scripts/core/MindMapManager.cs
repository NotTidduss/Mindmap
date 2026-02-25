using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public partial class MindMapManager : GraphEdit
{
    [Export] public NodePath TodoListContainerPath;

    private const string EntryNodePrefix = "Entry_";
    private const float NoteBaseHeight = 120f;
    private const float NoteMaxHeight = NoteBaseHeight * 2f;
    private const float NoteHeightPadding = 24f;
    private const float MinGraphZoom = 0.0001f;

    private static readonly Color DefaultEntryModulate = Colors.White;
    private static readonly Color SearchMatchEntryModulate = Colors.LightGoldenrod;

    private MindMapData _mindMapData = new();
    private readonly List<int> _searchMatchIds = new();
    private int _currentSearchMatchIndex = -1;

    private VBoxContainer _todoListContainer;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public override void _Ready()
    {
        EnableLowProcessorMode();

        ConnectionRequest += OnConnectionRequest;
        DisconnectionRequest += OnDisconnectionRequest;

        if (TodoListContainerPath != null && !string.IsNullOrWhiteSpace(TodoListContainerPath.ToString()))
        {
            _todoListContainer = GetNodeOrNull<VBoxContainer>(TodoListContainerPath);
        }

        RefreshTodoList();
    }

    private void OnConnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
    {
        if (fromNode == toNode)
        {
            return;
        }

        var fromName = fromNode.ToString();
        var toName = toNode.ToString();

        if (!IsNodeConnected(fromName, (int)fromPort, toName, (int)toPort))
        {
            ConnectNode(fromName, (int)fromPort, toName, (int)toPort);
        }

        AddConnectionToData(fromName, toName);
    }

    private void OnDisconnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
    {
        var fromName = fromNode.ToString();
        var toName = toNode.ToString();

        if (IsNodeConnected(fromName, (int)fromPort, toName, (int)toPort))
        {
            DisconnectNode(fromName, (int)fromPort, toName, (int)toPort);
        }

        RemoveConnectionFromData(fromName, toName);
    }

    public ProjectEntry AddEntry(Vector2 position)
    {
        var entry = _mindMapData.CreateEntry("New Entry", string.Empty, position);
        CreateEntryNode(entry);
        RefreshTodoList();
        return entry;
    }

    public int ApplyTitleSearch(string query)
    {
        PullVisualStateToData();

        _searchMatchIds.Clear();
        _currentSearchMatchIndex = -1;

        var normalizedQuery = query?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(normalizedQuery))
        {
            ClearSearchHighlighting();
            return 0;
        }

        var scoredMatches = new List<(int Id, int Score)>();
        foreach (var entry in _mindMapData.Entries)
        {
            var score = TitleFuzzyMatcher.GetScore(normalizedQuery, entry.Title);
            if (score < 0)
            {
                continue;
            }

            scoredMatches.Add((entry.Id, score));
        }

        foreach (var match in scoredMatches.OrderByDescending(match => match.Score).ThenBy(match => match.Id))
        {
            _searchMatchIds.Add(match.Id);
        }

        ApplySearchHighlighting();

        if (_searchMatchIds.Count > 0)
        {
            _currentSearchMatchIndex = 0;
            FocusEntryById(_searchMatchIds[_currentSearchMatchIndex]);
        }

        return _searchMatchIds.Count;
    }

    public void ResetTitleSearch()
    {
        _searchMatchIds.Clear();
        _currentSearchMatchIndex = -1;
        ClearSearchHighlighting();
    }

    public void RefreshTodoList()
    {
        if (_todoListContainer is null)
        {
            return;
        }

        foreach (var child in _todoListContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var entry in _mindMapData.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Note))
            {
                continue;
            }

            var lines = entry.Note.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("Todo:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var text = trimmed.Length > 5 ? trimmed.Substring(5).Trim() : string.Empty;
                var label = new Label
                {
                    Text = $"{entry.Title}: {text}"
                };
                _todoListContainer.AddChild(label);
            }
        }
    }

    public bool SaveProject(string path)
    {
        PullVisualStateToData();
        RebuildConnectionIdsFromGraph();

        var json = JsonSerializer.Serialize(_mindMapData, _jsonOptions);

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushError($"Cannot open file for writing: {path}");
            return false;
        }

        file.StoreString(json);
        return true;
    }

    public bool LoadProject(string path)
    {
        if (!FileAccess.FileExists(path))
        {
            GD.PushWarning($"Save file does not exist: {path}");
            return false;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"Cannot open file for reading: {path}");
            return false;
        }

        var json = file.GetAsText();

        MindMapData loaded;
        try
        {
            loaded = JsonSerializer.Deserialize<MindMapData>(json, _jsonOptions);
        }
        catch (Exception exception)
        {
            GD.PushError($"Failed to deserialize project: {exception.Message}");
            return false;
        }

        if (loaded is null)
        {
            GD.PushError("Loaded project data is null.");
            return false;
        }

        _mindMapData = loaded;
        _mindMapData.EnsureNextId();

        ClearAllEntryNodes();

        foreach (var entry in _mindMapData.Entries)
        {
            CreateEntryNode(entry);
        }

        CallDeferred(nameof(RefreshAllEntryNoteHeights));

        RestoreVisualConnectionsFromData();
        ResetTitleSearch();
        RefreshTodoList();
        return true;
    }

    private void EnableLowProcessorMode()
    {
        OS.LowProcessorUsageMode = true;
    }

    private void PullVisualStateToData()
    {
        foreach (var entry in _mindMapData.Entries)
        {
            var node = GetNodeOrNull<GraphNode>(GetEntryNodeName(entry.Id));
            if (node is null)
            {
                continue;
            }

            entry.Position = node.PositionOffset;

            var titleEdit = node.GetNodeOrNull<LineEdit>("Content/Title");
            if (titleEdit != null)
            {
                entry.Title = titleEdit.Text;
            }

            var noteEdit = node.GetNodeOrNull<TextEdit>("Content/Note");
            if (noteEdit != null)
            {
                entry.Note = noteEdit.Text;
            }
        }
    }

    private void RebuildConnectionIdsFromGraph()
    {
        foreach (var entry in _mindMapData.Entries)
        {
            entry.ConnectionIds.Clear();
        }

        var connectionList = GetConnectionList();
        foreach (Godot.Collections.Dictionary connection in connectionList)
        {
            var fromNodeName = connection["from_node"].AsString();
            var toNodeName = connection["to_node"].AsString();

            var fromId = TryParseEntryId(fromNodeName);
            var toId = TryParseEntryId(toNodeName);

            if (!fromId.HasValue || !toId.HasValue)
            {
                continue;
            }

            var fromEntry = _mindMapData.GetEntry(fromId.Value);
            if (fromEntry is null)
            {
                continue;
            }

            if (!fromEntry.ConnectionIds.Contains(toId.Value))
            {
                fromEntry.ConnectionIds.Add(toId.Value);
            }
        }
    }

    private void RestoreVisualConnectionsFromData()
    {
        ClearConnections();

        foreach (var entry in _mindMapData.Entries)
        {
            var fromNodeName = GetEntryNodeName(entry.Id);

            foreach (var connectionId in entry.ConnectionIds)
            {
                var toNodeName = GetEntryNodeName(connectionId);

                if (GetNodeOrNull<GraphNode>(fromNodeName) == null || GetNodeOrNull<GraphNode>(toNodeName) == null)
                {
                    continue;
                }

                if (!IsNodeConnected(fromNodeName, 0, toNodeName, 0))
                {
                    ConnectNode(fromNodeName, 0, toNodeName, 0);
                }
            }
        }
    }

    private void AddConnectionToData(string fromNodeName, string toNodeName)
    {
        var fromId = TryParseEntryId(fromNodeName);
        var toId = TryParseEntryId(toNodeName);

        if (!fromId.HasValue || !toId.HasValue)
        {
            return;
        }

        var fromEntry = _mindMapData.GetEntry(fromId.Value);
        if (fromEntry is null)
        {
            return;
        }

        if (!fromEntry.ConnectionIds.Contains(toId.Value))
        {
            fromEntry.ConnectionIds.Add(toId.Value);
        }
    }

    private void RemoveConnectionFromData(string fromNodeName, string toNodeName)
    {
        var fromId = TryParseEntryId(fromNodeName);
        var toId = TryParseEntryId(toNodeName);

        if (!fromId.HasValue || !toId.HasValue)
        {
            return;
        }

        var fromEntry = _mindMapData.GetEntry(fromId.Value);
        if (fromEntry is null)
        {
            return;
        }

        fromEntry.ConnectionIds.RemoveAll(id => id == toId.Value);
    }

    private GraphNode CreateEntryNode(ProjectEntry entry)
    {
        var node = new GraphNode
        {
            Name = GetEntryNodeName(entry.Id),
            Title = entry.Title,
            PositionOffset = entry.Position
        };

        var content = new VBoxContainer
        {
            Name = "Content"
        };

        var titleEdit = new LineEdit
        {
            Name = "Title",
            Text = entry.Title,
            PlaceholderText = "Entry title"
        };

        var noteEdit = new TextEdit
        {
            Name = "Note",
            Text = entry.Note,
            CustomMinimumSize = new Vector2(280f, NoteBaseHeight),
            WrapMode = TextEdit.LineWrappingMode.Boundary
        };

        UpdateNoteEditorHeight(noteEdit, node);

        titleEdit.TextChanged += (string newText) =>
        {
            entry.Title = newText;
            node.Title = newText;
            RefreshTodoList();
        };

        noteEdit.TextChanged += () =>
        {
            entry.Note = noteEdit.Text;
            UpdateNoteEditorHeight(noteEdit, node);
            RefreshTodoList();
        };

        noteEdit.Resized += () =>
        {
            UpdateNoteEditorHeight(noteEdit, node);
        };

        content.AddChild(titleEdit);
        content.AddChild(noteEdit);
        node.AddChild(content);

        node.SetSlot(0, true, 0, Colors.White, true, 0, Colors.White);

        AddChild(node);
        return node;
    }

    private static void UpdateNoteEditorHeight(TextEdit noteEdit, GraphNode entryNode)
    {
        var logicalLineCount = Math.Max(1, noteEdit.GetLineCount());
        var visualLineCount = 0;

        for (var line = 0; line < logicalLineCount; line++)
        {
            visualLineCount += 1 + noteEdit.GetLineWrapCount(line);
        }

        var contentHeight = visualLineCount * noteEdit.GetLineHeight() + NoteHeightPadding;
        var targetHeight = Mathf.Clamp(contentHeight, NoteBaseHeight, NoteMaxHeight);

        noteEdit.CustomMinimumSize = new Vector2(noteEdit.CustomMinimumSize.X, targetHeight);

        var minimumSize = entryNode.GetCombinedMinimumSize();
        var targetNodeWidth = Mathf.Max(entryNode.Size.X, minimumSize.X);
        entryNode.Size = new Vector2(targetNodeWidth, minimumSize.Y);
    }

    private void RefreshAllEntryNoteHeights()
    {
        foreach (var entry in _mindMapData.Entries)
        {
            var node = GetNodeOrNull<GraphNode>(GetEntryNodeName(entry.Id));
            if (node is null)
            {
                continue;
            }

            var noteEdit = node.GetNodeOrNull<TextEdit>("Content/Note");
            if (noteEdit is null)
            {
                continue;
            }

            UpdateNoteEditorHeight(noteEdit, node);
        }
    }

    private void ApplySearchHighlighting()
    {
        var highlightedIds = new HashSet<int>(_searchMatchIds);

        foreach (var entry in _mindMapData.Entries)
        {
            var node = GetNodeOrNull<GraphNode>(GetEntryNodeName(entry.Id));
            if (node is null)
            {
                continue;
            }

            node.Modulate = highlightedIds.Contains(entry.Id)
                ? SearchMatchEntryModulate
                : DefaultEntryModulate;
        }
    }

    private void ClearSearchHighlighting()
    {
        foreach (var entry in _mindMapData.Entries)
        {
            var node = GetNodeOrNull<GraphNode>(GetEntryNodeName(entry.Id));
            if (node is null)
            {
                continue;
            }

            node.Modulate = DefaultEntryModulate;
        }
    }

    private void FocusEntryById(int entryId)
    {
        var node = GetNodeOrNull<GraphNode>(GetEntryNodeName(entryId));
        if (node is null)
        {
            return;
        }

        var zoom = Mathf.Max(Zoom, MinGraphZoom);
        var viewportCenterInGraph = ScrollOffset + Size * 0.5f / zoom;
        var nodeCenter = node.PositionOffset + node.Size * 0.5f;
        ScrollOffset += nodeCenter - viewportCenterInGraph;
    }

    private void ClearAllEntryNodes()
    {
        var nodesToRemove = new List<Node>();

        foreach (var child in GetChildren())
        {
            if (child is GraphNode graphNode && graphNode.Name.ToString().StartsWith(EntryNodePrefix, StringComparison.Ordinal))
            {
                nodesToRemove.Add(graphNode);
            }
        }

        foreach (var node in nodesToRemove)
        {
            node.QueueFree();
        }
    }

    private static string GetEntryNodeName(int id)
    {
        return $"{EntryNodePrefix}{id}";
    }

    private static int? TryParseEntryId(string nodeName)
    {
        if (string.IsNullOrWhiteSpace(nodeName) || !nodeName.StartsWith(EntryNodePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var idText = nodeName.Substring(EntryNodePrefix.Length);
        return int.TryParse(idText, out var id) ? id : null;
    }
}
