using System;
using System.Text.Json;
using Godot;

public partial class MindMapFileDialog : FileDialog
{
    private const string DefaultSavePath = "user://mindmap.json";
    private readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
    private string _lastPath = DefaultSavePath;
    private FileAction _pendingFileAction = FileAction.None;

    public override void _Ready()
    {
        Access = AccessEnum.Userdata;
        AddFilter("*.json ; Mind Map Project");
        FileSelected += OnFileSelected;
        Canceled += OnCanceled;
    }

    private void OnFileSelected(string path)
    {
        _lastPath = path;

        switch (_pendingFileAction)
        {
            case FileAction.Save: Save(path); break;
            case FileAction.Load: Load(path); break;
        }

        _pendingFileAction = FileAction.None;
    }
    private void OnCanceled() => _pendingFileAction = FileAction.None;


    public void Open(FileAction action)
    {
        _pendingFileAction = action;
        FileMode = action == FileAction.Save ? FileModeEnum.SaveFile : FileModeEnum.OpenFile;
        CurrentPath = _lastPath;
        PopupCenteredRatio(0.7f);
    }

    public void Quicksave()
    {
        if (string.IsNullOrWhiteSpace(_lastPath) || !FileAccess.FileExists(_lastPath)) return;

        Save(_lastPath);
    }

    private void Save(string path)
    {
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            LogMaster.LogError($"Cannot open file for writing: {path}", "FileDialog", "Save");
            return;
        }

        Tree.MindMap.UpdateData();

        string json = JsonSerializer.Serialize(Tree.MindMap.Data, jsonOptions);
        file.StoreString(json);
    }
    private void Load(string path)
    {
        if (!FileAccess.FileExists(path))
        {
            LogMaster.LogError($"Save file does not exist: {path}", "FileDialog", "Load");
            return;
        }

        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            LogMaster.LogError($"Cannot open file for reading: {path}", "FileDialog", "Load");
            return;
        }

        string json = file.GetAsText();
        MindMapData loaded;
        try
        {
            loaded = JsonSerializer.Deserialize<MindMapData>(json, jsonOptions);
        }
        catch (Exception e)
        {
            LogMaster.LogError($"Failed to deserialize project: {e.Message}", "FileDialog", "Load");
            GD.PushError($"Failed to deserialize project: {e.Message}");
            return;
        }

        if (loaded is null)
        {
            LogMaster.LogError("Loaded project data is null.", "FileDialog", "Load");
            return;
        }

        Tree.MindMap.Build(loaded);
        Tree.SearchInput.Clear();
        Tree.TodoList.Refresh();
    }
}
