using Godot;

public partial class AddButton : Button
{
    public override void _Ready() => Pressed += OnAddEntryPressed;

    public void OnAddEntryPressed() => Tree.MindMap.AddEntry(GetEntrySpawnPosition());
    public void OnAddEntryInput() => Tree.MindMap.AddEntry(GetRelativeMousePosition());

    private Vector2 GetEntrySpawnPosition()
    {
        Vector2 visibleCenter = Tree.MindMap.Size * 0.5f;
        Vector2 visibleCenterGraphPosition = ConvertLocalGraphViewToGraphPosition(visibleCenter);
        return visibleCenterGraphPosition;
    }
    private Vector2 GetRelativeMousePosition()
    {
        Vector2 graphLocalMousePosition = GetGlobalMousePosition() - Tree.MindMap.GlobalPosition;
        return ConvertLocalGraphViewToGraphPosition(graphLocalMousePosition);
    }

    private Vector2 ConvertLocalGraphViewToGraphPosition(Vector2 localGraphViewPosition)
    {
        var zoom = Mathf.Max(Tree.MindMap.Zoom, 0.0001f);
        return Tree.MindMap.ScrollOffset + localGraphViewPosition / zoom;
    }
}
