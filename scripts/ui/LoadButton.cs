using Godot;

public partial class LoadButton : Button
{
    public override void _Ready() => Pressed += OnPressed;

    public void OnPressed() => Tree.FileDialog.Open(FileAction.Load);
}
