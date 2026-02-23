using Godot;

public partial class Main : Control
{
    private const string DefaultSavePath = "user://mindmap.json";
    private const float MinGraphZoom = 0.0001f;

    private enum FileAction
    {
        None,
        Save,
        Load
    }

    private MindMapManager _mindMapManager;
    private Control _sidebar;
    private FileDialog _projectFileDialog;
    private int _spawnIndex;
    private string _lastProjectPath = DefaultSavePath;
    private FileAction _pendingFileAction = FileAction.None;

    public override void _Ready()
    {
        _mindMapManager = GetNode<MindMapManager>("HSplitContainer/MindMapGraph");
        _sidebar = GetNode<Control>("HSplitContainer/Margin/Sidebar");
        _projectFileDialog = GetNode<FileDialog>("ProjectFileDialog");

        var addButton = GetNode<Button>("HSplitContainer/Margin/Sidebar/AddEntryButton");
        var saveButton = GetNode<Button>("HSplitContainer/Margin/Sidebar/SaveButton");
        var loadButton = GetNode<Button>("HSplitContainer/Margin/Sidebar/LoadButton");

        addButton.Pressed += OnAddEntryPressed;
        saveButton.Pressed += OnSavePressed;
        loadButton.Pressed += OnLoadPressed;

        _projectFileDialog.Access = FileDialog.AccessEnum.Userdata;
        _projectFileDialog.AddFilter("*.json ; Mind Map Project");
        _projectFileDialog.FileSelected += OnProjectFileSelected;
        _projectFileDialog.Canceled += OnProjectFileDialogCanceled;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
        {
            return;
        }

        if (!keyEvent.CtrlPressed)
        {
            return;
        }

        switch (keyEvent.Keycode)
        {
            case Key.N:
                OnAddEntryPressed();
                GetViewport().SetInputAsHandled();
                break;
            case Key.S:
                OnSavePressed();
                GetViewport().SetInputAsHandled();
                break;
            case Key.O:
                OnLoadPressed();
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    private void OnAddEntryPressed()
    {
        var position = GetEntrySpawnPosition();
        _mindMapManager.AddEntry(position);
        _spawnIndex++;
    }

    private Vector2 GetEntrySpawnPosition()
    {
        var mouseGlobalPosition = GetGlobalMousePosition();
        var mouseOverSidebar = _sidebar.GetGlobalRect().HasPoint(mouseGlobalPosition);
        var mouseOverMindMap = _mindMapManager.GetGlobalRect().HasPoint(mouseGlobalPosition);

        if (mouseOverMindMap && !mouseOverSidebar)
        {
            return ConvertLocalGraphViewToGraphPosition(_mindMapManager.GetLocalMousePosition());
        }

        var visibleCenter = _mindMapManager.Size * 0.5f;
        var visibleCenterGraphPosition = ConvertLocalGraphViewToGraphPosition(visibleCenter);
        var fallbackOffsetStep = _spawnIndex % 5;
        var fallbackOffset = new Vector2(40 * fallbackOffsetStep, 30 * fallbackOffsetStep);
        return visibleCenterGraphPosition + fallbackOffset;
    }

    private Vector2 ConvertLocalGraphViewToGraphPosition(Vector2 localGraphViewPosition)
    {
        var zoom = Mathf.Max(_mindMapManager.Zoom, MinGraphZoom);
        return _mindMapManager.ScrollOffset + localGraphViewPosition / zoom;
    }

    private void OnSavePressed()
    {
        if (TryOverwriteExistingSave())
        {
            return;
        }

        OpenPathPicker(FileAction.Save);
    }

    private void OnLoadPressed()
    {
        OpenPathPicker(FileAction.Load);
    }

    private void OpenPathPicker(FileAction action)
    {
        _pendingFileAction = action;
        _projectFileDialog.FileMode = action == FileAction.Save
            ? FileDialog.FileModeEnum.SaveFile
            : FileDialog.FileModeEnum.OpenFile;
        _projectFileDialog.CurrentPath = _lastProjectPath;
        _projectFileDialog.PopupCenteredRatio(0.7f);
    }

    private bool TryOverwriteExistingSave()
    {
        if (string.IsNullOrWhiteSpace(_lastProjectPath))
        {
            return false;
        }

        if (!FileAccess.FileExists(_lastProjectPath))
        {
            return false;
        }

        return _mindMapManager.SaveProject(_lastProjectPath);
    }

    private void OnProjectFileSelected(string path)
    {
        _lastProjectPath = path;

        switch (_pendingFileAction)
        {
            case FileAction.Save:
                _mindMapManager.SaveProject(path);
                break;
            case FileAction.Load:
                _mindMapManager.LoadProject(path);
                break;
        }

        _pendingFileAction = FileAction.None;
    }

    private void OnProjectFileDialogCanceled()
    {
        _pendingFileAction = FileAction.None;
    }
}
