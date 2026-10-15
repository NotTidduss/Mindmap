using Godot;

public partial class SearchCount : Label
{
    public override void _Ready() => Set(0);

    public void Set(int count) => Text = count == 1 ? "1 match" : $"{count} matches";
}
