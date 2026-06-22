using ConsoleApp.Controllers;
using ConsoleApp.DTOs;
using Spectre.Console;

namespace ConsoleApp.UI.Pages;

public class GameInitPage : IPage
{
    private readonly GameController _controller;

    public GameInitPage(GameController controller)
    {
        _controller = controller;
    }

    public IPage? Index()
    {
        Console.WriteLine("INSERT NAME");
        string playerOneName = AnsiConsole.Ask<string>("Insert first player's name?\t");
        string playerTwoName = AnsiConsole.Ask<string>("Insert second player's name?\t");

        _controller.StartPlacementPhase(new StartPlacementPhaseDto(playerOneName, playerTwoName));

        return new ShipPlacementPage(_controller);
    }
}
