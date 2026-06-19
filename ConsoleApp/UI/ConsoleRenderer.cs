using ConsoleApp.Controlles;

namespace ConsoleApp.UI;

public class ConsoleRenderer
{
    private readonly GameController _gameController;

    public ConsoleRenderer(GameController gameController)
    {
        _gameController = gameController;
    }
    public void Run()
    {
        Console.Clear();
    }
}