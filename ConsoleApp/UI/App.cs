using ConsoleApp.Controllers;
using ConsoleApp.UI.Pages;

namespace ConsoleApp.UI;

public class App
{
    private readonly GameController _controller;
    private readonly GameLayout _layout;

    public App(GameController controller)
    {
        _controller = controller;
        _layout = new GameLayout(_controller);
    }

    public void Run()
    {
        IPage? current = new MainMenuPage(_controller);
        _controller.LoadGame();
        while (current != null)
        {
            current = _layout.Render(current);
        }
        Console.Clear();
    }
}
