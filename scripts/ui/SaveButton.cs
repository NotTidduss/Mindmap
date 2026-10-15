using Godot;

public partial class SaveButton : Button
{
    public override void _Ready() => Pressed += OnPressed;

    private void OnPressed() => Tree.FileDialog.Open(FileAction.Save);
}
