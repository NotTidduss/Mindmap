using Godot;

public partial class Tree : Node
{
    [Export] private MindMap mindMap;
    [Export] private AddButton addButton;
    [Export] private SaveButton saveButton;
    [Export] private LoadButton loadButton;
    [Export] private SearchInput searchInput;
    [Export] private SearchCount searchCount;
    [Export] private TodoList todoList;
    [Export] private MindMapFileDialog fileDialog;

    public static MindMap MindMap;
    public static AddButton AddButton;
    public static SaveButton SaveButton;
    public static LoadButton LoadButton;
    public static SearchInput SearchInput;
    public static SearchCount SearchCount;
    public static TodoList TodoList;
    public static MindMapFileDialog FileDialog;

    public override void _Ready()
    {
        OS.LowProcessorUsageMode = true;

        if (mindMap == null) LogMaster.LogError("Mindmap is not assigned!", "Tree", "Ready");
        if (addButton == null) LogMaster.LogError("AddButton is not assigned!", "Tree", "Ready");
        if (saveButton == null) LogMaster.LogError("SaveButton is not assigned!", "Tree", "Ready");
        if (loadButton == null) LogMaster.LogError("LoadButton is not assigned!", "Tree", "Ready");
        if (searchInput == null) LogMaster.LogError("SearchInput is not assigned!", "Tree", "Ready");
        if (searchCount == null) LogMaster.LogError("SearchCount is not assigned!", "Tree", "Ready");
        if (todoList == null) LogMaster.LogError("TodoList is not assigned!", "Tree", "Ready");
        if (fileDialog == null) LogMaster.LogError("FileDialog is not assigned!", "Tree", "Ready");

        MindMap = mindMap;
        AddButton = addButton;
        SaveButton = saveButton;
        LoadButton = loadButton;
        SearchInput = searchInput;
        SearchCount = searchCount;
        TodoList = todoList;
        FileDialog = fileDialog;
    }
}
