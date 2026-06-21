using ConsoleApp.Controllers;

namespace ConsoleApp.UI.Pages;

public class GameInitPage : IPage
{
    private readonly GameController _controller;
    private bool _gameOver = false;

    public GameInitPage(GameController controller)
    {
        _controller = controller;
        _controller.OnGameOver += () => _gameOver = true;
    }

    public IPage? Index()
    {
        if (_gameOver)
            return new MainMenuPage(_controller);

        Console.WriteLine("INSERT NAME");
        Console.Write("Player one name: "); string? playerOneName = Console.ReadLine();
        Console.Write("Player two name: "); string? playerTwoName = Console.ReadLine();

        _controller.Start();

        return Console.ReadKey(true).Key switch
        {
            _ => this
        };
    }
}
