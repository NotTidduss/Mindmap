using Godot;

public partial class Main : Control
{
    private const string DefaultSavePath = "user://mindmap.json";

    private enum FileAction
    {
        None,
        Save,
        Load
    }

    private MindMapManager _mindMapManager;
    private FileDialog _projectFileDialog;
    private int _spawnIndex;
    private string _lastProjectPath = DefaultSavePath;
    private FileAction _pendingFileAction = FileAction.None;

    public override void _Ready()
    {
        _mindMapManager = GetNode<MindMapManager>("HSplitContainer/MindMapGraph");
        _projectFileDialog = GetNode<FileDialog>("ProjectFileDialog");

        var addButton = GetNode<Button>("HSplitContainer/Sidebar/AddEntryButton");
        var saveButton = GetNode<Button>("HSplitContainer/Sidebar/SaveButton");
        var loadButton = GetNode<Button>("HSplitContainer/Sidebar/LoadButton");

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
        var position = new Vector2(120 + 40 * _spawnIndex, 120 + 30 * _spawnIndex);
        _mindMapManager.AddEntry(position);
        _spawnIndex++;
    }

    private void OnSavePressed()
    {
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
