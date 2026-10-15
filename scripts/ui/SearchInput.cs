using Godot;

public partial class SearchInput : LineEdit
{
    public override void _Ready() => TextChanged += OnSearchInputChanged;

    private void OnSearchInputChanged(string query) => Tree.MindMap.Highlight(query);
}
