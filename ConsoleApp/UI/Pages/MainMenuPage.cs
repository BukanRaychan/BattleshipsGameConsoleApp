using ConsoleApp.Controllers;

namespace ConsoleApp.UI.Pages;

public class MainMenuPage : IPage
{
    private readonly GameController _controller;

    public MainMenuPage(GameController controller)
    {
        _controller = controller;
    }

    public IPage? Index()
    {
        Console.WriteLine("1. Start Game");
        Console.WriteLine("2. Exit");

        return Console.ReadKey(true).Key switch
        {
            ConsoleKey.D1 => new GameInitPage(_controller),
            ConsoleKey.D2 => null,
            _ => this
        };
    }
}
