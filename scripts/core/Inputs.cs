using Godot;

public partial class Inputs : Node
{
    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo) return;

        if (keyEvent.Keycode == Key.Escape)
        {
            Tree.SearchInput.Clear();
            Tree.SearchCount.Set(0);
            // test if necessary
            // Tree.MindMap.Lowlight();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!keyEvent.CtrlPressed)
        {
            return;
        }

        switch (keyEvent.Keycode)
        {
            case Key.N:
                Tree.AddButton.OnAddEntryInput();
                GetViewport().SetInputAsHandled();
                break;
            case Key.S:
                Tree.FileDialog.Quicksave();
                GetViewport().SetInputAsHandled();
                break;
            case Key.O:
                Tree.LoadButton.OnPressed();
                GetViewport().SetInputAsHandled();
                break;
        }
    }
}
